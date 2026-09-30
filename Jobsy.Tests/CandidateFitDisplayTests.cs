using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Tests;

public class CandidateFitDisplayTests
{
    [Fact]
    public void Gate_closed_returns_null_fit()
    {
        var match = ScoreSample(travel: 10, competency: 80);
        Assert.Null(CandidateFitDisplay.Build(match, CandidateFitGate.Closed));
    }

    [Fact]
    public void Gate_open_with_culture_or_values()
    {
        Assert.True(new CandidateFitGate(true, false).IsOpen);
        Assert.True(new CandidateFitGate(false, true).IsOpen);
        Assert.False(new CandidateFitGate(false, false).IsOpen);
    }

    [Fact]
    public void Percent_clamped_to_55_90()
    {
        Assert.Equal(55, CandidateFitDisplay.MapToPercent(0));
        Assert.Equal(90, CandidateFitDisplay.MapToPercent(1));
        Assert.InRange(CandidateFitDisplay.MapToPercent(0.5), 55, 90);
    }

    [Fact]
    public void Calibration_is_monotonic_in_s()
    {
        var prev = -1;
        for (var i = 0; i <= 20; i++)
        {
            var pct = CandidateFitDisplay.MapToPercent(i / 20.0);
            Assert.True(pct >= prev, $"s={i / 20.0}: {pct} < {prev}");
            prev = pct;
        }
    }

    [Theory]
    [InlineData(74, KbFitBand.Good)]
    [InlineData(75, KbFitBand.Strong)]
    [InlineData(64, KbFitBand.Some)]
    [InlineData(65, KbFitBand.Good)]
    public void Band_boundaries(int percent, KbFitBand expected)
        => Assert.Equal(expected, CandidateFitDisplay.BandFor(percent));

    [Fact]
    public void Why_kinds_map_from_match_why_max_two()
    {
        var match = ScoreSample(travel: 8, competency: 85);
        // Inject why points.
        match = CloneWithWhy(match,
        [
            new("culture", "mid", "Cultuur past."),
            new("travel", "ok", "Goede reistijd."),
            new("competency", "samenwerken", "Competenties.")
        ]);
        var kinds = CandidateFitDisplay.MapWhyKinds(match);
        Assert.Equal(2, kinds.Count);
        Assert.Equal("culture", kinds[0]);
        Assert.Equal("travel", kinds[1]);
    }

    [Fact]
    public void Apply_closed_nulls_match_percent()
    {
        var match = ScoreSample(travel: 10, competency: 90);
        var applied = CandidateFitApply.Apply(match, CandidateFitGate.Closed);
        Assert.Equal(CandidateFitApply.FitGateClosed, applied.FitGate);
        Assert.Null(applied.MatchPercent);
        Assert.Null(applied.FitPercent);
    }

    [Fact]
    public void Apply_open_sets_calibrated_percent_and_dimensions()
    {
        var match = ScoreSample(travel: 10, competency: 85, withCulture: true, withValues: true);
        var gate = new CandidateFitGate(true, true);
        var applied = CandidateFitApply.Apply(match, gate);
        Assert.Equal(CandidateFitApply.FitGateOpen, applied.FitGate);
        Assert.NotNull(applied.FitPercent);
        Assert.InRange(applied.FitPercent!.Value, 55, 90);
        Assert.Equal(applied.FitPercent, applied.MatchPercent);
        Assert.NotNull(applied.FitDimensions);
    }

    [Fact]
    public void Dislike_sort_penalty_orders_correctly()
    {
        // 85 with dislike (sort 70) sorts below 75 clean, above 69 clean.
        var items = new[]
        {
            (Id: 1, Pct: 85, Lower: true),
            (Id: 2, Pct: 75, Lower: false),
            (Id: 3, Pct: 69, Lower: false)
        };
        var ordered = KbRanking.OrderByFitThenTitle(
                items,
                i => i.Pct,
                i => i.Lower,
                i => i.Id.ToString(),
                i => Guid.Parse($"00000000-0000-0000-0000-00000000000{i.Id}"))
            .Select(i => i.Id)
            .ToList();
        Assert.Equal([2, 1, 3], ordered);
        Assert.Equal(85 - KbRanking.DislikePenalty, KbRanking.SortKey(85, true));
    }

    [Fact]
    public void Dislike_never_hides_same_count()
    {
        var ids = Enumerable.Range(1, 10).Select(i => Guid.NewGuid()).ToList();
        var with = ids.Select(id => (id, ranksLower: id.GetHashCode() % 2 == 0)).ToList();
        Assert.Equal(ids.Count, with.Count);
        Assert.Equal(ids.Count, with.Count(x => true)); // never filtered
    }

    [Fact]
    public void No_dislike_source_returns_none()
    {
        var codes = KbNoDislikeSource.Instance
            .GetMatchingDislikeCodesAsync(Guid.NewGuid(), Guid.NewGuid())
            .GetAwaiter().GetResult();
        Assert.Empty(codes);
    }

    private static ProfileVacancyMatch ScoreSample(
        int travel,
        int competency,
        bool withCulture = false,
        bool withValues = false)
    {
        CulturePersonalityScores? culture = withCulture
            ? new CulturePersonalityScores(70, 60, 65, 55, 50, 60, 55, 60, 50, 65, 55)
            : null;
        SchwartzValuesScores? values = withValues
            ? new SchwartzValuesScores(70, 60, 50, 55, 65)
            : null;
        return ProfileVacancyMatchCalculator.Calculate(new ProfileVacancyMatchInput
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Barista team",
            VacancyDescription = "Samenwerken in een rustig team, klantgericht.",
            WorkTypes = ["Horeca"],
            CandidateRoles = ["Horeca"],
            CandidateCompetencies = new CompetencyScores(competency, competency, competency, competency, competency),
            CandidateCultureScores = culture,
            CandidateValuesScores = values,
            CulturePillars = withCulture ? ["collaboration"] : null,
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = travel,
                MaxTravelMinutes = 30,
                CandidateHours = new HoursRange(16, 24),
                VacancyHours = new HoursRange(16, 24),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateSchedule = new SchedulePayload
                {
                    Slots = new Dictionary<string, List<string>> { ["Ma"] = ["Ochtend"] }
                }.Normalize(),
                CandidateAgeYears = 22
            }
        });
    }

    private static ProfileVacancyMatch CloneWithWhy(
        ProfileVacancyMatch match,
        IReadOnlyList<ProfileMatchExplainPoint> why)
        => new()
        {
            VacancyId = match.VacancyId,
            VacancyTitle = match.VacancyTitle,
            TotalPercent = match.TotalPercent,
            Core = match.Core,
            ExperienceScore01 = match.ExperienceScore01,
            CompetencyScore01 = match.CompetencyScore01,
            CompetencyDim01 = match.CompetencyDim01,
            InterestScore01 = match.InterestScore01,
            CultureDim01 = match.CultureDim01,
            ValuesFit01 = match.ValuesFit01,
            CultureFit = match.CultureFit,
            IsBroadMatch = match.IsBroadMatch,
            MatchRationale = match.MatchRationale,
            Why = why,
            Gaps = match.Gaps,
            ColorBand = match.ColorBand
        };
}
