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
        if (bands.Count == 0)
        {
            return
            [
                new WageAgeBand(21, vacancyHourlyWage, "Alle")
            ];
        }

        return bands
            .Select(r => new WageAgeBand(
                r.AgeYears,
                r.HourlyRate,
                string.IsNullOrWhiteSpace(r.Label) ? r.AgeYears.ToString() : r.Label))
            .ToList();
    }
}

public readonly record struct WageAgeBand(int AgeYears, decimal HourlyRate, string Label);
