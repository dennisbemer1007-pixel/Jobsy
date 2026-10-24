using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

/// <summary>List-card Na de match actions (mockup 01 sollicitaties.html).</summary>
internal static class AfterMatchSollicitatiesPresentation
{
    internal sealed record CardAction(string Href, string Label, string Modifier);

    internal static CardAction? BuildCardAction(CultureState culture, ApplicationAfterMatchSummary summary)
    {
        if (!Enum.TryParse<InterviewStatus>(summary.InterviewStatus, ignoreCase: true, out var status))
        {
            return null;
        }

        if (status is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested
            && summary.OpenSlotCount > 0)
        {
            return new CardAction(
                AfterMatchSollicitatiesUrls.PickSlot(summary.ApplicationId),
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

        return null;
    }
}

internal sealed record ApplicationAfterMatchSummary(
    Guid ApplicationId,
    string InterviewStatus,
    int UnreadMessageCount,
    int OpenSlotCount,
    DateTime? ScheduledStartUtc,
    DateTime? ScheduledEndUtc,
    DateTime? AcceptedAtUtc);
