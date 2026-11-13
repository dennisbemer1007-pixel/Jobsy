using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Turns scored answer clauses into flowing B1 paragraphs (deterministic).</summary>
public static class PupilG78StoryComposer
{
    /// <summary>Three to four neutral overview paragraphs; tiles keep per-world detail.</summary>
    public static IReadOnlyList<string> ComposeOverview(
        IReadOnlyList<G78AnswerClause> clauses,
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<int> tileBlockedIds,
        IReadOnlyList<string> tileTexts,
        IReadOnlySet<string> growthCategories,
        IReadOnlyList<string> storyStrongCategories,
        IReadOnlyList<int> competencyExampleIds,
        bool contrastV2,
        out IReadOnlyList<int> usedItemIds,
        out IReadOnlyList<PupilG78CompetencyExampleAnchor> competencyExamples)
    {
        var competencyReserved = competencyExampleIds.ToHashSet();
        var pool = clauses
            .Where(c => !tileBlockedIds.Contains(c.ItemId)
                        && !competencyReserved.Contains(c.ItemId)
                        && c.Model is AssessmentKind.Competence or AssessmentKind.Career)
            .ToList();
        var usedIds = new HashSet<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in tileBlockedIds)
        {
            RememberTheme(id, usedThemes);
        }

        var selected = tileBlockedIds.Union(competencyReserved).ToHashSet();
        foreach (var id in competencyExampleIds)
        {
            RememberTheme(id, usedThemes);
        }

        var avoidSimilar = new List<string>(tileTexts);
        foreach (var tile in tileTexts)
        {
            foreach (var s in tile.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (s.Length > 12)
                {
                    avoidSimilar.Add(s);
                }
            }
        }

        var paragraphs = new List<string>();
        var p1Beat = GrowthBlocksSocialParagraph(growthCategories)
            ? G78StoryBeat.Doen
            : G78StoryBeat.Samen;

        var p1Sentences = new List<string>();
        if (string.Equals(result.TopValue, "Connection", StringComparison.OrdinalIgnoreCase)
            && !growthCategories.Contains(CompetencyTestCatalog.Samenwerken))
        {
            TryAddOpeningClause(clauses, 9024, p1Sentences, selected, usedIds, usedThemes, growthCategories, contrastV2);
            TryAddOpeningClause(clauses, 9054, p1Sentences, selected, usedIds, usedThemes, growthCategories, contrastV2);
            if (p1Sentences.Count < 2)
            {
                TryAddOpeningClause(clauses, 9037, p1Sentences, selected, usedIds, usedThemes, growthCategories, contrastV2);
            }
        }

        var mensen = Pick(pool, clauses, selected, usedThemes, growthCategories, p1Beat, usedCategories, usedIds, avoidSimilar, Math.Max(0, 2 - p1Sentences.Count), similarFilter: true);
        RememberAvoid(avoidSimilar, mensen);
        foreach (var m in mensen)
        {
            p1Sentences.Add(PupilG78StoryFlow.MainClause(m, contrastV2));
            selected.Add(m.ItemId);
        }

        if (p1Sentences.Count < 2 && p1Beat == G78StoryBeat.Samen)
        {
            var mensenExtra = PickCareerSocial(pool, clauses, selected, usedThemes, growthCategories, usedIds, usedCategories, avoidSimilar, 2);
            foreach (var m in mensenExtra)
            {
                p1Sentences.Add(PupilG78StoryFlow.MainClause(m, contrastV2));
                selected.Add(m.ItemId);
            }

            RememberAvoid(avoidSimilar, mensenExtra);
        }

        if (p1Sentences.Count < 2)
        {
            var summary = PupilG78StorySummaries.SocialOverviewLine(result, answers, growthCategories, contrastV2);
            if (summary.Length > 0 && !SentenceOnPage(summary, avoidSimilar))
            {
                p1Sentences.Add(summary);
            }
        }

        if (p1Sentences.Count < 2)
        {
            foreach (var extraId in new[] { 9024, 9054, 9001 })
            {
                if (p1Sentences.Count >= 2 || selected.Contains(extraId))
                {
                    break;
                }

                var clause = clauses.FirstOrDefault(c => c.ItemId == extraId && c.EffectiveScore >= 4);
                if (clause is null
                    || PupilG78StrengthGuard.IsForbiddenStrength(extraId, clause.EffectiveScore, growthCategories))
                {
                    continue;
                }

                p1Sentences.Add(PupilG78StoryFlow.MainClause(clause, contrastV2));
                selected.Add(extraId);
                usedIds.Add(extraId);
                RememberTheme(extraId, usedThemes);
            }
        }

