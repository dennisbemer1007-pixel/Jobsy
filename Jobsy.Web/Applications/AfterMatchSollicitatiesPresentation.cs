using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchSollicitatiesPresentation
{
    internal sealed record CardAction(string Href, string Label, string Modifier);

    internal sealed record DetailActions(
        string PrimaryHref,
        string PrimaryLabel,
        string MessagesHref,
        int UnreadMessages);

    internal static CardAction? BuildCardAction(
        CultureState culture,
        ApplicationAfterMatchSummary summary,
        string? returnUrl = null)
    {
        if (!Enum.TryParse<InterviewStatus>(summary.InterviewStatus, ignoreCase: true, out var status))
        {
            return null;
        }

        if (status is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested
            && summary.OpenSlotCount > 0)
        {
            return new CardAction(
                AfterMatchSollicitatiesUrls.PickSlot(summary.ApplicationId, returnUrl),
                culture["Apps.Ndm.PickSlot"],
                "action-pick");
        }

        if (status == InterviewStatus.Scheduled && summary.ScheduledStartUtc is DateTime start)
        {
            var label = string.Format(
                culture["Apps.Ndm.InterviewPlannedShort"],
                ApplicationsSsrData.FormatShortDateTime(culture, start));
            return new CardAction(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId),
                label,
                "action-scheduled");
        }

        if (status == InterviewStatus.Done)
        {
            return new CardAction(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
                culture["AfterMatch.Screen13.PageTitle"],
                "action-outcome");
        }

        return null;
    }

    internal static DetailActions? BuildDetailActions(
        CultureState culture,
        ApplicationAfterMatchSummary summary,
        string? returnUrl = null)
    {
        if (!Enum.TryParse<InterviewStatus>(summary.InterviewStatus, ignoreCase: true, out var status))
        {
            return null;
        }

        var messagesHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabBerichten,
            ApplicationsSsrQuery.FilterAll,
            null,
            summary.ApplicationId);
        if (!string.IsNullOrWhiteSpace(summary.LastCancelReason))
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
                culture["AfterMatch.InterviewCancelled.DossierCta"],
                messagesHref,
                summary.UnreadMessageCount);
        }

        if (status is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested
            && summary.OpenSlotCount > 0)
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.PickSlot(summary.ApplicationId, returnUrl),
                culture["Apps.Ndm.PickSlot"],
                messagesHref,
                summary.UnreadMessageCount);
        }

        if (status == InterviewStatus.Scheduled)
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
                culture["Apps.Ndm.ViewInterview"],
                messagesHref,
                summary.UnreadMessageCount);
        }

        if (status == InterviewStatus.Done)
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
                culture["AfterMatch.Screen13.PageTitle"],
                messagesHref,
                summary.UnreadMessageCount);
        }

        if (status == InterviewStatus.RescheduleRequested)
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.Reschedule(summary.ApplicationId, returnUrl),
                culture["AfterMatch.Screen08.Reschedule"],
                messagesHref,
                summary.UnreadMessageCount);
        }

        return new DetailActions(
            AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
            culture["AfterMatch.Link.Open"],
            messagesHref,
            summary.UnreadMessageCount);
    }

    internal static IReadOnlyList<SollicitationTimelineStep> BuildAfterMatchTimeline(
        CultureState culture,
        ApplicationAfterMatchSummary summary)
    {
        if (!Enum.TryParse<InterviewStatus>(summary.InterviewStatus, ignoreCase: true, out var status))
        {
            return [];
        }

        var yesAt = summary.AcceptedAtUtc;
        DateTime? planAt = yesAt;
        if (status is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested && summary.OpenSlotCount > 0)
        {
            planAt = summary.LastMessageAtUtc ?? yesAt;
        }
        else if (summary.ScheduledStartUtc is DateTime scheduled)
        {
            planAt = scheduled;
        }

        var cancelled = !string.IsNullOrWhiteSpace(summary.LastCancelReason);
        var planState = cancelled
            ? "Done"
            : status switch
            {
                InterviewStatus.Proposing or InterviewStatus.RescheduleRequested => "Current",
                InterviewStatus.Scheduled or InterviewStatus.Done or InterviewStatus.Cancelled => "Done",
                _ => "Upcoming"
            };
        var interviewState = cancelled
            ? "Done"
            : status switch
            {
                InterviewStatus.Scheduled => "Current",
                InterviewStatus.Done => "Done",
                _ => "Upcoming"
            };
        var outcomeState = status == InterviewStatus.Done ? "Current" : "Upcoming";

        return
        [
            new("Yes", "Done", culture["AfterMatch.Timeline.Yes"], yesAt),
            new("Plan", planState, culture["AfterMatch.Timeline.Plan"], planAt),
            new("Interview", interviewState, culture["AfterMatch.Timeline.Interview"], summary.ScheduledStartUtc),
            new("Outcome", outcomeState, culture["AfterMatch.Timeline.Outcome"], null)
        ];
    }
}

internal sealed record ApplicationAfterMatchSummary(
    Guid ApplicationId,
    string InterviewStatus,
    int UnreadMessageCount,
    int OpenSlotCount,
    DateTime? ScheduledStartUtc,
    DateTime? ScheduledEndUtc,
    DateTime? AcceptedAtUtc,
    string? LastMessagePreview,
    bool LastMessageFromCandidate,
    DateTime? LastMessageAtUtc,
    string? LastCancelReason = null,
    bool? LastCancelByEmployer = null);
