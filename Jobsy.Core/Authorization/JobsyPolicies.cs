namespace Jobsy.Core.Authorization;

public static class JobsyPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireEmployer = "RequireEmployer";
    public const string RequireAdminOrEmployer = "RequireAdminOrEmployer";
    public const string RequireCandidate = "RequireCandidate";
    public const string RequireSalesManager = "RequireSalesManager";
    public const string RequireAdminOrSalesManager = "RequireAdminOrSalesManager";
    public const string RequireAmbassadeur = "RequireAmbassadeur";
    public const string RequireAdminOrAmbassadeur = "RequireAdminOrAmbassadeur";
    public const string RequireApiKey = "RequireApiKey";

    /// <summary>Admin, employer, sales manager and ambassadeur dashboards (manual cache refresh).</summary>
    public const string RequireDashboardAccess = "RequireDashboardAccess";
}
