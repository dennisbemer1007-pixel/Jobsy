using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal sealed record AfterMatchMailConfirmationFields(
    string WhenLabel,
    string CandidateWhere,
    string WithWhomLabel,
    string TravelLabel,
    string EmployerWhere,
    string OutlookStart,
    string OutlookEnd,
    string OutlookLocation,
    string OutlookBodyPrimary,
    string OutlookRescheduleLine);

internal static class AfterMatchMailConfirmationFieldsBuilder
{
    internal static AfterMatchMailConfirmationFields Build(
        CultureState culture,
        AfterMatchThreadDto thread,
        Guid applicationId)
    {
        if (applicationId == AfterMatchMailSsrDemoData.ScheduledApplicationId)
        {
            return DemoMockup(culture, thread);
        }

        var nl = System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
        var startUtc = thread.ScheduledStartUtc ?? DateTime.UtcNow;
        var endUtc = thread.ScheduledEndUtc ?? startUtc.AddMinutes(30);
        var start = AfterMatchMailSsrTime.ToAmsterdam(startUtc);
        var end = AfterMatchMailSsrTime.ToAmsterdam(endUtc);

        var whenLong = start.ToString("dddd d MMMM, HH:mm", nl);
        if (whenLong.Length > 0 && char.IsLower(whenLong[0]))
        {
            whenLong = char.ToUpper(whenLong[0], nl) + whenLong[1..];
        }

        var whenLabel = $"{whenLong} tot {end:HH:mm}";
        var candidateWhere = thread.LocationOrLink ?? thread.CompanyName;
        var withWhom = thread.WithWhom ?? thread.CompanyName;
        var travel = thread.TravelLabel ?? "";
        var employerWhere = thread.Format == InterviewFormat.OnSite
            ? FormatEmployerOnSiteLocation(thread.LocationOrLink)
            : thread.LocationOrLink ?? thread.CompanyName;

        var outlookStart = start.ToString("ddd dd-MM-yyyy HH:mm", nl);
        var outlookEnd = end.ToString("ddd dd-MM-yyyy HH:mm", nl);
        var outlookLocation = thread.LocationOrLink ?? "";
        var outlookPrimary = string.Format(
            culture["AfterMatch.Mail.OutlookBody"],
            thread.VacancyTitle,
            thread.WithWhom ?? thread.CompanyName);
        var rescheduleLine = culture["AfterMatch.Mail.OutlookReschedule"];

        return new AfterMatchMailConfirmationFields(
            whenLabel,
            candidateWhere,
            withWhom,
            travel,
            employerWhere,
            outlookStart,
            outlookEnd,
            outlookLocation,
            outlookPrimary,
            rescheduleLine);
    }

    private static AfterMatchMailConfirmationFields DemoMockup(
        CultureState culture,
        AfterMatchThreadDto thread)
    {
        var outlookPrimary = string.Format(
            culture["AfterMatch.Mail.OutlookBody"],
            thread.VacancyTitle,
            "Linda de Wit");

        return new AfterMatchMailConfirmationFields(
            "Donderdag 22 oktober, 10:30 tot 11:00",
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit, eigenaar",
            "9 min fietsen of 18 min met tram 2",
            "Op locatie, Loosduinseweg 120",
            "do 22-10-2026 10:30",
            "do 22-10-2026 11:00",
            "Loosduinseweg 120, 2571 AH Den Haag",
            outlookPrimary,
            culture["AfterMatch.Mail.OutlookReschedule"]);
    }

    private static string FormatEmployerOnSiteLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Op locatie";
        }

        if (location.StartsWith("Op locatie", StringComparison.OrdinalIgnoreCase))
        {
            return location;
        }

        return "Op locatie, " + location;
    }
}
