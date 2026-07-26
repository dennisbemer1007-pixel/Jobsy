using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves vacancy pay from a company salary table (age bands) or the flat hourly wage.
/// Age matching uses the highest band with <c>AgeYears &lt;= age</c> (same idea as WML).
/// </summary>
public static class VacancyWageResolver
{
    /// <summary>
    /// Indicative youth fractions of the adult (21+) rate when a vacancy has no salary table.
    /// Roughly mirrors common Dutch youth / WML-style steps used in Jobsy demo data.
    /// </summary>
    private static readonly (int AgeYears, string Label, decimal FractionOfAdult)[] DefaultYouthFractions =
    [
        (15, "15", 0.31m),
        (16, "16", 0.36m),
        (17, "17", 0.41m),
        (18, "18", 0.55m),
        (19, "19", 0.66m),
        (20, "20", 0.79m),
        (21, "21+", 1.00m)
    ];

    public static decimal ResolveHourlyWage(
        decimal vacancyHourlyWage,
        IEnumerable<CompanySalaryRate>? rates,
        int ageYears)
    {
        var bands = rates?.ToList() ?? [];
        if (bands.Count == 0)
        {
            return vacancyHourlyWage;
        }

        var age = Math.Clamp(ageYears, 15, 99);
        var match = bands
            .Where(r => r.AgeYears <= age)
            .OrderByDescending(r => r.AgeYears)
            .FirstOrDefault();

        if (match is not null)
        {
            return match.HourlyRate;
        }

        return bands.OrderBy(r => r.AgeYears).First().HourlyRate;
    }

    public static IReadOnlyList<WageAgeBand> GetWageBands(
        decimal vacancyHourlyWage,
        IEnumerable<CompanySalaryRate>? rates)
    {
        var bands = rates?.OrderBy(r => r.AgeYears).ToList() ?? [];
        if (bands.Count > 0)
        {
            return bands
                .Select(r => new WageAgeBand(
                    r.AgeYears,
                    r.HourlyRate,
                    string.IsNullOrWhiteSpace(r.Label) ? r.AgeYears.ToString() : r.Label))
                .ToList();
        }

        return BuildDefaultYouthBands(vacancyHourlyWage);
    }

    public static IReadOnlyList<WageAgeBand> BuildDefaultYouthBands(decimal adultHourlyWage)
    {
        var adult = Math.Max(0.01m, adultHourlyWage);
        return DefaultYouthFractions
            .Select(f => new WageAgeBand(
                f.AgeYears,
                Math.Round(adult * f.FractionOfAdult, 2, MidpointRounding.AwayFromZero),
                f.Label))
            .ToList();
    }
}

public readonly record struct WageAgeBand(int AgeYears, decimal HourlyRate, string Label);
