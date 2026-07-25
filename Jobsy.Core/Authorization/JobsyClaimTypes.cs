namespace Jobsy.Core.Authorization;

/// <summary>
/// Custom claim types used alongside standard role claims from Microsoft Entra ID.
/// </summary>
public static class JobsyClaimTypes
{
    public const string CompanyId = "company_id";
    public const string CompanyIds = "company_ids";
}
