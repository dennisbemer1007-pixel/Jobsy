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
}
