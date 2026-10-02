using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules.KandidaatBanen;

public enum ApplicationTimelineStepState
{
    Done = 0,
    Current = 1,
    Upcoming = 2,
    Skipped = 3
}

public enum ApplicationTimelineStepKey
{
    Sent = 0,
    Seen = 1,
    Interview = 2,
    Outcome = 3
}

public sealed record ApplicationTimelineStep(
    ApplicationTimelineStepKey Key,
    ApplicationTimelineStepState State,
    DateTime? OccurredAtUtc,
    string LabelKey);

public sealed record ApplicationTimeline(
    IReadOnlyList<ApplicationTimelineStep> Steps,
    string? ClosingLineKey,
    DateTime? ClosingAtUtc,
    bool LegacyNoHistory);

/// <summary>
/// Pure timeline for candidate Sollicitaties (D4). Never invents missing history dates.
/// </summary>
public static class ApplicationTimelineBuilder
{
    public const string LabelSent = "Kb.Timeline.Sent";
    public const string LabelSeen = "Kb.Timeline.Seen";
    public const string LabelInterview = "Kb.Timeline.Interview";
    public const string LabelOutcome = "Kb.Timeline.Outcome";
    public const string LabelWithdrawn = "Kb.Timeline.WithdrawnOn";
    public const string LabelLegacySub = "Kb.Timeline.LegacySub";

    public static ApplicationTimeline Build(
        DateTime createdAt,
        ApplicationStatus status,
        DateTime? respondedAt,
        IReadOnlyList<ApplicationStatusHistory> history)
    {
        history ??= Array.Empty<ApplicationStatusHistory>();
        var ordered = history.OrderBy(h => h.OccurredAtUtc).ThenBy(h => h.Kind).ToList();
        var hasHistory = ordered.Count > 0;

        if (!hasHistory)
        {
            return BuildLegacy(createdAt, status, respondedAt);
        }

        if (status == ApplicationStatus.Withdrawn)
        {
            return BuildWithdrawn(createdAt, respondedAt, ordered);
        }

        var sentAt = FirstKind(ordered, ApplicationStatusEventKind.Created)?.OccurredAtUtc ?? createdAt;
        var seenAt = FirstKind(ordered, ApplicationStatusEventKind.EmployerViewed)?.OccurredAtUtc
                     ?? FirstChangeTo(ordered, ApplicationStatus.Accepted)?.OccurredAtUtc;
        var interviewAt = FirstChangeTo(ordered, ApplicationStatus.EmployerContacting)?.OccurredAtUtc;
        var outcomeAt = FirstChangeToAny(
            ordered,
            ApplicationStatus.Hired,
            ApplicationStatus.Rejected,
            ApplicationStatus.FilledElsewhere)?.OccurredAtUtc;

        var steps = new ApplicationTimelineStep[4];
        steps[0] = Step(ApplicationTimelineStepKey.Sent, LabelSent, sentAt, done: true);

        var outcomeStatuses = status is ApplicationStatus.Hired
            or ApplicationStatus.Rejected
            or ApplicationStatus.FilledElsewhere;
        var interviewDone = interviewAt.HasValue || outcomeStatuses;
        var seenDone = seenAt.HasValue || interviewDone;

        steps[1] = ResolveStep(
            ApplicationTimelineStepKey.Seen,
            LabelSeen,
            seenAt,
            done: seenDone,
            current: status is ApplicationStatus.Pending or ApplicationStatus.Accepted && seenAt.HasValue
                     && !interviewDone);

        // Pending before any view: Seen is current only when viewed; otherwise Sent stays current.
        if (status == ApplicationStatus.Pending && !seenAt.HasValue)
        {
            steps[0] = steps[0] with { State = ApplicationTimelineStepState.Current };
            steps[1] = steps[1] with { State = ApplicationTimelineStepState.Upcoming };
        }
        else if (status == ApplicationStatus.Accepted && !seenAt.HasValue && !interviewDone)
        {
            // Accepted without a recorded view still advances past Sent.
            steps[0] = steps[0] with { State = ApplicationTimelineStepState.Done };
            steps[1] = Step(ApplicationTimelineStepKey.Seen, LabelSeen, respondedAt, done: false)
                with { State = ApplicationTimelineStepState.Current };
        }

        steps[2] = ResolveStep(
            ApplicationTimelineStepKey.Interview,
            LabelInterview,
            interviewAt,
            done: interviewDone && (interviewAt.HasValue || outcomeStatuses),
            current: status == ApplicationStatus.EmployerContacting,
            skipped: outcomeStatuses && !interviewAt.HasValue);

        if (status == ApplicationStatus.EmployerContacting)
        {
            steps[2] = Step(ApplicationTimelineStepKey.Interview, LabelInterview, interviewAt, done: false)
                with { State = ApplicationTimelineStepState.Current };
        }

        var outcomeState = outcomeStatuses
            ? ApplicationTimelineStepState.Done
            : ApplicationTimelineStepState.Upcoming;
        if (status is ApplicationStatus.Rejected or ApplicationStatus.FilledElsewhere)
        {
            outcomeState = ApplicationTimelineStepState.Skipped;
        }

        steps[3] = new ApplicationTimelineStep(
            ApplicationTimelineStepKey.Outcome,
            outcomeState,
            outcomeAt,
            LabelOutcome);

        if (status == ApplicationStatus.Hired)
        {
            steps[3] = steps[3] with { State = ApplicationTimelineStepState.Done };
        }

        return new ApplicationTimeline(steps, ClosingLineKey: null, ClosingAtUtc: null, LegacyNoHistory: false);
    }

