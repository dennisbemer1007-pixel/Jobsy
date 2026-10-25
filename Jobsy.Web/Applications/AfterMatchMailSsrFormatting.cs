using Jobsy.Core.Contracts;

namespace Jobsy.Web.Applications;

internal static class AfterMatchMailSsrFormatting
{
    internal static string CandidateFirstName(AfterMatchThreadDto thread) =>
        thread.CandidateName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
        ?? thread.CandidateName;

    internal static string WhenLabel(AfterMatchThreadDto thread)
    {
        if (thread.ScheduledStartUtc is not DateTime start || thread.ScheduledEndUtc is not DateTime end)
        {
            return "Donderdag 22 oktober, 10:30 tot 11:00";
        }

        var s = start.ToLocalTime();
        var e = end.ToLocalTime();
        var nl = System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
        var whenLong = s.ToString("dddd d MMMM, HH:mm", nl);
        if (whenLong.Length > 0 && char.IsLower(whenLong[0]))
        {
            whenLong = char.ToUpper(whenLong[0], nl) + whenLong[1..];
        }

        return $"{whenLong} tot {e:HH:mm}";
    }

    internal static string OutlookStart(AfterMatchThreadDto thread) =>
        thread.ScheduledStartUtc?.ToLocalTime().ToString(
            "ddd dd-MM-yyyy HH:mm",
            System.Globalization.CultureInfo.GetCultureInfo("nl-NL"))
        ?? "do 22-10-2026 10:30";

    internal static string OutlookEnd(AfterMatchThreadDto thread) =>
        thread.ScheduledEndUtc?.ToLocalTime().ToString(
            "ddd dd-MM-yyyy HH:mm",
            System.Globalization.CultureInfo.GetCultureInfo("nl-NL"))
        ?? "do 22-10-2026 11:00";
}
