using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchConfirmedSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string SollicitatiesHref,
        string RescheduleHref,
        string CancelHref,
        string? RouteHref,
        string CalendarIcsHref,
        string CalendarGoogleHref,
        string CalendarOutlookHref,
        string TipsHref,
        string AssistantHref,
        string HeroLead,
        string ScheduledTitle,
        string ScheduledSub,
        string YesDateLabel,
        string PlanDateLabel,
        string InterviewDateLabel);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        Guid applicationId,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var thread = await api.GetAfterMatchForCandidateAsync(applicationId, cancellationToken);
        if (thread is null
            || thread.InterviewStatus != InterviewStatus.Scheduled
            || thread.Outcome == AfterMatchOutcomeKind.NotNow
            || thread.ScheduledStartUtc is not DateTime startUtc)
        {
            return null;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var endUtc = thread.ScheduledEndUtc ?? startUtc.AddMinutes(30);
        var startLocal = AfterMatchMailSsrTime.ToAmsterdam(startUtc);
        var endLocal = AfterMatchMailSsrTime.ToAmsterdam(endUtc);
        var nl = System.Globalization.CultureInfo.GetCultureInfo("nl-NL");

        var whenLong = startLocal.ToString("dddd d MMMM, HH:mm", nl);
        if (char.IsLower(whenLong[0]))
        {
            whenLong = char.ToUpper(whenLong[0], nl) + whenLong[1..];
        }

        var scheduledTitle = $"{whenLong} tot {endLocal:HH:mm}";
        var contact = string.IsNullOrWhiteSpace(thread.WithWhom) ? thread.CompanyName : thread.WithWhom!;
        var venueLabel = string.IsNullOrWhiteSpace(thread.LocationOrLink) ? thread.CompanyName : thread.LocationOrLink!;
        var scheduledSub = thread.Format == InterviewFormat.OnSite
            ? string.Format(culture["AfterMatch.Screen05.ScheduledSub"], venueLabel, contact)
            : string.Format(culture["AfterMatch.Screen05.ScheduledSubOnline"], venueLabel, contact);

        var heroWhen = startLocal.ToString("dddd d MMMM 'om' HH:mm", nl);
        if (char.IsLower(heroWhen[0]))
        {
            heroWhen = char.ToUpper(heroWhen[0], nl) + heroWhen[1..];
        }

        var heroLead = string.Format(
            culture["AfterMatch.Screen05.Hero.LeadDetail"],
            heroWhen,
            thread.CompanyName,
            contact);

        var yesDate = thread.AcceptedAtUtc is DateTime acceptedAt
            ? AfterMatchMailSsrTime.ToAmsterdam(acceptedAt).ToString("d MMM", nl)
            : "13 okt";
        var planDate = startLocal.AddDays(-7).ToString("d MMM", nl);
        var interviewDate = startLocal.ToString("d MMM", nl);

        string? routeHref = null;
        if (thread.Format == InterviewFormat.OnSite && !string.IsNullOrWhiteSpace(thread.LocationOrLink))
        {
            routeHref = "https://www.google.com/maps/search/?api=1&query="
                + Uri.EscapeDataString(thread.LocationOrLink);
        }

        var summary = $"{thread.VacancyTitle} · {thread.CompanyName}";
        var details = thread.Note ?? "Kennismakingsgesprek via Lobsy";
        var calLocation = thread.Format == InterviewFormat.OnSite ? thread.LocationOrLink : thread.LocationOrLink ?? "Online";
        var googleHref = InterviewCalendarBuilder.GoogleCalendarUrl(
            startUtc,
            endUtc,
            summary,
            details,
            calLocation);
        var outlookHref = InterviewCalendarBuilder.OutlookWebCalendarUrl(
            startUtc,
            endUtc,
            summary,
            details,
            calLocation);

        const string candidateHow = "/candidate/hoe-werkt-lobsy";

        return new PageData(
            thread,
            backHref,
            AfterMatchSollicitatiesUrls.Reschedule(applicationId, backHref),
            AfterMatchSollicitatiesUrls.Cancel(applicationId, backHref),
            routeHref,
            $"/api/ndm-confirmed-client/calendar.ics?applicationId={applicationId:D}",
            googleHref,
            outlookHref,
            candidateHow,
            candidateHow,
            heroLead,
            scheduledTitle,
            scheduledSub,
            yesDate,
            planDate,
            interviewDate);
    }
}
