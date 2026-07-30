using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves vacancy pay from a company salary table (age bands) or the flat hourly wage.
/// Age matching uses the highest band with <c>AgeYears &lt;= age</c> (same idea as WML).
/// </summary>
public static class VacancyWageResolver
{
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

        var age = AgeRules.ClampWorkingAge(ageYears);
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

    /// <summary>Adult (21+) hourly rate from a salary table, or null when empty.</summary>
    public static decimal? ResolveAdultHourlyWage(IEnumerable<CompanySalaryRate>? rates)
    {
        var bands = rates?.ToList() ?? [];
        if (bands.Count == 0)
        {
            return null;
        }

        var adult = bands
            .Where(r => r.AgeYears >= AgeRules.AdultAgeYears)
            .OrderBy(r => r.AgeYears)
            .FirstOrDefault();
        return adult?.HourlyRate
               ?? bands.OrderByDescending(r => r.AgeYears).First().HourlyRate;
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
        return YouthWageFractions.Default
            .Select(f => new WageAgeBand(
                f.AgeYears,
                Math.Round(adult * f.FractionOfAdult, 2, MidpointRounding.AwayFromZero),
                f.Label))
            .ToList();
    }
}

public readonly record struct WageAgeBand(int AgeYears, decimal HourlyRate, string Label);
