using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Progressive disclosure for the auto-generated Lobsy-CV PDF.
/// Mirrors employer PII reveal: Accepted / EmployerContacting / Hired.
/// </summary>
public static class LobsyCvAccessRules
{
    public static bool IsPiiRevealed(ApplicationStatus status)
        => status is ApplicationStatus.Accepted
            or ApplicationStatus.EmployerContacting
            or ApplicationStatus.Hired;

    /// <summary>Employer may download the application snapshot PDF only after Accept + e-mail verification.</summary>
    public static bool CanEmployerDownloadCv(ApplicationStatus status, DateTime? emailVerifiedAt)
        => emailVerifiedAt is not null && IsPiiRevealed(status);

    /// <summary>Candidate may always preview their live profile PDF; application PDF when they own it.</summary>
    public static bool CanCandidateDownloadOwnApplication(
        Guid callerUserId,
        Guid? applicationCandidateUserId,
        string callerEmail,
        string applicationCandidateEmail)
        => applicationCandidateUserId == callerUserId
           || (!string.IsNullOrWhiteSpace(callerEmail)
               && string.Equals(callerEmail, applicationCandidateEmail, StringComparison.OrdinalIgnoreCase));
}
