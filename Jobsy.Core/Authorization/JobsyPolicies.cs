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
    public const string RequireSchoolAdmin = "RequireSchoolAdmin";
    public const string RequireTeacher = "RequireTeacher";
    public const string RequireSchoolStaff = "RequireSchoolStaff";
    public const string RequireApiKey = "RequireApiKey";

    /// <summary>Pupil code session (scheme <c>Pupil</c> only — never staff/candidate cookies).</summary>
    public const string PupilSession = "PupilSession";

    /// <summary>Admin, employer, sales manager and ambassadeur dashboards (manual cache refresh).</summary>
    public const string RequireDashboardAccess = "RequireDashboardAccess";
}
