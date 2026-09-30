using Jobsy.Core.Enums;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Tests;

public class KbSavedJobStateTests
{
    private static readonly DateTime NoonUtc = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Active_more_than_7_days_is_Open()
    {
        // Amsterdam today ≈ 30 Sep; EndDate 8 Oct = 8 days → Open
        var state = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Active,
            new DateOnly(2026, 10, 8),
            closedAtUtc: null,
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.Open, state.Kind);
        Assert.True(state.DaysUntilEnd > 7);
    }

    [Fact]
    public void Active_within_7_days_is_ClosingSoon()
    {
        var state = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Active,
            new DateOnly(2026, 10, 5),
            closedAtUtc: null,
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.ClosingSoon, state.Kind);
        Assert.Equal(5, state.DaysUntilEnd);
    }

    [Fact]
    public void Active_end_today_is_ClosingSoon_zero()
    {
        var state = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Active,
            new DateOnly(2026, 9, 30),
            closedAtUtc: null,
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.ClosingSoon, state.Kind);
        Assert.Equal(0, state.DaysUntilEnd);
    }

    [Fact]
    public void Fulfilled_wins()
    {
        var state = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Fulfilled,
            new DateOnly(2026, 12, 1),
            closedAtUtc: null,
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.Fulfilled, state.Kind);
    }

    [Fact]
    public void Archived_or_past_end_is_Closed()
    {
        var archived = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Archived,
            new DateOnly(2026, 12, 1),
            closedAtUtc: null,
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.Closed, archived.Kind);

        var past = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Active,
            new DateOnly(2026, 9, 29),
            closedAtUtc: null,
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.Closed, past.Kind);

        var closedAt = KbSavedJobStateResolver.Resolve(
            VacancyStatus.Active,
            new DateOnly(2026, 12, 1),
            closedAtUtc: NoonUtc.AddDays(-1),
            NoonUtc);
        Assert.Equal(KbSavedJobStateKind.Closed, closedAt.Kind);
    }

    [Fact]
    public void Draft_and_pending_are_Hidden()
    {
        Assert.Equal(
            KbSavedJobStateKind.Hidden,
            KbSavedJobStateResolver.Resolve(VacancyStatus.Draft, new DateOnly(2026, 12, 1), null, NoonUtc).Kind);
        Assert.Equal(
            KbSavedJobStateKind.Hidden,
            KbSavedJobStateResolver.Resolve(VacancyStatus.PendingApproval, new DateOnly(2026, 12, 1), null, NoonUtc).Kind);
    }
}
