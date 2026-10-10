using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

public static class AcceptCandidatePricingRules
{
    public const decimal TokenEuroValue = 25m;

    public static decimal ResolveCostTokens(FlexCommercialSettingsDto settings, DateOnly todayUtc)
        => ResolveCostTokens(ToEntity(settings), todayUtc);

    public static bool IsPilotActive(FlexCommercialSettingsDto settings, DateOnly todayUtc)
        => IsPilotActive(ToEntity(settings), todayUtc);

    private static FlexCommercialSettings ToEntity(FlexCommercialSettingsDto dto)
        => new()
        {
            AcceptCandidatePilotCostTokens = dto.AcceptCandidatePilotCostTokens,
            AcceptCandidatePilotEndsOn = dto.AcceptCandidatePilotEndsOn,
            AcceptCandidateStandardCostTokens = dto.AcceptCandidateStandardCostTokens
        };

    public static decimal ResolveCostTokens(FlexCommercialSettings settings, DateOnly todayUtc)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.AcceptCandidatePilotEndsOn is DateOnly end && todayUtc <= end)
        {
            return Math.Round(settings.AcceptCandidatePilotCostTokens, 2, MidpointRounding.AwayFromZero);
        }

        return Math.Round(settings.AcceptCandidateStandardCostTokens, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal EuroDisplay(decimal tokenCost)
        => Math.Round(tokenCost * TokenEuroValue, 2, MidpointRounding.AwayFromZero);

    public static bool IsPilotActive(FlexCommercialSettings settings, DateOnly todayUtc)
        => settings.AcceptCandidatePilotEndsOn is DateOnly end && todayUtc <= end;

    public static int NormalizeAgencyRadiusKm(int? km)
    {
        return km switch
        {
            5 => 5,
            10 => 10,
            _ => 2
        };
    }
}
