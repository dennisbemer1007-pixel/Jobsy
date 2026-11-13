using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilG78ContrastLikertScorerTests
{
    private static Dictionary<int, int> Fill(int value) =>
        Enumerable.Range(PupilQuestionBank.MinId, 60).ToDictionary(id => id, _ => value);

    [Fact]
    public void Answer_1_on_realistic_social_item_boosts_realistic_not_social()
    {
        var answers = new Dictionary<int, int> { [9016] = 1 };
        var bank = new PupilQuestionBankG78V2();
        var scores = PupilG78ContrastLikertScorer.ScoreCareer(answers, bank.Questions)!;
        Assert.True(scores.Realistic > scores.Social);
    }

    [Fact]
    public void Answer_5_on_realistic_social_item_boosts_social_not_realistic()
    {
        var answers = new Dictionary<int, int> { [9016] = 5 };
        var bank = new PupilQuestionBankG78V2();
        var scores = PupilG78ContrastLikertScorer.ScoreCareer(answers, bank.Questions)!;
        Assert.True(scores.Social > scores.Realistic);
    }

    [Fact]
    public void All_3_is_neutral_on_competence_and_values()
    {
        var answers = Fill(3);
        var bank = new PupilQuestionBankG78V2();
        var competence = PupilG78ContrastLikertScorer.ScoreCompetence(answers, bank.Questions)!;
        var values = PupilG78ContrastLikertScorer.ScoreValues(answers, bank.Questions)!;
        Assert.Equal(50, competence.Samenwerken);
        Assert.Equal(50, values.Autonomy);
    }

    [Fact]
    public void G78_v1_bank_still_scores_with_classic_likert_path()
    {
        var answers = Fill(5);
        var bank = new PupilQuestionBank();
        var scores = CompetencyTestCatalog.Score(answers, bank.AsCompetencyItems())!;
        Assert.True(scores.Samenwerken > 50);
    }
}
