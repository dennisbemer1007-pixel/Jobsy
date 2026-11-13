using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
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
        Assert.True(parasA.Count >= 2 && parasB.Count >= 2);

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
            var paragraphs = PupilG78FactStory.SplitParagraphs(package.Body);
            var para = paragraphs.FirstOrDefault(p =>
                p.StartsWith("Wat in je antwoorden vooral naar voren komt", StringComparison.Ordinal));
            if (para is null || package.CompetencyExamples.Count == 0)
            {
                continue;
            }

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
                $"Tile '{tile.KidLabel}' had {sentences.Count} sentence(s): {tile.Explanation}");
        }
    }

    [Fact]
    public void A_heavy_and_B_heavy_v2_shift_riasec_scores_with_same_likes()
    {
        var a = PupilG78ContrastV2Fixtures.AllTowardA();
        var b = PupilG78ContrastV2Fixtures.AllTowardB();
        Assert.NotEqual(a.Result.CompetenceScoresJson, b.Result.CompetenceScoresJson);
        var jobsA = PupilRiasecJobIdeas.TopGroep78Jobs(a.Result, a.Progress);
        var jobsB = PupilRiasecJobIdeas.TopGroep78Jobs(b.Result, b.Progress);
        Assert.NotEmpty(jobsA);
        Assert.NotEmpty(jobsB);
    }

    [Fact]
    public void All_v2_je_forms_have_proper_conjugation()
    {
        for (var id = PupilQuestionBank.MinId; id <= PupilQuestionBank.MaxId; id++)
        {
            Assert.True(PupilQuestionBankJeFormsG78V2.TryGet(id, towardB: false, out var jeA));
            Assert.False(PupilG78QuestionJeForms.HasBareStemAfterJe(jeA), $"900{id - 9000} A: {jeA}");
            Assert.True(PupilQuestionBankJeFormsG78V2.TryGet(id, towardB: true, out var jeB));
            Assert.False(PupilG78QuestionJeForms.HasBareStemAfterJe(jeB), $"900{id - 9000} B: {jeB}");
        }
    }

    [Fact]
    public void B_heavy_page_does_not_contradict_doorzetten_with_stoppen_or_leuks()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.AllTowardB();
        var package = PupilG78FactStory.Build(result, progress, "nl");
        var intro = CompetencyIntro(result, progress);
        if (intro.Contains("doorzetten", StringComparison.OrdinalIgnoreCase))
        {
            var page = package.Body + " " + string.Join(' ', package.Tiles.Select(t => t.Explanation));
            Assert.DoesNotContain("wil je even stoppen", page, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("iets leuks doen en kijkt later wel", page, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Story_and_tiles_share_no_duplicate_sentences()
    {
        foreach (var fixture in new Func<(PupilResult, PupilProgress)>[]
                 {
                     PupilG78ContrastV2Fixtures.AllTowardA,
                     PupilG78ContrastV2Fixtures.AllTowardB,
                     PupilG78ContrastV2Fixtures.MixedToward,
                 })
        {
            var (result, progress) = fixture();
            var package = PupilG78FactStory.Build(result, progress, "nl");
            var norms = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sentence in EnumeratePageSentences(package))
            {
                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                if (norm.Length < 12)
                {
                    continue;
                }

                Assert.True(norms.Add(norm), $"Duplicate on page: {sentence}");
            }
        }
    }

    [Fact]
    public void A_heavy_story_does_not_repeat_tijd_bijna_om_sentence_on_competence_tile()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.AllTowardA();
        var package = PupilG78FactStory.Build(result, progress, "nl");
        var zoBenJij = package.Tiles.First(t => t.ModelKey == "competence");
        var tileSentence = PupilG78PageTextDedupe.SplitRenderedSentences(
                PupilG78PageTextDedupe.FinalizeRenderedText(zoBenJij.Explanation))
            .First(s => s.Contains("tijd bijna om is", StringComparison.OrdinalIgnoreCase));
        var tileNorm = PupilG78PageLint.NormalizeSentence(tileSentence);
        foreach (var para in PupilG78FactStory.SplitParagraphs(package.Body))
        {
            foreach (var sentence in PupilG78PageTextDedupe.SplitRenderedSentences(
                         PupilG78PageTextDedupe.FinalizeRenderedText(para)))
            {
                Assert.NotEqual(tileNorm, PupilG78PageLint.NormalizeSentence(sentence));
            }
        }
    }

    [Fact]
    public void Mixed_v2_has_strengths_paragraph_or_growth_block()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.MixedToward();
        var package = PupilG78FactStory.Build(result, progress, "nl");
        var hasStrength = PupilG78StoryComposer.HasSubstantiveStrengthParagraph(package.Body);
        var hasGrowth = package.GrowthCompetencies.Count > 0;
        Assert.True(hasStrength || hasGrowth, DescribeMixedCompetencyState(result, progress, package));
    }

    [Fact]
    public void B_heavy_v2_has_three_story_paragraphs_without_strengths_intro()
    {
        var (result, progress) = PupilG78ContrastV2Fixtures.AllTowardB();
        var package = PupilG78FactStory.Build(result, progress, "nl");
        var paragraphs = PupilG78FactStory.SplitParagraphs(package.Body);
        Assert.True(paragraphs.Count >= 3, string.Join(" | ", paragraphs));
        Assert.False(
            PupilG78StoryComposer.HasSubstantiveStrengthParagraph(package.Body),
            "All-B foils: expect no strengths paragraph, only factual overview + values/culture + school.");
    }

    private static string DescribeMixedCompetencyState(
        PupilResult result,
        PupilProgress progress,
        PupilG78StoryPackage package)
    {
        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(progress.AnswersJson!)
                            ?? new Dictionary<string, int>();
        var bank = new PupilQuestionBankG78V2();
        var clauses = new List<G78AnswerClause>();
        foreach (var q in bank.Questions)
        {
            if (!answers.TryGetValue(q.Id.ToString(), out var raw))
            {
                continue;
            }

            var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw, "nl");
            if (fact is null)
            {
                continue;
            }

            var (text, eff, category) = fact.Value;
            clauses.Add(new G78AnswerClause(q.Id, category, q.Model, G78StoryBeat.Doen, text, eff));
        }

        var scores = PupilG78GrowthCompetencies.ContrastDimensionScoresForDiagnostics(clauses, result, answers, contrastV2: true);
        var growth = PupilG78GrowthCompetencies.SelectGrowthCategories(answers, maxCount: 4, contrastV2: true);
        var scoreLine = string.Join(", ", scores.OrderByDescending(kv => kv.Value).Select(kv => $"{PupilG78CompetencyNarrative.KidLabelTitle(kv.Key)}={kv.Value:F2}"));
        var growthLine = string.Join(", ", growth.Select(PupilG78CompetencyNarrative.KidLabelTitle));
        return $"strength={PupilG78StoryComposer.HasSubstantiveStrengthParagraph(package.Body)} growthCount={package.GrowthCompetencies.Count}; "
               + $"threshold growth (avg<3.0 or low&avg<3.35): [{growthLine}]; blended scores: {scoreLine}";
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
        Assert.NotEqual(a.Result.CompetenceScoresJson, b.Result.CompetenceScoresJson);
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
                                  || paragraphRest.Contains("opnieuw", StringComparison.OrdinalIgnoreCase),
               "creativiteit" => paragraphRest.Contains("idee", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("verhaal", StringComparison.OrdinalIgnoreCase)
                                  || paragraphRest.Contains("bedenkt", StringComparison.OrdinalIgnoreCase),
               "samenwerken" => paragraphRest.Contains("samen", StringComparison.OrdinalIgnoreCase)
                                || paragraphRest.Contains("groep", StringComparison.OrdinalIgnoreCase),
               _ => false,
           };

    private static IEnumerable<string> EnumeratePageSentences(PupilG78StoryPackage package)
    {
        foreach (var sentence in SplitRenderedSentences(package.Body))
        {
            yield return sentence;
        }

        foreach (var tile in package.Tiles)
        {
            foreach (var sentence in SplitRenderedSentences(tile.Explanation))
            {
                yield return sentence;
            }
        }
    }

    private static IEnumerable<string> SplitRenderedSentences(string text)
    {
        var polished = PupilG78PageTextDedupe.FinalizeRenderedText(text);
        return PupilG78PageTextDedupe.SplitRenderedSentences(polished);
    }

    private static List<string> SplitSentences(string text)
        => SplitRenderedSentences(text).ToList();
}
