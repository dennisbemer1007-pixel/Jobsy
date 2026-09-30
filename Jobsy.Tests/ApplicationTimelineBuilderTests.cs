using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Tests;

public class ApplicationTimelineBuilderTests
{
    private static ApplicationStatusHistory Row(
        ApplicationStatusEventKind kind,
        DateTime at,
        ApplicationStatus? from = null,
        ApplicationStatus? to = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Kind = kind,
            FromStatus = from,
            ToStatus = to,
            OccurredAtUtc = at,
            ActorKind = ApplicationStatusActorKind.Employer
        };

    [Theory]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.EmployerContacting)]
    [InlineData(ApplicationStatus.Hired)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.FilledElsewhere)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void Legacy_no_history_only_marks_sent_and_current(ApplicationStatus status)
    {
        var created = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime? responded = status == ApplicationStatus.Pending
            ? null
            : created.AddDays(2);
        var timeline = ApplicationTimelineBuilder.Build(created, status, responded, []);

        Assert.True(timeline.LegacyNoHistory);
        Assert.Equal(4, timeline.Steps.Count);
        Assert.Equal(created, timeline.Steps[0].OccurredAtUtc);

        if (status == ApplicationStatus.Pending)
        {
            Assert.Equal(ApplicationTimelineStepState.Current, timeline.Steps[0].State);
            Assert.All(timeline.Steps.Skip(1), s => Assert.Equal(ApplicationTimelineStepState.Upcoming, s.State));
        }
        else if (status == ApplicationStatus.Withdrawn)
        {
            Assert.Equal(ApplicationTimelineStepState.Done, timeline.Steps[0].State);
            Assert.Equal(ApplicationTimelineBuilder.LabelWithdrawn, timeline.ClosingLineKey);
        }
        else
        {
            Assert.Equal(ApplicationTimelineStepState.Done, timeline.Steps[0].State);
            // Intermediate steps must NOT be Done for legacy (D4).
            Assert.DoesNotContain(
                timeline.Steps.Skip(1),
                s => s.State == ApplicationTimelineStepState.Done
                     && s.Key is ApplicationTimelineStepKey.Seen or ApplicationTimelineStepKey.Interview);
        }
    }

    [Fact]
    public void With_history_marks_dated_steps()
    {
        var created = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc);
        var seen = created.AddDays(2);
        var interview = created.AddDays(14);
        var history = new[]
        {
            Row(ApplicationStatusEventKind.Created, created, to: ApplicationStatus.Pending),
            Row(ApplicationStatusEventKind.EmployerViewed, seen),
            Row(ApplicationStatusEventKind.StatusChanged, interview,
                ApplicationStatus.Accepted, ApplicationStatus.EmployerContacting)
        };

        var timeline = ApplicationTimelineBuilder.Build(
            created,
            ApplicationStatus.EmployerContacting,
            interview,
            history);

        Assert.False(timeline.LegacyNoHistory);
        Assert.Equal(ApplicationTimelineStepState.Done, timeline.Steps[0].State);
        Assert.Equal(ApplicationTimelineStepState.Done, timeline.Steps[1].State);
        Assert.Equal(ApplicationTimelineStepState.Current, timeline.Steps[2].State);
        Assert.Equal(interview, timeline.Steps[2].OccurredAtUtc);
        Assert.Equal(ApplicationTimelineStepState.Upcoming, timeline.Steps[3].State);
    }

    [Fact]
    public void Withdrawn_adds_closing_line()
    {
        var created = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var withdrawn = created.AddDays(3);
        var history = new[]
        {
            Row(ApplicationStatusEventKind.Created, created, to: ApplicationStatus.Pending),
            Row(ApplicationStatusEventKind.StatusChanged, withdrawn,
                ApplicationStatus.Pending, ApplicationStatus.Withdrawn)
        };

        var timeline = ApplicationTimelineBuilder.Build(
            created,
            ApplicationStatus.Withdrawn,
            withdrawn,
            history);

        Assert.Equal(ApplicationTimelineBuilder.LabelWithdrawn, timeline.ClosingLineKey);
        Assert.Equal(withdrawn, timeline.ClosingAtUtc);
    }

    [Theory]
    [InlineData(ApplicationStatus.Pending, "Kb.Next.Pending")]
    [InlineData(ApplicationStatus.Accepted, "Kb.Next.Accepted")]
    [InlineData(ApplicationStatus.EmployerContacting, "Kb.Next.EmployerContacting")]
    [InlineData(ApplicationStatus.Hired, "Kb.Next.Hired")]
    [InlineData(ApplicationStatus.Rejected, "Kb.Next.Rejected")]
    [InlineData(ApplicationStatus.FilledElsewhere, "Kb.Next.FilledElsewhere")]
    [InlineData(ApplicationStatus.Withdrawn, null)]
    public void NextStepKey_matches_status(ApplicationStatus status, string? key)
        => Assert.Equal(key, ApplicationTimelineBuilder.NextStepKey(status));
}