    /// <summary>"Wat nu?" key for the candidate UI.</summary>
    public static string? NextStepKey(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Pending => "Kb.Next.Pending",
        ApplicationStatus.Accepted => "Kb.Next.Accepted",
        ApplicationStatus.EmployerContacting => "Kb.Next.EmployerContacting",
        ApplicationStatus.Hired => "Kb.Next.Hired",
        ApplicationStatus.Rejected => "Kb.Next.Rejected",
        ApplicationStatus.FilledElsewhere => "Kb.Next.FilledElsewhere",
        ApplicationStatus.Withdrawn => null,
        _ => null
    };

    public static bool IsRunning(ApplicationStatus status)
        => status is ApplicationStatus.Pending
            or ApplicationStatus.Accepted
            or ApplicationStatus.EmployerContacting;

    public static bool IsFinished(ApplicationStatus status)
        => status is ApplicationStatus.Hired
            or ApplicationStatus.Rejected
            or ApplicationStatus.FilledElsewhere
            or ApplicationStatus.Withdrawn;

    private static ApplicationTimeline BuildLegacy(
        DateTime createdAt,
        ApplicationStatus status,
        DateTime? respondedAt)
    {
        var steps = new List<ApplicationTimelineStep>
        {
            Step(ApplicationTimelineStepKey.Sent, LabelSent, createdAt, done: true)
        };

        if (status == ApplicationStatus.Pending)
        {
            steps[0] = steps[0] with { State = ApplicationTimelineStepState.Current };
            steps.Add(Upcoming(ApplicationTimelineStepKey.Seen, LabelSeen));
            steps.Add(Upcoming(ApplicationTimelineStepKey.Interview, LabelInterview));
            steps.Add(Upcoming(ApplicationTimelineStepKey.Outcome, LabelOutcome));
        }
        else if (status == ApplicationStatus.Withdrawn)
        {
            steps.Add(Upcoming(ApplicationTimelineStepKey.Seen, LabelSeen));
            steps.Add(Upcoming(ApplicationTimelineStepKey.Interview, LabelInterview));
            steps.Add(Upcoming(ApplicationTimelineStepKey.Outcome, LabelOutcome));
            return new ApplicationTimeline(
                steps,
                LabelWithdrawn,
                respondedAt,
                LegacyNoHistory: true);
        }
        else
        {
            // Only Verstuurd + current status as current step (D4). No other steps marked done.
            var (key, label) = status switch
            {
                ApplicationStatus.Accepted => (ApplicationTimelineStepKey.Seen, LabelSeen),
                ApplicationStatus.EmployerContacting => (ApplicationTimelineStepKey.Interview, LabelInterview),
                ApplicationStatus.Hired
                    or ApplicationStatus.Rejected
                    or ApplicationStatus.FilledElsewhere => (ApplicationTimelineStepKey.Outcome, LabelOutcome),
                _ => (ApplicationTimelineStepKey.Seen, LabelSeen)
            };

            foreach (var sk in new[]
                     {
                         ApplicationTimelineStepKey.Seen,
                         ApplicationTimelineStepKey.Interview,
                         ApplicationTimelineStepKey.Outcome
                     })
            {
                if (sk == key)
                {
                    var state = status is ApplicationStatus.Rejected or ApplicationStatus.FilledElsewhere
                        ? ApplicationTimelineStepState.Skipped
                        : ApplicationTimelineStepState.Current;
                    if (status == ApplicationStatus.Hired)
                    {
                        state = ApplicationTimelineStepState.Done;
                    }

                    steps.Add(new ApplicationTimelineStep(sk, state, respondedAt, label));
                }
                else if ((int)sk < (int)key)
                {
                    // Intermediate steps are NOT marked done for legacy (D4).
                    steps.Add(Upcoming(sk, LabelFor(sk)));
                }
                else
                {
                    steps.Add(Upcoming(sk, LabelFor(sk)));
                }
            }
        }

        return new ApplicationTimeline(steps, ClosingLineKey: null, ClosingAtUtc: null, LegacyNoHistory: true);
    }

