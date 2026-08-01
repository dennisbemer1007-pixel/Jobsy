namespace Jobsy.Core.Rules;

public static class VacancyProductRules
{
    /// <summary>Default length added when extending a vacancy.</summary>
    public const int ExtendDays = 14;

    /// <summary>Paid highlight visibility window after purchase/activation (legacy fallback).</summary>
    public const int HighlightDays = 14;

    /// <summary>Default 1-week Funda carousel highlight window from sales commercial settings.</summary>
    public const int DefaultHighlightCarouselDays = 7;

    /// <summary>Default highlight token cost when no admin TokenSpendCosts row exists (1–2 tokens).</summary>
    public const decimal DefaultHighlightCostTokens = 1m;

    /// <summary>Default list price per token for partner sales materials.</summary>
    public const decimal DefaultBaseTokenValueEuro = 25m;

    /// <summary>Default carousel highlight token cost (also the start-highlight bonus value).</summary>
    public const decimal DefaultHighlightCarouselTokens = 2m;

    /// <summary>Default pulse-marker token value shown in sales materials.</summary>
    public const decimal DefaultHighlightPulseTokens = 1m;

    /// <summary>Fallback PushBom radius when no admin settings row exists.</summary>
    public const double PushBomRadiusKm = 10;

    /// <summary>Fallback max travel minutes when no admin settings row exists.</summary>
    public const int PushBomMaxTravelMinutes = 30;
}
