using System.Globalization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchProposeSlotParse
{
    internal sealed record Result(DateTime StartUtc, DateTime EndUtc, string? ErrorKey);

    internal static Result TryParse(string dateLabel, string timeWindow)
    {
        var dateText = (dateLabel ?? "").Trim();
        var timeText = (timeWindow ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dateText) || string.IsNullOrWhiteSpace(timeText))
        {
            return new Result(default, default, "AfterMatch.Pick.ProposeError.Required");
        }

        var nl = CultureInfo.GetCultureInfo("nl-NL");
        if (!DateTime.TryParse(dateText, nl, DateTimeStyles.AllowWhiteSpaces, out var dateOnly)
            && !DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateOnly))
        {
            return new Result(default, default, "AfterMatch.Pick.ProposeError.Date");
        }

        if (!TryParseTimeWindow(timeText, out var startLocal, out var endLocal))
        {
            return new Result(default, default, "AfterMatch.Pick.ProposeError.Time");
        }

        var start = dateOnly.Date.Add(startLocal.TimeOfDay);
        var end = dateOnly.Date.Add(endLocal.TimeOfDay);
        if (end <= start)
        {
            end = start.AddMinutes(30);
        }

        var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        startUtc = TimeZoneInfo.ConvertTimeToUtc(
            startUtc,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam"));
        var endUtc = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
        endUtc = TimeZoneInfo.ConvertTimeToUtc(
            endUtc,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam"));

        if (startUtc <= DateTime.UtcNow.AddMinutes(-5))
        {
            return new Result(default, default, "AfterMatch.Pick.ProposeError.Past");
        }

        return new Result(startUtc, endUtc, null);
    }

    private static bool TryParseTimeWindow(string timeText, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        var parts = timeText.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            if (!TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var single))
            {
                return false;
            }

            start = DateOnly.MinValue.ToDateTime(single);
            end = start.AddMinutes(30);
            return true;
        }

        if (parts.Length >= 2
            && TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var from)
            && TimeOnly.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
        {
            start = DateOnly.MinValue.ToDateTime(from);
            end = DateOnly.MinValue.ToDateTime(to);
            return true;
        }

        return false;
    }
}