        if (p1Sentences.Count < 2)
        {
            foreach (var extraId in new[] { 9024, 9054, 9001, 9002, 9003 })
            {
                TryAddOpeningClause(clauses, extraId, p1Sentences, selected, usedIds, usedThemes, growthCategories, contrastV2);
                if (p1Sentences.Count >= 2)
                {
                    break;
                }
            }
        }

        if (contrastV2 && p1Sentences.Count < 2)
        {
            foreach (var clause in clauses
                         .Where(c => c.EffectiveScore >= 4
                                     && !selected.Contains(c.ItemId)
                                     && !tileBlockedIds.Contains(c.ItemId)
                                     && c.Model is AssessmentKind.Career or AssessmentKind.Culture or AssessmentKind.Values)
                         .OrderByDescending(c => c.EffectiveScore)
                         .ThenBy(c => c.ItemId))
            {
                if (p1Sentences.Count >= 2)
                {
                    break;
                }

                var line = PupilG78StoryFlow.MainClause(clause, contrastV2: true);
                if (line.Length == 0 || SentenceOnPage(line, avoidSimilar))
                {
                    continue;
                }

                p1Sentences.Add(line);
                selected.Add(clause.ItemId);
                usedIds.Add(clause.ItemId);
                RememberTheme(clause.ItemId, usedThemes);
            }
        }

        if (p1Sentences.Count < 2)
        {
            var social = PupilG78StorySummaries.SocialOverviewLine(result, answers, growthCategories, contrastV2);
            if (social.Length > 0
                && p1Sentences.All(s => !string.Equals(
                    PupilG78PageLint.NormalizeSentence(s),
                    PupilG78PageLint.NormalizeSentence(social),
                    StringComparison.Ordinal)))
            {
                p1Sentences.Add(social);
            }
        }

        if (p1Sentences.Count < 2)
        {
            foreach (var cat in storyStrongCategories)
            {
                var line = PupilG78StorySummaries.CompetencyStrengthLine(cat, answers, growthCategories, contrastV2);
                if (line.Length == 0)
                {
                    continue;
                }

                if (p1Sentences.Any(s => PupilG78JeNarrative.TooSimilar(s, line)))
                {
                    continue;
                }

                p1Sentences.Add(line);
                if (p1Sentences.Count >= 2)
                {
                    break;
                }
            }
        }

        paragraphs.Add(PupilG78StoryFlow.FlowJoin(p1Sentences.Take(2).ToList()));
        foreach (var sentence in SplitStorySentences(paragraphs[^1]))
        {
            if (!SentenceOnPage(sentence, avoidSimilar))
            {
                avoidSimilar.Add(sentence);
            }
        }

        var competencyPara = ComposeCompetencyParagraph(
            clauses,
            storyStrongCategories,
            competencyExampleIds,
            usedIds,
            selected,
            usedThemes,
            avoidSimilar,
            contrastV2,
            out competencyExamples);
        paragraphs.Add(string.IsNullOrWhiteSpace(competencyPara)
            ? FallbackCompetencyParagraph(storyStrongCategories, answers, growthCategories, contrastV2)
            : competencyPara);

        paragraphs.Add(PupilG78StorySummaries.ComposeValuesCultureParagraph(
            result, answers, tileTexts, growthCategories, contrastV2));

        paragraphs.Add(PupilG78StorySummaries.ComposeSchoolHobbiesRiasecParagraph(
            result, progress, vak, language, growthCategories, contrastV2));

        DedupeSentencesAcrossParagraphs(paragraphs, contrastV2);

        if (!contrastV2)
        {
            EnsureEveryParagraphHasTwoSentences(paragraphs, answers, result, progress, vak, language, growthCategories);
        }

