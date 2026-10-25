using System.Globalization;
using System.Net;
using System.Text;
namespace Jobsy.Web.Applications;

internal static class NdMWeekCalendarSsr
{
    internal static readonly DateTime DefaultWeekStartUtc = new(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

    internal static void AppendWeekCalendar(
        StringBuilder sb,
        DateTime weekStartUtc,
        IReadOnlyCollection<DateTime> selectedStartsUtc)
    {
        var selected = new HashSet<DateTime>(selectedStartsUtc);
        var weekLabel =
            $"{weekStartUtc.ToLocalTime():d MMM} – {weekStartUtc.AddDays(4).ToLocalTime():d MMM yyyy}";

        sb.Append("<div class=\"ndm-week\" data-ndm-week-calendar data-week-start=\"");
        sb.Append(WebUtility.HtmlEncode(weekStartUtc.ToString("O")));
        sb.Append("\"><div class=\"ndm-week__head\"><button type=\"button\" class=\"btn line sm\" data-ndm-week-prev aria-label=\"Vorige week\">‹</button><b data-ndm-week-label>");
        sb.Append(WebUtility.HtmlEncode(weekLabel));
        sb.Append("</b><button type=\"button\" class=\"btn line sm\" data-ndm-week-next aria-label=\"Volgende week\">›</button></div><div class=\"ndm-week__grid\" data-ndm-week-grid>");

        foreach (var day in WeekDays(weekStartUtc))
        {
            sb.Append("<div class=\"ndm-week__col\"><span class=\"lbl\">");
            sb.Append(WebUtility.HtmlEncode(day.ToLocalTime().ToString(
                "ddd d MMM",
                CultureInfo.GetCultureInfo("nl-NL"))));
            sb.Append("</span>");
            foreach (var slot in SlotsForDay(day))
            {
                var on = selected.Contains(slot) ? " on" : "";
                sb.Append("<button type=\"button\" class=\"chip");
                sb.Append(on);
                sb.Append("\" data-ndm-week-slot=\"");
                sb.Append(WebUtility.HtmlEncode(slot.ToString("O")));
                sb.Append("\">");
                sb.Append(WebUtility.HtmlEncode(slot.ToLocalTime().ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture)));
                sb.Append("</button>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div></div>");
    }

    internal static IEnumerable<DateTime> WeekDays(DateTime weekStartUtc) =>
        Enumerable.Range(0, 5).Select(i => weekStartUtc.AddDays(i));

    internal static IEnumerable<DateTime> SlotsForDay(DateTime dayUtc)
    {
        var local = dayUtc.ToLocalTime().Date;
        foreach (var hour in new[] { 9, 10, 11, 14, 15 })
        {
            yield return DateTime.SpecifyKind(local.AddHours(hour), DateTimeKind.Local).ToUniversalTime();
        }
    }
}
