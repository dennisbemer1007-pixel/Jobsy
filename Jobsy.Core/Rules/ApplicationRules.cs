using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class ApplicationRules
{
    public static bool IsSameCandidate(
        Guid candidateUserId,
        string candidateEmail,
        Guid? existingUserId,
        string existingEmail)
        => existingUserId == candidateUserId
           || string.Equals(existingEmail, candidateEmail, StringComparison.OrdinalIgnoreCase);

    public static bool CanEmployerReact(ApplicationStatus status)
        => status == ApplicationStatus.Pending;

    public static bool CanCandidateWithdraw(ApplicationStatus status, DateTime? emailVerifiedAt)
        => emailVerifiedAt is not null && status == ApplicationStatus.Pending;

    public static bool IsOpenForEmployerPipeline(ApplicationStatus status)
        => status is ApplicationStatus.Pending
            or ApplicationStatus.Accepted
            or ApplicationStatus.EmployerContacting;

    public static bool IsTerminal(ApplicationStatus status)
        => status is ApplicationStatus.Rejected
            or ApplicationStatus.Hired
            or ApplicationStatus.FilledElsewhere
            or ApplicationStatus.Withdrawn;

    /// <summary>
    /// Only e-mail-verified applications appear under Sollicitaties.
    /// Drafts waiting on a verification code are not listed and have no candidate-facing status.
    /// </summary>
    public static bool IsListedForCandidate(DateTime? emailVerifiedAt)
        => emailVerifiedAt is not null;

    /// <summary>Employer sees candidate PII (and Lobsy-CV PDF) after Accept.</summary>
    public static bool IsPiiRevealed(ApplicationStatus status)
        => LobsyCvAccessRules.IsPiiRevealed(status);

    /// <summary>
    /// Verified applications occupy a vacancy slot, except withdrawn ones (slot freed).
    /// </summary>
    public static bool CountsTowardVacancyCapacity(ApplicationStatus status, DateTime? emailVerifiedAt)
        => emailVerifiedAt is not null && status != ApplicationStatus.Withdrawn;

    /// <summary>
    /// A verified application blocks a new apply unless it was withdrawn (reusable).
    /// </summary>
    public static bool BlocksDuplicateApplication(ApplicationStatus status, DateTime? emailVerifiedAt)
        => emailVerifiedAt is not null && status != ApplicationStatus.Withdrawn;

    /// <summary>Withdrawn applications may be reopened on a fresh apply.</summary>
    public static bool CanReuseWithdrawnApplication(ApplicationStatus status)
        => status == ApplicationStatus.Withdrawn;

    /// <summary>
    /// When fulfilling a vacancy, other applications that should move to FilledElsewhere.
    /// </summary>
    public static bool ShouldRejectAsFilledElsewhere(ApplicationStatus status, DateTime? emailVerifiedAt)
        => emailVerifiedAt is not null
           && status is not (ApplicationStatus.Rejected
               or ApplicationStatus.FilledElsewhere
               or ApplicationStatus.Hired
               or ApplicationStatus.Withdrawn);
}
