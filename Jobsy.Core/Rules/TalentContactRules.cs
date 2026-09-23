using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Business rules for the 48-hour ContactUnlock refund guarantee.</summary>
public static class TalentContactRules
{
    public static readonly TimeSpan ResponseWindow = TimeSpan.FromHours(TalentContactRequestHours);

    public const int TalentContactRequestHours = 48;

    public const decimal DefaultUnlockCostTokens = FlexCommercialSettings.DefaultContactUnlockCostTokens;

    public static DateTime ComputeRespondByUtc(DateTime createdAtUtc)
        => createdAtUtc.Add(ResponseWindow);

    public static bool IsPastDeadline(DateTime utcNow, DateTime respondByUtc)
        => utcNow >= respondByUtc;

    public static bool CanEmployerWithdraw(TalentContactStatus status, DateTime utcNow, DateTime respondByUtc)
        => status is TalentContactStatus.RefundEligible or TalentContactStatus.CandidateDeclined
           || (status == TalentContactStatus.Pending && IsPastDeadline(utcNow, respondByUtc));

    public static bool CanCandidateRespond(TalentContactStatus status)
        => status is TalentContactStatus.Pending or TalentContactStatus.RefundEligible;

    /// <summary>
    /// After a successful contact exchange ("de klik is er niet" later) there is no refund.
    /// </summary>
    public static bool IsRefundBlockedAfterContactShared(TalentContactStatus status)
        => status == TalentContactStatus.ContactShared;
}
