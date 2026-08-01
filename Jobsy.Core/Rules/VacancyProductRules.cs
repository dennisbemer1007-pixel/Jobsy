namespace Jobsy.Core.Rules;

public static class VacancyProductRules
{
    /// <summary>Default length added when extending a vacancy.</summary>
    public const int ExtendDays = 14;

    /// <summary>Paid highlight visibility window after purchase/activation.</summary>
    public const int HighlightDays = 14;

    /// <summary>Default highlight token cost when no admin TokenSpendCosts row exists (1–2 tokens).</summary>
    public const decimal DefaultHighlightCostTokens = 1m;

    /// <summary>Fallback PushBom radius when no admin settings row exists.</summary>
    public const double PushBomRadiusKm = 10;

    /// <summary>Fallback max travel minutes when no admin settings row exists.</summary>
    public const int PushBomMaxTravelMinutes = 30;
}
