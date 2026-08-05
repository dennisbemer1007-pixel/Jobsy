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

    /// <summary>Set when the user's primary company was referred via a salesmanager tracking code.</summary>
    public const string HasSalesReferral = "has_sales_referral";

    /// <summary>
    /// HMAC session proof for local/password (and external) login — forwarded as
    /// <c>X-Jobsy-Local-Session</c> so Production DevelopmentAuth can authorize non-demo emails.
    /// </summary>
    public const string LocalSession = "jobsy_local_session";
}
