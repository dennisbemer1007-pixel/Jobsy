using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>In-memory filters for the banenkaart snapshot (same rules as HTTP discover).</summary>
public static class VacancyDiscoveryQuery
{
    public static IEnumerable<VacancyDiscoveryRecord> Filter(
        IEnumerable<VacancyDiscoveryRecord> records,
        IReadOnlyCollection<Guid>? companyIds = null,
        IReadOnlyCollection<Guid>? categoryIds = null,
        bool? suitableFor65Plus = null,
        IEnumerable<string>? workTypes = null,
        string? searchQuery = null,
        int minHoursPerWeek = 0,
        int maxHoursPerWeek = 40)
    {
        var companyFilter = companyIds is { Count: > 0 }
            ? companyIds.Where(id => id != Guid.Empty).ToHashSet()
            : null;
        var categoryFilter = categoryIds is { Count: > 0 }
            ? categoryIds.Where(id => id != Guid.Empty).ToHashSet()
            : null;

        foreach (var record in records)
        {
            if (companyFilter is { Count: > 0 } && !companyFilter.Contains(record.CompanyId))
            {
                continue;
            }

            if (!VacancyCategoryDefaults.MatchesSelectedCategories(
                    record.CategoryId,
                    record.SuitableFor65Plus,
                    categoryFilter))
            {
                continue;
            }

            if (suitableFor65Plus == true
                && !VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(
                    record.CategoryId,
                    record.SuitableFor65Plus))
            {
                continue;
            }

            if (!WorkTypeLabels.MatchesFilter(record.WorkTypes, record.WorkTypeLabels, workTypes))
            {
                continue;
            }

            if (!VacancyTextSearch.MatchesText(
                    record.Title,
                    record.Description,
                    record.WorkTypeLabels,
                    record.RequiredDrivingLicense,
                    record.RequiredEducation,
                    searchQuery,
                    companyName: record.CompanyName,
                    intermediaryName: record.OfferedByLabel))
            {
                continue;
            }

            if (!HoursRangeRules.MatchesFilter(
                    record.MinHoursPerWeek,
                    record.MaxHoursPerWeek,
                    minHoursPerWeek,
                    maxHoursPerWeek))
            {
                continue;
            }

            yield return record;
        }
    }

    public static bool MatchesTransport(VacancyDiscoveryRecord record, string transport)
        => TransportLabels.MatchesRequired(record.RequiredTransportLabels, transport);

    public static bool MatchesWageFilter(
        decimal? hourlyWage,
        decimal? minHourlyWage,
        decimal? maxHourlyWage)
    {
        if (minHourlyWage is null && maxHourlyWage is null)
        {
            return true;
        }

        if (hourlyWage is not decimal wage)
        {
            return false;
        }

        if (minHourlyWage is not null && wage < minHourlyWage)
        {
            return false;
        }

        return maxHourlyWage is null || wage <= maxHourlyWage;
    }

    public static decimal? ResolveHourlyWage(VacancyDiscoveryRecord record, int? ageYears)
    {
        if (ageYears is not int age)
        {
            return null;
        }

        return VacancyWageResolver.ResolveHourlyWage(
            record.HourlyWage,
            record.SalaryRates.Count == 0
                ? null
                : record.SalaryRates.Select(b => new Entities.CompanySalaryRate
                {
                    AgeYears = b.AgeYears,
                    HourlyRate = b.HourlyRate,
                    Label = b.Label
                }),
            age);
    }
}
