using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
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
    public void Contrast_facts_use_A_text_for_1_2_and_B_text_for_4_5_every_item()
    {
        var bank = new PupilQuestionBankG78V2();
        foreach (var q in bank.Questions)
        {
            Assert.True(
                PupilQuestionBankNlStringsG78V2.TryGet(q.TextKeyA, out var stemA),
                $"missing A stem for {q.Id}");
            Assert.True(
                PupilQuestionBankNlStringsG78V2.TryGet(q.TextKeyB!, out var stemB),
                $"missing B stem for {q.Id}");

            var towardA = PupilQuestionStoryParaphrase.ContrastChosenFact(q, 1)!.Value.Text;
            var towardB = PupilQuestionStoryParaphrase.ContrastChosenFact(q, 5)!.Value.Text;
            Assert.NotEqual(towardA, towardB);
            Assert.DoesNotContain(
                PupilQuestionStoryParaphrase.PolishJeForm(stemB),
                towardA,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                PupilQuestionStoryParaphrase.PolishJeForm(stemA),
                towardB,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_heavy_and_B_heavy_v2_stories_differ_and_use_chosen_pole_only()
    {
        var a = PupilG78ContrastV2Fixtures.AllTowardA();
        var b = PupilG78ContrastV2Fixtures.AllTowardB();
        var pkgA = PupilG78FactStory.Build(a.Result, a.Progress, "nl");
        var pkgB = PupilG78FactStory.Build(b.Result, b.Progress, "nl");

        Assert.NotEqual(pkgA.Body, pkgB.Body);
        var parasA = PupilG78FactStory.SplitParagraphs(pkgA.Body);
        var parasB = PupilG78FactStory.SplitParagraphs(pkgB.Body);
        Assert.Equal(parasA.Count, parasB.Count);
        Assert.All(parasA.Zip(parasB), pair => Assert.NotEqual(pair.First, pair.Second));

        Assert.DoesNotContain("vindt het lastig", pkgB.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("verdrietig", pkgB.Body, StringComparison.OrdinalIgnoreCase);

        var growthB = pkgB.GrowthCompetencies.Select(g => g.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var strongB = PupilG78GrowthCompetencies.TopStrongCategories(
            PupilG78GrowthCompetencies.DimensionAverages(
                JsonSerializer.Deserialize<Dictionary<string, int>>(b.Progress.AnswersJson!)!,
                contrastV2: true),
            3);
        foreach (var dim in strongB)
        {
            Assert.DoesNotContain(dim, growthB);
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
    public void A_and_B_heavy_v2_paragraph_two_strongest_competency_labels_differ()
    {
        var a = PupilG78ContrastV2Fixtures.AllTowardA();
        var b = PupilG78ContrastV2Fixtures.AllTowardB();
        var introA = CompetencyIntro(a.Result, a.Progress);
        var introB = CompetencyIntro(b.Result, b.Progress);
        Assert.NotEqual(introA, introB);
    }

    [Fact]
    public void Paragraph_two_lists_supported_answer_sentences_per_named_competency()
    {
        foreach (var (result, progress) in new[]
                 {
                     PupilG78ContrastV2Fixtures.AllTowardA(),
                     PupilG78ContrastV2Fixtures.AllTowardB(),
                     PupilG78ContrastV2Fixtures.MixedToward(),
                 })
        {
            var package = PupilG78FactStory.Build(result, progress, "nl");
            var para = PupilG78FactStory.SplitParagraphs(package.Body)[1];
            var dot = para.IndexOf('.', StringComparison.Ordinal);
            var intro = dot < 0 ? para : para[..(dot + 1)];
            Assert.StartsWith("Wat in je antwoorden vooral naar voren komt, is", intro, StringComparison.Ordinal);
            var rest = para[intro.Length..].Trim();
            var labels = KidLabelsFromIntro(intro).ToList();
            Assert.Equal(labels.Count, package.CompetencyExamples.Count);
            foreach (var label in labels)
            {
                var anchor = package.CompetencyExamples.FirstOrDefault(a =>
                    string.Equals(PupilG78CompetencyNarrative.KidLabelList(a.Category), label, StringComparison.OrdinalIgnoreCase));
                Assert.NotEqual(default, anchor);
                Assert.True(
                    LabelSupportedInParagraph(label, rest),
                    $"No supporting fact in paragraph 2 for '{label}'. Paragraph: {para}");
            }
        }
    }

    [Fact]
    public void Story_sentences_start_with_capital_letter()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.MixedToward();
        var body = PupilG78FactStory.Build(result, progress, "nl").Body;
        foreach (var sentence in SplitSentences(body))
        {
            Assert.True(char.IsUpper(sentence.TrimStart()[0]), $"Lowercase start: {sentence}");
        }
    }

    [Fact]
    public void Mixed_competence_tile_uses_maar_between_stress_opposites()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.MixedToward();
        var package = PupilG78FactStory.Build(result, progress, "nl");
        var tile = package.Tiles.First(t => t.ModelKey == "competence");
        if (tile.Explanation.Contains("rustig verder", StringComparison.OrdinalIgnoreCase)
            && tile.Explanation.Contains("haast", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("Maar", tile.Explanation, StringComparison.Ordinal);
        }
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

    private static string CompetencyIntro(PupilResult result, PupilProgress progress)
    {
        var paragraphs = PupilG78FactStory.SplitParagraphs(
            PupilG78FactStory.Build(result, progress, "nl").Body);
        Assert.True(paragraphs.Count >= 2);
        var para = paragraphs[1];
        var dot = para.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? para : para[..(dot + 1)];
    }

    private static IEnumerable<string> KidLabelsFromIntro(string intro)
    {
        const string prefix = "Wat in je antwoorden vooral naar voren komt, is ";
        Assert.StartsWith(prefix, intro, StringComparison.Ordinal);
        var listPart = intro[prefix.Length..].TrimEnd('.').Trim();
        foreach (var chunk in listPart.Split(',', StringSplitOptions.TrimEntries))
        {
            foreach (var part in chunk.Split(" en ", StringSplitOptions.TrimEntries))
            {
                if (part.Length > 0)
                {
                    yield return part;
                }
            }
        }
    }

    private static bool LabelSupportedInParagraph(string kidLabel, string paragraphRest)
        => paragraphRest.Contains(kidLabel, StringComparison.OrdinalIgnoreCase)
           || kidLabel switch
           {
               "plannen" => paragraphRest.Contains("plan", StringComparison.OrdinalIgnoreCase),
               "communiceren" => paragraphRest.Contains("groep", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("praat", StringComparison.OrdinalIgnoreCase),
               "doorzetten" => paragraphRest.Contains("verder", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("tegenzit", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("doorwerk", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("leuks", StringComparison.OrdinalIgnoreCase),
               "creativiteit" => paragraphRest.Contains("idee", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("verhaal", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("bedenkt", StringComparison.OrdinalIgnoreCase),
               "samenwerken" => paragraphRest.Contains("samen", StringComparison.OrdinalIgnoreCase)
                                || paragraphRest.Contains("groep", StringComparison.OrdinalIgnoreCase),
               _ => false,
           };

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
