using System.Globalization;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class InterviewCalendarBuilder
{
    public static byte[] BuildIcs(
        Interview interview,
        string organizerEmail,
        string attendeeEmail,
        string rescheduleUrl,
        string summary,
        string description)
    {
        if (interview.ChosenSlot is null)
        {
            throw new InvalidOperationException("Interview has no chosen slot.");
        }

        var uid = string.IsNullOrWhiteSpace(interview.CalendarUid)
            ? $"interview-{interview.Id:N}@lobsy.nl"
            : interview.CalendarUid;

        var start = interview.ChosenSlot.StartUtc;
        var end = interview.ChosenSlot.EndUtc;
        var stamp = DateTime.UtcNow;

        var location = interview.Format switch
        {
            InterviewFormat.OnSite => interview.LocationOrLink ?? "",
            InterviewFormat.Online => interview.LocationOrLink ?? "Online",
            InterviewFormat.Phone => "Telefoon",
            _ => interview.LocationOrLink ?? ""
        };

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Lobsy//Na de match//NL");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:REQUEST");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}");
        sb.AppendLine($"DTSTAMP:{FormatUtc(stamp)}");
        sb.AppendLine($"DTSTART:{FormatUtc(start)}");
        sb.AppendLine($"DTEND:{FormatUtc(end)}");
        sb.AppendLine($"SUMMARY:{Escape(summary)}");
        sb.AppendLine($"DESCRIPTION:{Escape(description + "\\n\\nVerzetten: " + rescheduleUrl)}");
        if (!string.IsNullOrWhiteSpace(location))
        {
            sb.AppendLine($"LOCATION:{Escape(location)}");
        }

        sb.AppendLine($"ORGANIZER;CN=Lobsy:mailto:{organizerEmail}");
        sb.AppendLine($"ATTENDEE;CN=Deelnemer;RSVP=TRUE:mailto:{attendeeEmail}");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static string GoogleCalendarUrl(
        DateTime startUtc,
        DateTime endUtc,
        string title,
        string details,
        string? location)
    {
        var start = FormatGoogle(startUtc);
        var end = FormatGoogle(endUtc);
        var loc = Uri.EscapeDataString(location ?? "");
        var text = Uri.EscapeDataString(title);
        var det = Uri.EscapeDataString(details);
        return $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={text}&dates={start}/{end}&details={det}&location={loc}";
    }

    public static string OutlookWebCalendarUrl(
        DateTime startUtc,
        DateTime endUtc,
        string title,
        string details,
        string? location)
    {
        var start = Uri.EscapeDataString(FormatOutlookWeb(startUtc));
        var end = Uri.EscapeDataString(FormatOutlookWeb(endUtc));
        var subject = Uri.EscapeDataString(title);
        var body = Uri.EscapeDataString(details);
        var loc = Uri.EscapeDataString(location ?? "");
        return "https://outlook.office.com/calendar/action/compose"
            + "?allday=false"
            + $"&body={body}"
            + $"&enddt={end}"
            + $"&location={loc}"
            + "&path=/calendar/action/compose"
            + "&rru=addevent"
            + $"&startdt={start}"
            + $"&subject={subject}";
    }

    private static string FormatUtc(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string FormatGoogle(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string FormatOutlookWeb(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
