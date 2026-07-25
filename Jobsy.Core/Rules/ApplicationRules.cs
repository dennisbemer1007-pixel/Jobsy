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
}
