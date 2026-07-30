namespace Jobsy.Core.Rules;

/// <summary>
/// Shared Dutch youth / WML-style fractions of the adult (21+) hourly rate.
/// Used by vacancy wage display bands and hardcoded salary fallbacks.
/// </summary>
public static class YouthWageFractions
{
    public static readonly (int AgeYears, string Label, decimal FractionOfAdult)[] Default =
    [
        (15, "15", 0.30m),
        (16, "16", 0.345m),
        (17, "17", 0.395m),
        (18, "18", 0.50m),
        (19, "19", 0.60m),
        (20, "20", 0.80m),
        (21, "21+", 1.00m)
    ];

    public static decimal FractionForAge(int ageYears)
    {
        var age = AgeRules.ClampWorkingAge(ageYears);
        if (age >= AgeRules.AdultAgeYears)
        {
            return 1.00m;
        }

        var match = Default.LastOrDefault(f => f.AgeYears <= age);
        return match.AgeYears == 0 ? Default[0].FractionOfAdult : match.FractionOfAdult;
    }
}
