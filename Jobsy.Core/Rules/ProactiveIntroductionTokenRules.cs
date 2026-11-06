using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Proactive kennismakingsverzoeken: token pas na acceptatie door de kandidaat.
/// </summary>
public static class ProactiveIntroductionTokenRules
{
    public static bool UsesDeferredTokenCharge(TalentContactRequest request)
        => request.TokenChargeOnAccept;

    public static bool ShouldChargeTokenOnAccept(TalentContactRequest request, bool accept, bool alreadyPlaced)
        => accept
           && !alreadyPlaced
           && UsesDeferredTokenCharge(request)
           && request.SpendTransactionId is null
           && request.Status is TalentContactStatus.Pending or TalentContactStatus.RefundEligible;

    public static bool EmployerSeesPii(TalentContactRequest request)
        => request.Status == TalentContactStatus.ContactShared
           && (!UsesDeferredTokenCharge(request) || request.SpendTransactionId is not null);
}
