namespace Jobsy.Core.Authorization;

/// <summary>Claim types on the Loopbaan-APK employee principal. No employee name or employer role.</summary>
public static class WerkgeverApkClaimTypes
{
    public const string EmployeeCodeId = "werkgever_apk_employee_code_id";
    public const string TeamId = "werkgever_apk_team_id";
    public const string CompanyId = "werkgever_apk_company_id";
    public const string SessionVersion = "session_version";
    public const string IssuedAt = "iat";
    /// <summary>Display-only masked code for chrome.</summary>
    public const string CodeDisplay = "code_display";
    /// <summary>Display-only team label for chrome.</summary>
    public const string TeamName = "team_name";
    /// <summary>Display-only company label for chrome.</summary>
    public const string CompanyName = "company_name";
}
