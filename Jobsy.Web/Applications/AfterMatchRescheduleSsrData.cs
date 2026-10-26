using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchRescheduleSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string SollicitatiesHref,
        string ConfirmedHref,
        string ProposeSlotHref,
        string ContactShort,
        string HeroLead,
        string ScheduledTitle,
        string ScheduledSub,
        string YesDateLabel,
        string PlanDateLabel,
        string InterviewDateLabel,
        string? AlternateSlotLabel,
        Guid? AlternateSlotId);

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

        var startLocal = AfterMatchMailSsrTime.ToAmsterdam(startUtc);
        var nl = System.Globalization.CultureInfo.GetCultureInfo("nl-NL");

        var whenLong = startLocal.ToString("dddd d MMMM, HH:mm", nl);
        if (whenLong.Length > 0 && char.IsLower(whenLong[0]))
        {
            whenLong = char.ToUpper(whenLong[0], nl) + whenLong[1..];
        }

        var scheduledTitle = whenLong;
        var venueLabel = string.IsNullOrWhiteSpace(thread.LocationOrLink) ? thread.CompanyName : thread.LocationOrLink!;
        var scheduledSub = thread.Format == InterviewFormat.OnSite
            ? string.Format(culture["AfterMatch.Screen08.ScheduledSubOnSite"], venueLabel)
            : string.Format(culture["AfterMatch.Screen08.ScheduledSubOnline"], venueLabel);

        var contactShort = ContactShortName(thread);
        var heroLead = string.Format(culture["AfterMatch.Screen08.Hero.LeadDetail"], contactShort);

        var yesDate = thread.AcceptedAtUtc is DateTime acceptedAt
            ? AfterMatchMailSsrTime.ToAmsterdam(acceptedAt).ToString("d MMM", nl)
            : "13 okt";
        var planDate = startLocal.AddDays(-7).ToString("d MMM", nl);
        var interviewDate = AfterMatchTimelineLabels.InterviewStep(thread);

        string? alternateSlotLabel = null;
        Guid? alternateSlotId = null;
        if (thread.OpenSlots.Count > 0)
        {
            var slot = thread.OpenSlots
                .OrderBy(s => s.StartUtc)
                .FirstOrDefault(s => s.StartUtc != startUtc)
                ?? thread.OpenSlots.OrderBy(s => s.StartUtc).First();
            alternateSlotId = slot.Id;
            var slotStart = AfterMatchMailSsrTime.ToAmsterdam(slot.StartUtc);
            var slotLabel = slotStart.ToString("dddd d MMMM, HH:mm", nl);
            if (slotLabel.Length > 0 && char.IsLower(slotLabel[0]))
            {
                slotLabel = char.ToUpper(slotLabel[0], nl) + slotLabel[1..];
            }

            alternateSlotLabel = string.Format(
                culture["AfterMatch.Screen08.AlternateSlotHint"],
                slotLabel);
        }

        return new PageData(
            thread,
            backHref,
            AfterMatchSollicitatiesUrls.AfterMatchBase(applicationId, backHref),
            AfterMatchSollicitatiesUrls.ProposeSlot(applicationId, backHref),
            contactShort,
            heroLead,
            scheduledTitle,
            scheduledSub,
            yesDate,
            planDate,
            interviewDate,
            alternateSlotLabel,
            alternateSlotId);
    }

    private static string ContactShortName(AfterMatchThreadDto thread)
    {
        if (!string.IsNullOrWhiteSpace(thread.WithWhom))
        {
            var first = thread.WithWhom.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return thread.CompanyName;
    }
}
