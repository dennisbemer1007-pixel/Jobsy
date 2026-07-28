namespace Jobsy.Core.Authorization;

/// <summary>
/// Custom claim types used alongside standard role claims from Microsoft Entra ID.
/// </summary>
public static class JobsyClaimTypes
{
    public const string CompanyId = "company_id";
    public const string CompanyIds = "company_ids";

    /// <summary>Set when the user has personal candidate applications (e.g. after promotion to manager).</summary>
    public const string HasCandidateApplications = "has_candidate_applications";
}
