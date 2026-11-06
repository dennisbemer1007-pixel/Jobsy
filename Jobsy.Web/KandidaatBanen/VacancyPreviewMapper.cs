using System.Text.Json;
using Jobsy.Core.Media;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Build a candidate-facing vacancy model from employer create-form state (unsaved preview).</summary>
public static class VacancyPreviewMapper
{
    public static VacancyListItem FromForm(
        CompanySummary? company,
        VacancyCategoryItem? category,
        SalaryTableItem? salaryTable,
        string title,
        string descriptionHtml,
        string? imageUrl,
        string? videoUrl,
        decimal? minHours,
        decimal? maxHours,
        bool flexibleTimes,
        IReadOnlyCollection<string> scheduleSlotKeys,
        IReadOnlyCollection<string> workTypes,
        Guid? editVacancyId)
    {
        var adultRate = salaryTable?.Rates
            .Where(r => r.AgeYears >= 21)
            .OrderBy(r => r.AgeYears)
            .FirstOrDefault()
            ?? salaryTable?.Rates.OrderByDescending(r => r.AgeYears).FirstOrDefault();

        var scheduleJson = flexibleTimes ? null : BuildScheduleJson(scheduleSlotKeys);

        return new VacancyListItem
        {
            Id = editVacancyId ?? Guid.Empty,
            Title = string.IsNullOrWhiteSpace(title) ? "Functietitel…" : title.Trim(),
            Description = HtmlSanitize.ToVacancyDescriptionHtml(descriptionHtml),
            HourlyWage = adultRate?.HourlyRate,
            WageVisible = adultRate is not null,
            WageByAge = salaryTable?.Rates.Select(r => new WageByAgeItem
            {
                AgeYears = r.AgeYears,
                Label = r.Label,
                HourlyRate = r.HourlyRate
            }).ToList() ?? [],
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today).AddMonths(3),
            Status = "Draft",
            CompanyId = company?.Id ?? Guid.Empty,
            CompanyName = company?.Name ?? "Vestiging",
            CompanyAddress = company?.Address ?? "",
            CompanyLogoUrl = null,
            ImageUrl = VacancyImageUrls.Normalize(imageUrl),
            VideoUrl = HtmlSanitize.NormalizeMediaUrl(videoUrl),
            SalaryTableId = salaryTable?.Id,
            WorkTypes = workTypes.ToArray(),
            MinHoursPerWeek = minHours,
            MaxHoursPerWeek = maxHours,
            FlexibleTimes = flexibleTimes,
            ScheduleJson = scheduleJson,
            CategoryId = category?.Id,
            CategoryName = category?.Name,
            CategoryColorHex = category?.ColorHex,
            Kind = category?.PlacementKind ?? "Regular",
            IsPreview = true
        };
    }

    private static string? BuildScheduleJson(IReadOnlyCollection<string> slotKeys)
    {
        if (slotKeys.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in slotKeys
                     .Select(k => k.Split(':', 2))
                     .Where(p => p.Length == 2)
                     .GroupBy(p => p[0], StringComparer.OrdinalIgnoreCase))
        {
            result[group.Key] = group.Select(p => p[1]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return JsonSerializer.Serialize(new { slots = result });
    }
}
