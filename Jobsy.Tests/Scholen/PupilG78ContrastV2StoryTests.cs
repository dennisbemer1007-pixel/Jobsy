using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78ContrastV2StoryTests
{
    [Fact]
    public void Contrast_chosen_fact_covers_b_pole_for_every_v2_item()
    {
        var bank = new PupilQuestionBankG78V2();
        foreach (var q in bank.Questions)
        {
            Assert.NotNull(PupilQuestionStoryParaphrase.ContrastChosenFact(q, 5));
            Assert.NotNull(PupilQuestionStoryParaphrase.ContrastChosenFact(q, 1));
            Assert.Null(PupilQuestionStoryParaphrase.ContrastChosenFact(q, 3));
        }
    }

    [Fact]
    public void Contrast_clauses_direct_build_fills_four_tiles()
    {
        var bank = new PupilQuestionBankG78V2();
        var clauses = new List<G78AnswerClause>();
        foreach (var q in bank.Questions)
        {
            var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, 5);
            if (fact is null)
            {
                continue;
            }

            clauses.Add(new G78AnswerClause(
                q.Id,
                fact.Value.Category,
                q.Model,
                G78StoryBeat.Doen,
                fact.Value.Text,
                fact.Value.EffectiveScore));
        }

        var (tiles, _) = PupilG78FactTileComposer.Build(
            clauses,
            "nl",
            new HashSet<int>(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            4);
        Assert.All(tiles, t => Assert.True(SplitSentences(t.Explanation).Count >= 2));
    }

    [Fact]
    public void Mixed_v2_fills_all_four_tiles_with_at_least_two_sentences_each()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.MixedToward();
        var package = PupilG78FactStory.Build(result, progress, "nl");
        Assert.All(package.Tiles, t => Assert.True(SplitSentences(t.Explanation).Count >= 2, t.KidLabel));
    }

    [Fact]
    public void B_heavy_v2_fills_all_four_tiles_with_at_least_two_sentences_each()
    {
        var (_, progress) = PupilG78ContrastV2Fixtures.AllTowardB();
        var result = PupilG78ContrastV2Fixtures.AllTowardB().Result;
        var package = PupilG78FactStory.Build(result, progress, "nl");
        Assert.Equal(4, package.Tiles.Count);
        foreach (var tile in package.Tiles)
        {
            var sentences = SplitSentences(tile.Explanation);
            Assert.True(
                sentences.Count >= 2,
                $"Tile '{tile.KidLabel}' had {sentences.Count} sentence(s). All tiles: "
                + string.Join(" | ", package.Tiles.Select(t => $"{t.KidLabel}=[{t.Explanation.Length} chars]")));
            Assert.False(string.IsNullOrWhiteSpace(tile.Explanation));
        }
    }

    [Fact]
    public void A_heavy_and_B_heavy_v2_shift_riasec_scores_with_same_likes()
    {
        var a = PupilG78ContrastV2Fixtures.AllTowardA();
        var b = PupilG78ContrastV2Fixtures.AllTowardB();
        Assert.NotEqual(a.Result.HollandCode, b.Result.HollandCode);
        Assert.NotEqual(a.Result.RiasecScoresJson, b.Result.RiasecScoresJson);
        var jobsA = PupilRiasecJobIdeas.TopGroep78Jobs(a.Result, a.Progress);
        var jobsB = PupilRiasecJobIdeas.TopGroep78Jobs(b.Result, b.Progress);
        Assert.NotEmpty(jobsA);
        Assert.NotEmpty(jobsB);
    }

    [Fact]
    public void Display_job_chips_without_likes_follow_riasec_from_v2_answers_not_likes_pool()
    {
        var a = PupilG78ContrastV2Fixtures.AllTowardANoLikes();
        var b = PupilG78ContrastV2Fixtures.AllTowardBNoLikes();
        var renderer = new PupilStoryRenderer();
        var storyA = renderer.Render(a.Result, a.Progress, "nl");
        var storyB = renderer.Render(b.Result, b.Progress, "nl");
        Assert.NotEqual(storyA.Body, storyB.Body);
        Assert.NotEqual(
            string.Join('|', storyA.JobIdeas),
            string.Join('|', storyB.JobIdeas));
    }

    private static List<string> SplitSentences(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        return body.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .Select(s => s + ".")
            .ToList();
    }
}
