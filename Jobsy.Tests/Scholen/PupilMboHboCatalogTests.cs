using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Tests.Scholen;

public sealed class PupilMboHboCatalogTests
{
    [Fact]
    public void Loads_all_csv_banks_with_expected_counts()
    {
        var cat = PupilMboHboCatalog.Default;
        Assert.Equal(40, cat.QuestionCount(PupilMboHboBlock.Vakken, SchoolLevel.Mbo2));
        Assert.Equal(40, cat.QuestionCount(PupilMboHboBlock.Vakken, SchoolLevel.Hbo));
        Assert.Equal(48, cat.QuestionCount(PupilMboHboBlock.Interesse, SchoolLevel.Mbo1));
        Assert.Equal(40, cat.QuestionCount(PupilMboHboBlock.Competentie, SchoolLevel.Mbo1));
        Assert.Equal(50, cat.QuestionCount(PupilMboHboBlock.Beroepen, SchoolLevel.Mbo1));
        Assert.Equal(41, cat.QuestionCount(PupilMboHboBlock.Werkwaarden, SchoolLevel.Mbo1));
    }

    [Fact]
    public void Every_csv_question_has_non_empty_voorbeeldscenario()
    {
        var cat = PupilMboHboCatalog.Default;
        var levels = new[]
        {
            SchoolLevel.Mbo1,
            SchoolLevel.Mbo2,
            SchoolLevel.Mbo3,
            SchoolLevel.Mbo4,
            SchoolLevel.Hbo
        };
        var missing = new List<string>();
        foreach (var block in PupilMboHboCatalog.AllBlocks)
        {
            foreach (var level in levels)
            {
                foreach (var q in cat.QuestionsFor(block, level))
                {
                    if (string.IsNullOrWhiteSpace(q.Scenario))
                    {
                        missing.Add($"{block}/{level}/{q.Id}");
                    }
                }
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void First_vakken_question_matches_csv_wording()
    {
        var q = PupilMboHboCatalog.Default.QuestionsFor(PupilMboHboBlock.Vakken, SchoolLevel.Mbo3)[0];
        Assert.Equal("Het vak Nederlands.", q.Question);
        Assert.Contains("Stel je voor:", q.Scenario, StringComparison.Ordinal);
    }

    [Fact]
    public void Werkwaarden_outcome_bars_have_unique_labels_for_storage()
    {
        var cat = PupilMboHboCatalog.Default;
        var state = new PupilMboHboProgressState();
        foreach (var q in cat.QuestionsFor(PupilMboHboBlock.Werkwaarden, SchoolLevel.Mbo2))
        {
            state.Answers[q.Id] = 3;
        }

        var outcome = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Werkwaarden, state, SchoolLevel.Mbo2);
        var dupes = outcome.Bars.GroupBy(b => b.Label).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(dupes);
    }

    [Fact]
    public void Pattern_signal_detects_uniform_answers()
    {
        var vals = Enumerable.Repeat(3, 45).ToList();
        Assert.True(PupilMboHboScoring.DetectAnswerPattern(vals));
        var mixed = Enumerable.Range(1, 45).Select(i => (i % 5) + 1).ToList();
        Assert.False(PupilMboHboScoring.DetectAnswerPattern(mixed));
    }
}