    private static ApplicationTimeline BuildWithdrawn(
        DateTime createdAt,
        DateTime? respondedAt,
        List<ApplicationStatusHistory> ordered)
    {
        var sentAt = FirstKind(ordered, ApplicationStatusEventKind.Created)?.OccurredAtUtc ?? createdAt;
        var seenAt = FirstKind(ordered, ApplicationStatusEventKind.EmployerViewed)?.OccurredAtUtc
                     ?? FirstChangeTo(ordered, ApplicationStatus.Accepted)?.OccurredAtUtc;
        var interviewAt = FirstChangeTo(ordered, ApplicationStatus.EmployerContacting)?.OccurredAtUtc;
        var withdrawnAt = FirstChangeTo(ordered, ApplicationStatus.Withdrawn)?.OccurredAtUtc ?? respondedAt;

        var steps = new List<ApplicationTimelineStep>
        {
            Step(ApplicationTimelineStepKey.Sent, LabelSent, sentAt, done: true)
        };
        if (seenAt.HasValue)
        {
            steps.Add(Step(ApplicationTimelineStepKey.Seen, LabelSeen, seenAt, done: true));
        }

        if (interviewAt.HasValue)
        {
            steps.Add(Step(ApplicationTimelineStepKey.Interview, LabelInterview, interviewAt, done: true));
        }

        // Pad remaining as upcoming so UI still has 4 slots when useful.
        while (steps.Count < 4)
        {
            var key = (ApplicationTimelineStepKey)steps.Count;
            steps.Add(Upcoming(key, LabelFor(key)));
        }

        return new ApplicationTimeline(steps, LabelWithdrawn, withdrawnAt, LegacyNoHistory: false);
    }

    private static ApplicationTimelineStep ResolveStep(
        ApplicationTimelineStepKey key,
        string label,
        DateTime? at,
        bool done,
        bool current,
        bool skipped = false)
    {
        var state = skipped ? ApplicationTimelineStepState.Skipped
            : done ? ApplicationTimelineStepState.Done
            : current ? ApplicationTimelineStepState.Current
            : ApplicationTimelineStepState.Upcoming;
        return new ApplicationTimelineStep(key, state, at, label);
    }

    private static ApplicationTimelineStep Step(
        ApplicationTimelineStepKey key,
        string label,
        DateTime? at,
        bool done)
        => new(key, done ? ApplicationTimelineStepState.Done : ApplicationTimelineStepState.Upcoming, at, label);

    private static ApplicationTimelineStep Upcoming(ApplicationTimelineStepKey key, string label)
        => new(key, ApplicationTimelineStepState.Upcoming, null, label);

    private static string LabelFor(ApplicationTimelineStepKey key) => key switch
    {
        ApplicationTimelineStepKey.Sent => LabelSent,
        ApplicationTimelineStepKey.Seen => LabelSeen,
        ApplicationTimelineStepKey.Interview => LabelInterview,
        _ => LabelOutcome
    };

    private static ApplicationStatusHistory? FirstKind(
        IEnumerable<ApplicationStatusHistory> history,
        ApplicationStatusEventKind kind)
        => history.FirstOrDefault(h => h.Kind == kind);

    private static ApplicationStatusHistory? FirstChangeTo(
        IEnumerable<ApplicationStatusHistory> history,
        ApplicationStatus to)
        => history.FirstOrDefault(h =>
            h.Kind == ApplicationStatusEventKind.StatusChanged && h.ToStatus == to);

    private static ApplicationStatusHistory? FirstChangeToAny(
        IEnumerable<ApplicationStatusHistory> history,
        params ApplicationStatus[] tos)
        => history.FirstOrDefault(h =>
            h.Kind == ApplicationStatusEventKind.StatusChanged
            && h.ToStatus is { } t
            && tos.Contains(t));
}
