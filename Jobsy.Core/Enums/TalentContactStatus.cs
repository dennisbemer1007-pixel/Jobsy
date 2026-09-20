namespace Jobsy.Core.Enums;

/// <summary>Lifecycle of an employer → candidate contact unlock from the anonymous talent pool.</summary>
public enum TalentContactStatus
{
    /// <summary>Token spent; waiting for candidate response within 48h.</summary>
    Pending = 0,

    /// <summary>Candidate accepted; PII may be shared.</summary>
    ContactShared = 1,

    /// <summary>Candidate declined (already placed / not interested); refund eligible.</summary>
    CandidateDeclined = 2,

    /// <summary>Employer withdrew after timeout or decline; token refunded.</summary>
    WithdrawnRefunded = 3,

    /// <summary>48h passed without response; employer may withdraw for refund.</summary>
    RefundEligible = 4
}
