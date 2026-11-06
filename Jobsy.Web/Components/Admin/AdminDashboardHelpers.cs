using System.Globalization;

namespace Jobsy.Web.Components.Admin;

public static class AdminRelativeTime
{
    public static string Format(DateTime sinceUtc, DateTime? utcNow = null, CultureInfo? culture = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var since = sinceUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc)
            : sinceUtc.ToUniversalTime();
        var span = now - since;
        culture ??= CultureInfo.CurrentUICulture;

        if (span.TotalMinutes < 1)
        {
            return "Nu";
        }

        if (span.TotalHours < 1)
        {
            var m = Math.Max(1, (int)span.TotalMinutes);
            return $"{m} m";
        }

        if (span.TotalHours < 24)
        {
            var h = Math.Max(1, (int)span.TotalHours);
            return $"{h} u";
        }

        if (span.TotalDays < 2)
        {
            return culture.TwoLetterISOLanguageName == "nl" ? "Gisteren" : "Yesterday";
        }

        if (span.TotalDays < 7)
        {
            var d = (int)span.TotalDays;
            return $"{d} d";
        }

        return since.ToLocalTime().ToString("d MMM", culture);
    }
}

public static class AdminKpiDelta
{
    public static (string Text, string Tone) Format(decimal value, decimal? previous)
    {
        if (previous is null)
        {
            return ("", "neutral");
        }

        var prev = previous.Value;
        var delta = value - prev;
        if (prev == 0 && delta == 0)
        {
            return ("0%", "neutral");
        }

        if (prev == 0)
        {
            var tone = delta > 0 ? "up" : "down";
            var arrow = delta > 0 ? "↑" : "↓";
            return ($"{arrow} nieuw", tone);
        }

        var pct = Math.Round(100m * delta / Math.Abs(prev), 1);
        var sign = pct > 0 ? "+" : "";
        var arrow2 = pct > 0 ? "↑" : pct < 0 ? "↓" : "";
        var tone2 = pct > 0 ? "up" : pct < 0 ? "down" : "neutral";
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        return ($"{arrow2} {sign}{pct.ToString("0.#", culture)}% vs vorige", tone2);
    }

    public static (string Text, string Tone) FormatAbsolute(decimal delta, string? context = null)
    {
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var tone = delta > 0 ? "up" : delta < 0 ? "down" : "neutral";
        var arrow = delta > 0 ? "↑" : delta < 0 ? "↓" : "";
        var sign = delta > 0 ? "+" : "";
        var suffix = string.IsNullOrWhiteSpace(context) ? "" : $" {context}";
        return ($"{arrow} {sign}{delta.ToString("0.#", culture)}{suffix}", tone);
    }
}
