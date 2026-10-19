using Jobsy.Core.Enums;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Core.Rules;

public static class WorkInitiativeTimelineBuilder
{
    public static ApplicationTimeline Build(
        DateTime createdAtUtc,
        WorkInitiativeStatus status,
        DateTime? employerViewedAtUtc,
        DateTime? respondedAtUtc)
    {
        var steps = new List<ApplicationTimelineStep>(3);
        steps.Add(new ApplicationTimelineStep(
            ApplicationTimelineStepKey.Sent,
            ApplicationTimelineStepState.Done,
            createdAtUtc,
            ApplicationTimelineBuilder.LabelSent));

        var seen = employerViewedAtUtc is not null || status is not WorkInitiativeStatus.Pending;
        steps.Add(new ApplicationTimelineStep(
            ApplicationTimelineStepKey.Seen,
            seen ? ApplicationTimelineStepState.Done : ApplicationTimelineStepState.Upcoming,
            employerViewedAtUtc,
            ApplicationTimelineBuilder.LabelSeen));

        var terminal = status is WorkInitiativeStatus.Accepted
            or WorkInitiativeStatus.Declined
            or WorkInitiativeStatus.AutoDeclined
            or WorkInitiativeStatus.Withdrawn;
        steps.Add(new ApplicationTimelineStep(
            ApplicationTimelineStepKey.Outcome,
            terminal ? ApplicationTimelineStepState.Done : ApplicationTimelineStepState.Upcoming,
            respondedAtUtc,
            ApplicationTimelineBuilder.LabelOutcome));

        string? closing = status == WorkInitiativeStatus.AutoDeclined
            ? "Bk.AutoDecline.Lead"
            : null;

        return new ApplicationTimeline(steps, closing, respondedAtUtc, LegacyNoHistory: false);
    }
}
