namespace Jobsy.Core.Enums;

/// <summary>
/// Lifecycle of a partner (BM/IM) referral reward for a referred company.
/// </summary>
public enum PartnerReferralStatus
{
    /// <summary>Not attributed to a partner affiliate.</summary>
    None = 0,

    /// <summary>Linked via tracking code; waiting for welcome-token spend.</summary>
    Pending = 1,

    /// <summary>Welcome token spent; 0.5 token bonus granted to the referring partner (once).</summary>
    Rewarded = 2
}
