using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Tests.Scholen;

public sealed class PupilMboHboGroupInsightsTests
{
    [Fact]
    public void Aggregator_hides_below_five_completed_blocks()
    {
        var states = Enumerable.Range(0, 4).Select(_ => CompleteState()).ToList();
        var agg = PupilMboHboGroupInsightsAggregator.Aggregate(states, SchoolLevel.Mbo2);
        Assert.False(agg.Visible);
        Assert.Equal(4, agg.CompletedCount);
    }

    [Fact]
    public void Aggregator_counts_werkwaarden_and_werkcultuur_for_five_pupils()
    {
        var states = Enumerable.Range(0, 5).Select(_ => CompleteState()).ToList();
        var agg = PupilMboHboGroupInsightsAggregator.Aggregate(states, SchoolLevel.Mbo2);
        Assert.True(agg.Visible);
        Assert.Equal(5, agg.CompletedCount);
        Assert.Equal(6, agg.RiasecBars.Count);
        Assert.NotEmpty(agg.TopValues);
        Assert.NotEmpty(agg.TopCultures);
        Assert.NotEmpty(agg.CompetenceBands);
        Assert.Empty(agg.DreamJobs);
        Assert.Equal(0, agg.NotFilledDreamJobCount);
    }

    [Fact]
    public void ScoreVakken_normalizes_bar_range_to_user_scores()
    {
        var cat = PupilMboHboCatalog.Default;
        var progress = new PupilMboHboProgressState();
        var questions = cat.QuestionsFor(PupilMboHboBlock.Vakken, SchoolLevel.Mbo2).Take(6).ToList();
        for (var i = 0; i < questions.Count; i++)
        {
            var high = i < 4;
            progress.VakScores[questions[i].Id] = new PupilMboHboVakScore
            {
                Leuk = high ? 5 : 2,
                Goed = high ? 5 : 2,
            };
        }

        var outcome = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Vakken, progress, SchoolLevel.Mbo2);
        Assert.NotEmpty(outcome.Bars);
        Assert.Equal(100, outcome.Bars[0].Percent);
        Assert.InRange(outcome.Bars.Min(b => b.Percent), 0, 99);
    }

    [Fact]
    public void Holland_code_helper_orders_interesse_bars()
    {
        var bars = new List<PupilMboHboScoring.NamedScore>
        {
            new("S (Sociaal)", 80),
            new("R (Praktisch)", 70),
            new("I (Onderzoekend)", 60)
        };
        Assert.Equal("SRI", PupilMboHboScoring.HollandCodeFromInteresse(bars));
    }

    private static PupilMboHboProgressState CompleteState()
    {
        var cat = PupilMboHboCatalog.Default;
        var state = new PupilMboHboProgressState();
        var now = DateTime.UtcNow.ToString("O");
        foreach (var block in PupilMboHboCatalog.AllBlocks)
        {
            state.BlocksCompleted[PupilMboHboProgressState.BlockKey(block)] = now;
            foreach (var q in cat.QuestionsFor(block, SchoolLevel.Mbo2))
            {
                state.Answers[q.Id] = 4;
            }
        }

        return state;
    }
}
