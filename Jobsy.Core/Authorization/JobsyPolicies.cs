namespace Jobsy.Core.Authorization;

public static class JobsyPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireEmployer = "RequireEmployer";
    public const string RequireAdminOrEmployer = "RequireAdminOrEmployer";
    public const string RequireCandidate = "RequireCandidate";
}
