using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Gates for companies registered while KVK was unavailable.
/// Pending/Failed accounts may prepare drafts but must not go public or spend tokens
/// until KVK verification succeeds (AVG: no candidate exposure to unverified employers).
/// </summary>
public static class KvkVerificationRules
{
    public static bool IsVerified(KvkVerificationStatus status)
        => status == KvkVerificationStatus.Verified;

    public static bool CanPublishOrSpend(KvkVerificationStatus status)
        => IsVerified(status);

    public static string BlockedMessage(KvkVerificationStatus status)
        => status == KvkVerificationStatus.Failed
            ? "KVK-verificatie is mislukt. Neem contact op met support voordat je vacatures publiceert of tokens koopt."
            : "Je account staat op KVK-verificatie in afwachting. Je kunt concepten klaarzetten; publiceren en tokenaankopen volgen na automatische KVK-controle.";
}
