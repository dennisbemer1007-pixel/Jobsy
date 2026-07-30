namespace Jobsy.Core.Rules;

public enum HoursCategory
{
    SideJob = 0,
    PartTimeSmall = 1,
    PartTimeLarge = 2,
    FullTime = 3
}

/// <summary>Min/max hours per week with derived category (midpoint rules).</summary>
public readonly record struct HoursRange(decimal MinHoursPerWeek, decimal MaxHoursPerWeek)
{
    public const decimal AbsoluteMin = 1m;
    public const decimal AbsoluteMax = 60m;

    public decimal Midpoint => (MinHoursPerWeek + MaxHoursPerWeek) / 2m;

    public HoursCategory Category => HoursRangeRules.Categorize(Midpoint);

    public string? Validate() => HoursRangeRules.Validate(MinHoursPerWeek, MaxHoursPerWeek);
}

public static class HoursRangeRules
{
    public static string? Validate(decimal minHours, decimal maxHours)
    {
        if (minHours < HoursRange.AbsoluteMin || minHours > HoursRange.AbsoluteMax)
        {
            return $"Minimum uren/week moet tussen {HoursRange.AbsoluteMin} en {HoursRange.AbsoluteMax} liggen.";
        }

        if (maxHours < HoursRange.AbsoluteMin || maxHours > HoursRange.AbsoluteMax)
        {
            return $"Maximum uren/week moet tussen {HoursRange.AbsoluteMin} en {HoursRange.AbsoluteMax} liggen.";
        }

        if (maxHours < minHours)
        {
            return "Maximum uren/week mag niet lager zijn dan het minimum.";
        }

        return null;
    }

    public static HoursCategory Categorize(decimal midpoint) => midpoint switch
    {
        < 12m => HoursCategory.SideJob,
        < 24m => HoursCategory.PartTimeSmall,
        < 32m => HoursCategory.PartTimeLarge,
        _ => HoursCategory.FullTime
    };

    public static string CategoryLabel(HoursCategory category) => category switch
    {
        HoursCategory.SideJob => "Bijbaan / oproep",
        HoursCategory.PartTimeSmall => "Parttime klein",
        HoursCategory.PartTimeLarge => "Parttime groot",
        HoursCategory.FullTime => "Fulltime",
        _ => category.ToString()
    };

    /// <summary>
    /// Coverage of vacancy interval by candidate interval: overlap / vacancySpan, clamped 0–1.
    /// </summary>
    public static decimal OverlapScore01(HoursRange candidate, HoursRange vacancy)
    {
        var overlap = Math.Max(
            0m,
            Math.Min(candidate.MaxHoursPerWeek, vacancy.MaxHoursPerWeek)
            - Math.Max(candidate.MinHoursPerWeek, vacancy.MinHoursPerWeek));
        if (overlap <= 0)
        {
            return 0m;
        }

        var vacancySpan = Math.Max(vacancy.MaxHoursPerWeek - vacancy.MinHoursPerWeek, 1m);
        return Math.Clamp(overlap / vacancySpan, 0m, 1m);
    }

    public static decimal OverlapHours(HoursRange candidate, HoursRange vacancy)
        => Math.Max(
            0m,
            Math.Min(candidate.MaxHoursPerWeek, vacancy.MaxHoursPerWeek)
            - Math.Max(candidate.MinHoursPerWeek, vacancy.MinHoursPerWeek));
}
