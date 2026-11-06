using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
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

        if (AfterMatchCandidateSlotRules.MayPickOpenSlots(status)
            && summary.OpenSlotCount > 0)
        {
            return new CardAction(
                AfterMatchSollicitatiesUrls.PickSlot(summary.ApplicationId, returnUrl),
                culture["Apps.Ndm.PickSlot"],
                "action-pick");
        }

        if (status == InterviewStatus.RescheduleRequested)
        {
            return new CardAction(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
                culture["AfterMatch.Candidate.ProposalPending.CardAction"],
                "action-wait");
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

        if (AfterMatchCandidateSlotRules.MayPickOpenSlots(status)
            && summary.OpenSlotCount > 0)
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.PickSlot(summary.ApplicationId, returnUrl),
                culture["Apps.Ndm.PickSlot"],
                messagesHref,
                summary.UnreadMessageCount);
        }

        if (status == InterviewStatus.RescheduleRequested)
        {
            return new DetailActions(
                AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
                culture["AfterMatch.Candidate.ProposalPending.Title"],
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

        return new DetailActions(
            AfterMatchSollicitatiesUrls.AfterMatchBase(summary.ApplicationId, returnUrl),
            culture["AfterMatch.Link.Open"],
            messagesHref,
            summary.UnreadMessageCount);
    }

    internal static string? FormatListCardTimelineSummary(
        CultureState culture,
        ApplicationAfterMatchSummary summary)
    {
        var steps = BuildAfterMatchTimeline(culture, summary);
        var segments = new List<string>();
        foreach (var step in steps)
        {
            if (string.Equals(step.State, "Upcoming", StringComparison.OrdinalIgnoreCase)
                && step.OccurredAtUtc is null)
            {
                continue;
            }

            var subtitle = FormatTimelineStepSubtitle(culture, step);
            if (subtitle is null)
            {
                continue;
            }

            segments.Add($"{step.Label} {subtitle}");

            if (segments.Count >= 2)
            {
                break;
            }
        }

        return segments.Count == 0 ? null : string.Join(", ", segments);
    }

    internal static string? FormatTimelineStepSubtitle(CultureState culture, SollicitationTimelineStep step)
    {
        if (string.Equals(step.State, "Current", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(step.Key, "Plan", StringComparison.OrdinalIgnoreCase))
            {
                return culture["AfterMatch.Candidate.ProposalPending.CardAction"];
            }

            return culture["AfterMatch.Timeline.Now"];
        }

        if (step.OccurredAtUtc is DateTime at)
        {
            return string.Equals(step.Key, "Interview", StringComparison.OrdinalIgnoreCase)
                ? ApplicationsSsrData.FormatShortDateTime(culture, at)
                : ApplicationsSsrData.FormatShortDate(culture, at);
        }

        if (string.Equals(step.Key, "Outcome", StringComparison.OrdinalIgnoreCase)
            && string.Equals(step.State, "Upcoming", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Kb.Timeline.AfterInterview"];
        }

        return null;
    }

    internal static IReadOnlyList<SollicitationTimelineStep> BuildAfterMatchTimeline(
        CultureState culture,
        ApplicationAfterMatchSummary summary)
    {
        if (!Enum.TryParse<InterviewStatus>(summary.InterviewStatus, ignoreCase: true, out var status))
        {
            return [];
        }

        var yesAt = summary.RespondedAtUtc ?? summary.AcceptedAtUtc;
        DateTime? planAt = yesAt;
        if (status == InterviewStatus.Proposing && summary.OpenSlotCount > 0)
        {
            planAt = null;
        }
        else if (status == InterviewStatus.RescheduleRequested)
        {
            planAt = summary.LastMessageAtUtc ?? yesAt;
        }
        else if (status == InterviewStatus.Scheduled)
        {
            planAt = summary.LastMessageAtUtc ?? yesAt?.AddDays(2);
        }

        var cancelled = !string.IsNullOrWhiteSpace(summary.LastCancelReason);
        var planState = cancelled
            ? "Done"
            : status switch
            {
                InterviewStatus.Proposing when summary.OpenSlotCount > 0 => "Current",
                InterviewStatus.RescheduleRequested => "Done",
                InterviewStatus.Scheduled or InterviewStatus.Done or InterviewStatus.Cancelled => "Done",
                _ => "Upcoming"
            };
        var interviewState = cancelled
            ? "Done"
            : status switch
            {
                InterviewStatus.Scheduled => "Current",
                InterviewStatus.Done => "Done",
                InterviewStatus.RescheduleRequested => "Upcoming",
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
    DateTime? RespondedAtUtc,
    string? LastMessagePreview,
    bool LastMessageFromCandidate,
    DateTime? LastMessageAtUtc,
    string? LastCancelReason = null,
    bool? LastCancelByEmployer = null);