        usedItemIds = usedIds.ToList();
        return paragraphs
            .Where(p => p.Length > 0)
            .Take(4)
            .Select(PupilG78StoryFlow.PolishConnectors)
            .Select(PupilQuestionStoryParaphrase.PolishJeForm)
            .Select(PupilG78StoryFlow.EnsureSentenceStartsCapitalized)
            .ToList();
    }

    private static string FallbackCompetencyParagraph(
        IReadOnlyList<string> storyStrongCategories,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> growthCategories,
        bool contrastV2)
    {
        var dims = storyStrongCategories.Count > 0
            ? storyStrongCategories.Take(3).ToList()
            : PupilG78GrowthCompetencies.TopStrongCategories(
                    PupilG78GrowthCompetencies.DimensionAverages(answers, contrastV2), 3)
                .Where(c => !growthCategories.Contains(c))
                .ToList();
        if (dims.Count == 0)
        {
            return "";
        }

        var intro = $"Wat in je antwoorden vooral naar voren komt, is {PupilG78CompetencyNarrative.JoinKidLabelList(dims)}.";
        var summaries = dims
            .Select(d => PupilG78StorySummaries.CompetencyStrengthLine(d, answers, growthCategories, contrastV2))
            .Where(s => s.Length > 0)
            .ToList();
        return PupilG78StoryFlow.FlowJoin(
            summaries.Count == 0 ? [intro] : [intro, .. summaries.Take(2)]);
    }

    public static string ScrubStoryAgainstTiles(string body, IReadOnlyList<string> tileTexts)
    {
        var paragraphs = body.Split(PupilG78FactStory.ParagraphBreak, StringSplitOptions.TrimEntries);
        var scrubbed = paragraphs
            .Select(p => ScrubTileOverlap(p, tileTexts))
            .Where(p => p.Length > 0)
            .ToList();
        return string.Join(PupilG78FactStory.ParagraphBreak, scrubbed);
    }

    private static string ComposeCompetencyParagraph(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlyList<string> storyStrongCategories,
        IReadOnlyList<int> competencyExampleIds,
        HashSet<int> usedIds,
        HashSet<int> selected,
        HashSet<string> usedThemes,
        List<string> avoidSimilar,
        bool contrastV2,
        out IReadOnlyList<PupilG78CompetencyExampleAnchor> anchors)
    {
        var anchorList = new List<PupilG78CompetencyExampleAnchor>();
        anchors = anchorList;
        if (storyStrongCategories.Count == 0)
        {
            return "";
        }

        var topDims = storyStrongCategories.Take(3).ToList();
        var phrases = new List<string>();
        var byId = clauses.ToDictionary(c => c.ItemId);
        var dimsWithExamples = new List<string>();

        var ordinal = 0;
        foreach (var dim in topDims)
        {
            G78AnswerClause? clause = null;
            foreach (var candidateId in PupilG78CompetencyExampleItems.ExampleCandidateIds(
                         dim,
                         competencyExampleIds,
                         clauses,
                         contrastV2))
            {
                if (!byId.TryGetValue(candidateId, out var candidate)
                    || candidate.EffectiveScore < 4)
                {
                    continue;
                }

                if (!PupilG78CompetencyExampleItems.ClauseSupportsCategory(candidate, dim, contrastV2))
                {
                    continue;
                }

                clause = candidate;
                break;
            }

            if (clause is null)
            {
                continue;
            }

            var pickedId = clause.ItemId;
            string phrase;
            if (contrastV2)
            {
                var main = PupilG78StoryFlow.MainClause(clause, contrastV2: true).TrimEnd('.');
                if (main.Length == 0)
                {
                    continue;
                }

                phrase = main;
            }
            else if (!PupilG78CompetencyZoForms.TryStem(pickedId, out var stem))
            {
                continue;
            }
            else
            {
                phrase = stem;
            }

            anchorList.Add(new PupilG78CompetencyExampleAnchor(dim, pickedId));
            usedIds.Add(pickedId);
            selected.Add(pickedId);
            RememberTheme(pickedId, usedThemes);
            phrases.Add(phrase);
            dimsWithExamples.Add(dim);
            ordinal++;
        }

        if (dimsWithExamples.Count == 0)
        {
            anchors = anchorList;
            return "";
        }

        var introDims = dimsWithExamples;
        var intro = $"Wat in je antwoorden vooral naar voren komt, is {PupilG78CompetencyNarrative.JoinKidLabelList(introDims)}.";

        if (phrases.Count == 0)
        {
            return "";
        }

        var examples = contrastV2
            ? PupilG78StoryFlow.FlowJoin(phrases)
            : PupilG78CompetencyZoForms.JoinExampleStems(phrases);
        if (SentenceOnPage(examples, avoidSimilar))
        {
            return intro;
        }

        avoidSimilar.Add(examples);
        return PupilG78StoryFlow.EnsureSentenceStartsCapitalized(intro + " " + examples);
    }

    private static void TryAddOpeningClause(
        IReadOnlyList<G78AnswerClause> clauses,
        int itemId,
        List<string> sentences,
        HashSet<int> selected,
        HashSet<int> usedIds,
        HashSet<string> usedThemes,
        IReadOnlySet<string> growthCategories,
        bool contrastV2)
    {
        if (sentences.Count >= 2 || selected.Contains(itemId))
        {
            return;
        }

        var clause = clauses.FirstOrDefault(c => c.ItemId == itemId && c.EffectiveScore >= 4);
        if (clause is null
            || PupilG78StrengthGuard.IsForbiddenStrength(itemId, clause.EffectiveScore, growthCategories))
        {
            return;
        }

        sentences.Add(PupilG78StoryFlow.MainClause(clause, contrastV2));
        selected.Add(itemId);
        usedIds.Add(itemId);
        RememberTheme(itemId, usedThemes);
    }

    private static bool GrowthBlocksSocialParagraph(IReadOnlySet<string> growthCategories)
        => growthCategories.Contains(CompetencyTestCatalog.Samenwerken)
           || growthCategories.Contains(CompetencyTestCatalog.Extraversie);

    private static bool SentenceOnPage(string sentence, IReadOnlyList<string> avoidSimilar)
    {
        var norm = PupilG78PageLint.NormalizeSentence(sentence);
        foreach (var other in avoidSimilar)
        {
            if (PupilG78PageLint.NormalizeSentence(other) == norm)
            {
                return true;
            }
        }

        return false;
    }

    private static string PickParagraphPaddingSentence(
        int paragraphIndex,
        string paragraph,
        PupilResult result,
        IReadOnlySet<string> growthCategories)
    {
        string[] candidates = paragraphIndex switch
        {
            2 =>
            [
                PupilG78StorySummaries.TopCultureSummary(result.TopCulture),
                PupilG78StorySummaries.TopValueSummary(result.TopValue),
                "Je weet goed wat je belangrijk vindt en waar je je op je gemak voelt."
            ],
            3 => [PupilG78StorySummaries.RiasecTopTwoSentence(result, growthCategories)],
            _ => []
        };

        foreach (var candidate in candidates)
        {
            if (candidate.Length == 0)
            {
                continue;
            }

            var stem = candidate.TrimEnd('.');
            if (!paragraph.Contains(stem, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return "";
    }

    private static void EnsureEveryParagraphHasTwoSentences(
        List<string> paragraphs,
        IReadOnlyDictionary<string, int> answers,
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language,
        IReadOnlySet<string> growthCategories)
    {
        _ = progress;
        _ = vak;
        _ = language;
        _ = answers;
        for (var i = 0; i < paragraphs.Count; i++)
        {
            if (SentenceCount(paragraphs[i]) >= 2)
            {
                continue;
            }

            var extra = PickParagraphPaddingSentence(i, paragraphs[i], result, growthCategories);
            if (extra.Length == 0)
            {
                continue;
            }

            paragraphs[i] = PupilG78StoryFlow.FlowJoin([paragraphs[i].TrimEnd('.'), extra]);
        }
    }

    private static IReadOnlyList<G78AnswerClause> PickCareerSocial(
        List<G78AnswerClause> pool,
        IReadOnlyList<G78AnswerClause> allClauses,
        HashSet<int> selected,
        HashSet<string> usedThemes,
        IReadOnlySet<string> growthCategories,
        HashSet<int> usedIds,
        HashSet<string> usedCategories,
        List<string> avoidSimilar,
        int max)
    {
        var blocked = PupilG78OppositePairs.BlockedItems(allClauses, selected);
        var picked = new List<G78AnswerClause>();
        foreach (var c in pool
                     .Where(c => c.Model == AssessmentKind.Career
                                 && c.Beat == G78StoryBeat.Samen
                                 && c.EffectiveScore >= 4
                                 && !usedIds.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (usedCategories.Contains(c.Category) && picked.Count > 0)
            {
                continue;
            }

            if (avoidSimilar.Any(t => PupilG78JeNarrative.TooSimilar(t, c.Text)))
            {
                continue;
            }

            if (ThemeBlocked(c.ItemId, usedThemes))
            {
                continue;
            }

            if (PupilG78OppositePairs.WouldCreatePageTension(c.ItemId, selected))
            {
                continue;
            }

            picked.Add(c);
            selected.Add(c.ItemId);
            RememberTheme(c.ItemId, usedThemes);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            if (picked.Count >= max)
            {
                break;
            }
        }

        return picked;
    }

    private static string ScrubTileOverlap(string paragraph, IReadOnlyList<string> tileTexts)
    {
        foreach (var tile in tileTexts)
        {
            foreach (var sentence in SplitSentences(tile))
            {
                if (sentence.Length <= 30)
                {
                    continue;
                }

                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                paragraph = RemoveSentenceWithNorm(paragraph, norm);
            }
        }

        return PupilG78StoryFlow.EnsureSentenceStartsCapitalized(paragraph.Trim());
    }

    private static string RemoveSentenceWithNorm(string paragraph, string forbiddenNorm)
    {
        var kept = new List<string>();
        foreach (var part in paragraph.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var s = part.TrimEnd('.') + ".";
            if (PupilG78PageLint.NormalizeSentence(s) == forbiddenNorm)
            {
                continue;
            }

            kept.Add(part.TrimEnd('.'));
        }

        return kept.Count == 0 ? "" : PupilG78StoryFlow.FlowJoin(kept);
    }

    private static IEnumerable<string> SplitSentences(string text)
        => text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.TrimEnd('.') + ".");

    public static IReadOnlyList<string> ComposeParagraphs(
        IReadOnlyList<G78AnswerClause> clauses,
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language,
        out IReadOnlyList<int> usedItemIds)
    {
        var usedIds = new HashSet<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var avoidSimilar = new List<string>();
        var paragraphs = new List<string>();

        var selected = new HashSet<int>();
        var usedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var noGrowth = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var doen = Pick(clauses, clauses, selected, usedThemes, noGrowth, G78StoryBeat.Doen, usedCategories, usedIds, avoidSimilar, 2, similarFilter: true);
        RememberAvoid(avoidSimilar, doen);
        var doenLow = PickLow(clauses, G78StoryBeat.Doen, usedIds, 1, skipSimilarTo: doen);
        if (doen.Count > 0)
        {
            paragraphs.Add(ComposeDoen(doen, doenLow));
        }

        var samen = Pick(clauses, clauses, selected, usedThemes, noGrowth, G78StoryBeat.Samen, usedCategories, usedIds, avoidSimilar, 2, similarFilter: true);
        RememberAvoid(avoidSimilar, samen);
        if (samen.Count > 0)
        {
            paragraphs.Add(ComposeSamen(samen));
        }

        var waarden = Pick(clauses, clauses, selected, usedThemes, noGrowth, G78StoryBeat.Waarden, usedCategories, usedIds, avoidSimilar, 2);
        if (waarden.Count > 0)
        {
            paragraphs.Add(ComposeWaarden(waarden));
        }

        var schoolLater = ComposeSchoolLater(result, progress, vak, language);
        if (!string.IsNullOrWhiteSpace(schoolLater))
        {
            paragraphs.Add(schoolLater);
        }

        var closing = ClosingSentence(result, language, []);
        if (!string.IsNullOrWhiteSpace(closing) && paragraphs.Count > 0)
        {
            paragraphs[^1] = paragraphs[^1].TrimEnd() + " " + closing;
        }

        if (paragraphs.Count == 0)
        {
            paragraphs.Add(PupilVerhaalLocalized.IsEnglish(language)
                ? "You answered the questions yourself. There is no right or wrong."
                : "Je gaf zelf antwoord op de vragen. Er is geen goed of fout.");
        }

        paragraphs = MergeThinMiddleParagraphs(paragraphs);

        while (paragraphs.Count > 4)
        {
            paragraphs.RemoveAt(paragraphs.Count - 2);
        }

        usedItemIds = usedIds.ToList();
        return paragraphs.Select(PupilQuestionStoryParaphrase.PolishJeForm).ToList();
    }

    private static string ComposeDoen(IReadOnlyList<G78AnswerClause> highs, IReadOnlyList<G78AnswerClause> lows)
    {
        var parts = new List<string>();
        if (highs.Count >= 2)
        {
            parts.Add(PupilG78JeNarrative.MergeTwo(highs[0].Text, highs[1].Text));
        }
        else if (highs.Count == 1)
        {
            parts.Add(PupilG78JeNarrative.AsSentence(highs[0].Text));
        }

        if (lows.Count > 0 && IsDoenContrast(highs, lows[0]))
        {
            parts.Add(ContrastLine(lows[0].Text));
        }

        return string.Join(' ', parts);
    }

    private static string ComposeSamen(IReadOnlyList<G78AnswerClause> highs)
    {
        if (highs.Count >= 2)
        {
            return PupilG78JeNarrative.MergeTwo(highs[0].Text, highs[1].Text);
        }

        return PupilG78JeNarrative.AsSentence(highs[0].Text);
    }

    private static string ComposeWaarden(IReadOnlyList<G78AnswerClause> highs)
    {
        if (highs.Count >= 2)
        {
            var first = PupilG78JeNarrative.AsSentence(highs[0].Text).TrimEnd('.');
            var second = PupilG78JeNarrative.AsSentence(highs[1].Text).TrimEnd('.');
            return $"{first}, en {PupilG78JeNarrative.JeContinuation(second)}.";
        }

        return PupilG78JeNarrative.AsSentence(highs[0].Text);
    }

    private static bool IsDoenContrast(IReadOnlyList<G78AnswerClause> highs, G78AnswerClause low)
    {
        if (highs.Count == 0)
        {
            return true;
        }

        if (low.Text.StartsWith("Juist hier zie je het anders", StringComparison.Ordinal))
        {
            return true;
        }

        if (low.Text.StartsWith("Dit zei je minder vaak", StringComparison.Ordinal))
        {
            return !highs.Any(h => PupilG78JeNarrative.TooSimilar(h.Text, low.Text));
        }

        return !highs.Any(h => PupilG78JeNarrative.TooSimilar(h.Text, low.Text));
    }

    private static List<string> MergeThinMiddleParagraphs(List<string> paragraphs)
    {
        if (paragraphs.Count < 3)
        {
            return paragraphs;
        }

        var i = 1;
        while (i < paragraphs.Count - 1)
        {
            if (SentenceCount(paragraphs[i]) == 1 || SentenceCount(paragraphs[i + 1]) == 1)
            {
                paragraphs[i] = paragraphs[i].TrimEnd() + " " + paragraphs[i + 1].TrimStart();
                paragraphs.RemoveAt(i + 1);
                continue;
            }

            i++;
        }

        return paragraphs;
    }

    private static int SentenceCount(string paragraph)
    {
        if (string.IsNullOrWhiteSpace(paragraph))
        {
            return 0;
        }

        return paragraph.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    private static string ContrastLine(string lowSentence)
    {
        var s = lowSentence.Trim().TrimEnd('.');
        if (s.StartsWith("Dit zei je minder vaak over jezelf: ", StringComparison.Ordinal))
        {
            var inner = s["Dit zei je minder vaak over jezelf: ".Length..].Trim().TrimEnd('.');
            return ContrastFromJeInner(inner);
        }

        if (s.StartsWith("Juist hier zie je het anders: ", StringComparison.Ordinal))
        {
            var inner = s["Juist hier zie je het anders: ".Length..].Trim().TrimEnd('.');
            return ContrastFromJeInner(inner);
        }

        if (s.StartsWith("Je ", StringComparison.Ordinal))
        {
            return ContrastFromJeInner("je " + s[3..]);
        }

        return ContrastFromJeInner(s);
    }

    private static string ContrastFromJeInner(string inner)
    {
        inner = inner.Trim().TrimEnd('.');
        if (inner.StartsWith("Je ", StringComparison.Ordinal))
        {
            inner = "je " + inner[3..];
        }

        return $"Tegelijk {inner}.";
    }

    private static void RememberAvoid(List<string> avoid, IReadOnlyList<G78AnswerClause> picked)
    {
        foreach (var c in picked)
        {
            avoid.Add(c.Text);
        }
    }

    private static IReadOnlyList<G78AnswerClause> Pick(
        IReadOnlyList<G78AnswerClause> pool,
        IReadOnlyList<G78AnswerClause> allClauses,
        HashSet<int> selected,
        HashSet<string> usedThemes,
        IReadOnlySet<string> growthCategories,
        G78StoryBeat beat,
        HashSet<string> usedCategories,
        HashSet<int> usedIds,
        IReadOnlyList<string> avoidSimilarTexts,
        int max,
        bool similarFilter = false)
    {
        var blocked = PupilG78OppositePairs.BlockedItems(allClauses, selected);
        var picked = new List<G78AnswerClause>();
        foreach (var c in pool
                     .Where(c => c.Beat == beat
                                 && c.EffectiveScore >= 4
                                 && !usedCategories.Contains(c.Category)
                                 && !usedIds.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (similarFilter && picked.Count > 0 && PupilG78JeNarrative.TooSimilar(picked[0].Text, c.Text))
            {
                continue;
            }

            if (avoidSimilarTexts.Any(t => PupilG78JeNarrative.TooSimilar(t, c.Text)))
            {
                continue;
            }

            var jeClause = PupilG78QuestionJeForms.ForItem(c.ItemId);
            if (jeClause is not null
                && avoidSimilarTexts.Any(t => t.Contains(jeClause.TrimEnd('.'), StringComparison.Ordinal)))
            {
                continue;
            }

            if (ThemeBlocked(c.ItemId, usedThemes))
            {
                continue;
            }

            if (PupilG78OppositePairs.WouldCreatePageTension(c.ItemId, selected))
            {
                continue;
            }

            picked.Add(c);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            selected.Add(c.ItemId);
            RememberTheme(c.ItemId, usedThemes);
            if (picked.Count >= max)
            {
                break;
            }
        }

        return picked;
    }

    private static bool ThemeBlocked(int itemId, HashSet<string> usedThemes)
    {
        var key = PupilG78ThemeKeys.DedupeKey(itemId);
        return key is not null && usedThemes.Contains(key);
    }

    private static void RememberTheme(int itemId, HashSet<string> usedThemes)
    {
        var key = PupilG78ThemeKeys.DedupeKey(itemId);
        if (key is not null)
        {
            usedThemes.Add(key);
        }
    }

    private static IReadOnlyList<G78AnswerClause> PickLow(
        IReadOnlyList<G78AnswerClause> all,
        G78StoryBeat beat,
        HashSet<int> usedIds,
        int max,
        IReadOnlyList<G78AnswerClause> skipSimilarTo)
    {
        var picked = new List<G78AnswerClause>();
        foreach (var c in all
                     .Where(c => c.Beat == beat
                                 && c.EffectiveScore <= 2
                                 && !usedIds.Contains(c.ItemId))
                     .OrderBy(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (skipSimilarTo.Count > 0 && skipSimilarTo.Any(h => PupilG78JeNarrative.TooSimilar(h.Text, c.Text)))
            {
                continue;
            }

            picked.Add(c);
            usedIds.Add(c.ItemId);
            if (picked.Count >= max)
            {
                break;
            }
        }

        return picked;
    }

    private static string ComposeSchoolLater(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language)
    {
        var bits = new List<string>();
        var vakNames = vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Take(3)
            .Select(e => PupilVakSubjectLabels.Nl(e.SubjectKey))
            .ToList();
        var hobbies = HobbyLabels(progress, language);
        if (vakNames.Count > 0 && hobbies.Count > 0)
        {
            bits.Add(
                $"Op school vind je vooral {JoinDutchList(vakNames)} leuk, en in je vrije tijd ben je graag bezig met {JoinDutchList(hobbies)}.");
        }
        else if (vakNames.Count > 0)
        {
            bits.Add($"Op school vind je vooral {JoinDutchList(vakNames)} leuk om te doen.");
        }
        else if (hobbies.Count > 0)
        {
            bits.Add($"In je vrije tijd ben je graag bezig met {JoinDutchList(hobbies)}.");
        }

        var dream = DreamPhrase(result, progress, language);
        if (!string.IsNullOrWhiteSpace(dream))
        {
            bits.Add(dream);
        }

        return bits.Count == 0 ? "" : string.Join(' ', bits);
    }

    private static string? DreamPhrase(PupilResult result, PupilProgress? progress, string language)
    {
        var key = progress?.DreamJobKey ?? result.DreamJobKey;
        if (string.IsNullOrWhiteSpace(key)
            || string.Equals(key, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var title = PupilPassportLocalizedCopy.JobTitle(key.Trim(), language).ToLowerInvariant();
        return $"Je gaf aan dat je later misschien meer wilt weten over werk als {title}.";
    }

    private static string ClosingSentence(
        PupilResult result,
        string language,
        IReadOnlyList<int> tileLowItemIds,
        bool suppressHelpInStory = false)
    {
        if (PupilVerhaalLocalized.IsEnglish(language))
        {
            return "";
        }

        var holland = (result.HollandCode ?? "").Trim().ToUpperInvariant();
        var riasecBits = new List<string>();
        foreach (var ch in holland)
        {
            if (suppressHelpInStory && ch == 'S')
            {
                continue;
            }

            if (PupilG78AnswerConflicts.HollandLetterConflictsWithTileLows(ch, tileLowItemIds))
            {
                continue;
            }

            var bit = ch switch
            {
                'R' => "graag met de handen bezig is",
                'I' => "graag wil weten hoe dingen werken",
                'A' => "graag creatief bezig is",
                'S' => "anderen graag helpt",
                'E' => "graag het voortouw neemt",
                'C' => "van overzicht en duidelijke stappen houdt",
                _ => null
            };
            if (bit is not null && !riasecBits.Contains(bit))
            {
                riasecBits.Add(bit);
            }
        }

        riasecBits = riasecBits.Take(2).ToList();
        var value = ValueClosingPhrase(result.TopValue);
        if (riasecBits.Count == 0 && value is null)
        {
            return "";
        }

        if (riasecBits.Count >= 2 && value is not null)
        {
            return $"Je bent dus iemand die {riasecBits[0]} en {riasecBits[1]}, en voor wie {value}.";
        }

        if (riasecBits.Count == 1 && value is not null)
        {
            return $"Je bent dus iemand die {riasecBits[0]}, en voor wie {value}.";
        }

        return riasecBits.Count switch
        {
            >= 2 => $"Je bent dus iemand die {riasecBits[0]} en {riasecBits[1]}.",
            1 => $"Je bent dus iemand die {riasecBits[0]}.",
            _ => value is null ? "" : $"Je bent dus iemand voor wie {value}."
        };
    }

    private static string? ValueClosingPhrase(string? topValue) => topValue?.Trim() switch
    {
        "Connection" => "verbinding met anderen belangrijk is",
        "Autonomy" => "zelf mogen kiezen belangrijk is",
        "Achievement" => "iets goed afmaken belangrijk is",
        "Stability" => "rust en duidelijkheid belangrijk is",
        "Impact" => "verschil maken voor anderen belangrijk is",
        _ => null
    };

    private static List<string> HobbyLabels(PupilProgress? progress, string language)
    {
        var likes = ParseChips(progress?.LikesJson)
            .Select(k => PupilPortalChipLabels.Label(k, language))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            likes.Add(progress.LikeOtherWord.Trim());
        }

        return likes.Take(4).ToList();
    }

    private static IReadOnlyList<string> ParseChips(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string JoinDutchList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };

    private static IEnumerable<string> SplitStorySentences(string text)
    {
        foreach (var part in text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var s = part.Trim();
            if (s.Length > 0)
            {
                yield return s.EndsWith('.') ? s : s + ".";
            }
        }
    }

    private static void DedupeSentencesAcrossParagraphs(List<string> paragraphs, bool contrastV2 = false)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < paragraphs.Count; i++)
        {
            var kept = new List<string>();
            foreach (var sentence in SplitStorySentences(paragraphs[i]))
            {
                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                if (norm.Length >= 15 && !seen.Add(norm))
                {
                    continue;
                }

                kept.Add(sentence);
            }

            paragraphs[i] = kept.Count == 0 ? "" : PupilG78StoryFlow.FlowJoin(kept);
        }

        paragraphs.RemoveAll(static p => string.IsNullOrWhiteSpace(p));
        ReconcileCompetencyParagraphs(paragraphs, contrastV2);
    }

    private static void ReconcileCompetencyParagraphs(List<string> paragraphs, bool contrastV2 = false)
    {
        for (var i = 0; i < paragraphs.Count; i++)
        {
            if (!paragraphs[i].StartsWith("Wat in je antwoorden vooral naar voren komt", StringComparison.Ordinal))
            {
                continue;
            }

            var firstDot = paragraphs[i].IndexOf('.', StringComparison.Ordinal);
            if (firstDot < 0 || paragraphs[i].Length <= firstDot + 2)
            {
                if (contrastV2 && PupilG78PageLint.CountCompetencyIntroLabels(paragraphs[i]) >= 2)
                {
                    continue;
                }

                paragraphs[i] = "";
            }
        }

        paragraphs.RemoveAll(static p => string.IsNullOrWhiteSpace(p));
    }

    /// <summary>After tile dedupe, drop strength labels whose example sentence was removed from the story.</summary>
    public static string SyncStrengthIntroAfterTileDedupe(string body)
    {
        var paragraphs = PupilG78FactStory.SplitParagraphs(body).ToList();
        for (var i = 0; i < paragraphs.Count; i++)
        {
            if (!paragraphs[i].StartsWith("Wat in je antwoorden vooral naar voren komt", StringComparison.Ordinal))
            {
                continue;
            }

            var dot = paragraphs[i].IndexOf('.', StringComparison.Ordinal);
            if (dot < 0)
            {
                continue;
            }

            var intro = paragraphs[i][..(dot + 1)];
            var rest = paragraphs[i][(dot + 1)..].Trim();
            var labels = KidLabelsFromIntroSentence(intro);
            var kept = labels
                .Where(label => LabelSupportedInParagraphText(label, rest))
                .ToList();
            if (kept.Count == 0)
            {
                paragraphs.RemoveAt(i);
                break;
            }

            var newIntro = $"Wat in je antwoorden vooral naar voren komt, is {PupilG78CompetencyNarrative.JoinKidLabelList(MapLabelsToCategories(kept))}.";
            paragraphs[i] = rest.Length > 0
                ? PupilG78StoryFlow.EnsureSentenceStartsCapitalized(newIntro + " " + rest)
                : newIntro;
            break;
        }

        return string.Join(PupilG78FactStory.ParagraphBreak, paragraphs);
    }

    private static IReadOnlyList<string> MapLabelsToCategories(IReadOnlyList<string> kidLabels)
    {
        var list = new List<string>();
        foreach (var kid in kidLabels)
        {
            var cat = CompetencyTestCatalog.QuickScanCategories.FirstOrDefault(c =>
                string.Equals(PupilG78CompetencyNarrative.KidLabelList(c), kid, StringComparison.OrdinalIgnoreCase));
            if (cat is not null)
            {
                list.Add(cat);
            }
        }

        return list;
    }

    private static List<string> KidLabelsFromIntroSentence(string intro)
    {
        const string prefix = "Wat in je antwoorden vooral naar voren komt, is ";
        if (!intro.StartsWith(prefix, StringComparison.Ordinal))
        {
            return [];
        }

        var listPart = intro[prefix.Length..].TrimEnd('.').Trim();
        var labels = new List<string>();
        foreach (var chunk in listPart.Split(',', StringSplitOptions.TrimEntries))
        {
            foreach (var part in chunk.Split(" en ", StringSplitOptions.TrimEntries))
            {
                if (part.Length > 0)
                {
                    labels.Add(part);
                }
            }
        }

        return labels;
    }

    private static bool LabelSupportedInParagraphText(string kidLabel, string paragraphRest)
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

    public static bool HasSubstantiveStrengthParagraph(string body)
    {
        foreach (var paragraph in PupilG78FactStory.SplitParagraphs(body))
        {
            if (!paragraph.StartsWith("Wat in je antwoorden vooral naar voren komt", StringComparison.Ordinal))
            {
                continue;
            }

            if (PupilG78PageLint.CountCompetencyIntroLabels(paragraph) >= 2)
            {
                return true;
            }

            if (SplitStorySentences(paragraph).Count() >= 2)
            {
                return true;
            }
        }

        return false;
    }
}
