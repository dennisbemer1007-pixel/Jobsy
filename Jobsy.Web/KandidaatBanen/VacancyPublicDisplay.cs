using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.KandidaatBanen;

public static class VacancyPublicDisplay
{
    private static readonly System.Text.Json.JsonSerializerOptions ScheduleJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string SanitizedDescriptionHtml(VacancyListItem? vacancy)
        => vacancy is null ? string.Empty : HtmlSanitize.ToVacancyDescriptionHtml(vacancy.Description);

    public static bool HasDescriptionContent(VacancyListItem? vacancy)
    {
        if (vacancy is null || string.IsNullOrWhiteSpace(vacancy.Description))
        {
            return false;
        }

        var html = SanitizedDescriptionHtml(vacancy);
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
        return text.Length > 0;
    }

    public static bool HasHoursInfo(VacancyListItem? vacancy)
        => vacancy is { MinHoursPerWeek: not null, MaxHoursPerWeek: not null };

    public static string HoursRangeText(VacancyListItem? vacancy)
        => vacancy is { MinHoursPerWeek: decimal min, MaxHoursPerWeek: decimal max }
            ? $"{min:0}–{max:0} uur"
            : string.Empty;

    public static decimal? DisplayHourlyWage(VacancyListItem? vacancy)
        => vacancy?.HourlyWage ?? vacancy?.WageByAge.FirstOrDefault()?.HourlyRate;

    public static IReadOnlyList<string> ScheduleChips(VacancyListItem? vacancy)
    {
        if (vacancy is null || vacancy.FlexibleTimes || string.IsNullOrWhiteSpace(vacancy.ScheduleJson))
        {
            return [];
        }

        try
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize<SchedulePayload>(
                vacancy.ScheduleJson,
                ScheduleJsonOptions);
            if (payload?.Slots is null || payload.Slots.Count == 0)
            {
                return [];
            }

            return payload.Slots
                .OrderBy(kv => Array.IndexOf(DayPartMatrix.DayCodes, DayPartMatrix.NormalizeDayCode(kv.Key)))
                .SelectMany(kv => (kv.Value ?? [])
                    .OrderBy(p => Array.IndexOf(DayPartMatrix.DayPartCodes, DayPartMatrix.NormalizeDayPartCode(p)))
                    .Select(p => $"{DayPartMatrix.NormalizeDayCode(kv.Key)} {DayPartMatrix.NormalizeDayPartCode(p)}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public static string DetailWhenLabel(VacancyListItem? vacancy, Func<string, string> localize)
    {
        if (vacancy is null)
        {
            return "—";
        }

        if (vacancy.FlexibleTimes)
        {
            return localize("Kb.List.DayFlexible");
        }

        var chips = ScheduleChips(vacancy);
        if (chips.Count == 0)
        {
            return "—";
        }

        return string.Join(", ", chips.Take(4)) + (chips.Count > 4 ? "…" : "");
    }

    private sealed class SchedulePayload
    {
        public Dictionary<string, string[]>? Slots { get; set; }
    }
}
