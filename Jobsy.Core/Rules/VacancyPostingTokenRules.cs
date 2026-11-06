namespace Jobsy.Core.Rules;

/// <summary>
/// Vacancy lifecycle (create, publish, renew/extend, edit) never debits tokens.
/// Tokens are spent on accept-candidate (and optional paid map products such as highlight/PushBom).
/// </summary>
public static class VacancyPostingTokenRules
{
    public const decimal PublishCostTokens = 0m;
    public const decimal ExtendCostTokens = 0m;

    public static decimal EffectivePublishCost(decimal _) => PublishCostTokens;

    public static decimal EffectiveExtendCost() => ExtendCostTokens;
}
