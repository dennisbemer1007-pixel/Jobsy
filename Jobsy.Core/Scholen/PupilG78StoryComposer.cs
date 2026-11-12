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
            TryAddOpeningClause(clauses, 9024, p1Sentences, selected, usedIds, usedThemes, growthCategories);
            TryAddOpeningClause(clauses, 9054, p1Sentences, selected, usedIds, usedThemes, growthCategories);
        }

        var mensen = Pick(pool, clauses, selected, usedThemes, growthCategories, p1Beat, usedCategories, usedIds, avoidSimilar, Math.Max(0, 2 - p1Sentences.Count), similarFilter: true);
        RememberAvoid(avoidSimilar, mensen);
        foreach (var m in mensen)
        {
            p1Sentences.Add(PupilG78StoryFlow.MainClause(m));
            selected.Add(m.ItemId);
        }

        if (p1Sentences.Count < 2 && p1Beat == G78StoryBeat.Samen)
        {
            var mensenExtra = PickCareerSocial(pool, clauses, selected, usedThemes, growthCategories, usedIds, usedCategories, avoidSimilar, 2);
            foreach (var m in mensenExtra)
            {
                p1Sentences.Add(PupilG78StoryFlow.MainClause(m));
                selected.Add(m.ItemId);
            }

            RememberAvoid(avoidSimilar, mensenExtra);
        }

        if (p1Sentences.Count < 2)
        {
            var summary = PupilG78StorySummaries.SocialOverviewLine(result, answers, growthCategories);
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

                p1Sentences.Add(PupilG78StoryFlow.MainClause(clause));
                selected.Add(extraId);
                usedIds.Add(extraId);
                RememberTheme(extraId, usedThemes);
            }
        }

        if (p1Sentences.Count < 2)
        {
            p1Sentences.Add("Je hebt duidelijke voorkeuren in hoe je iets aanpakt.");
        }

        paragraphs.Add(PupilG78StoryFlow.FlowJoin(p1Sentences.Take(2).ToList()));

        var competencyPara = ComposeCompetencyParagraph(
            clauses,
            storyStrongCategories,
            competencyExampleIds,
            usedIds,
            selected,
            usedThemes,
            avoidSimilar,
            out competencyExamples);
        paragraphs.Add(string.IsNullOrWhiteSpace(competencyPara)
            ? FallbackCompetencyParagraph(storyStrongCategories, answers, growthCategories)
            : competencyPara);

        paragraphs.Add(PupilG78StorySummaries.ComposeValuesCultureParagraph(result, answers));

        paragraphs.Add(PupilG78StorySummaries.ComposeSchoolHobbiesRiasecParagraph(
            result, progress, vak, language, growthCategories));

        EnsureEveryParagraphHasTwoSentences(paragraphs, answers, result, progress, vak, language, growthCategories);

        usedItemIds = usedIds.ToList();
        return paragraphs
            .Where(p => p.Length > 0)
            .Take(4)
            .Select(PupilG78StoryFlow.PolishConnectors)
            .Select(PupilQuestionStoryParaphrase.PolishJeForm)
            .ToList();
    }

    private static string FallbackCompetencyParagraph(
        IReadOnlyList<string> storyStrongCategories,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> growthCategories)
    {
        var dims = storyStrongCategories.Count > 0
            ? storyStrongCategories.Take(3).ToList()
            : PupilG78GrowthCompetencies.TopStrongCategories(
                    PupilG78GrowthCompetencies.DimensionAverages(answers), 3)
                .Where(c => !growthCategories.Contains(c))
                .ToList();
        if (dims.Count == 0)
        {
            return "";
        }

        var intro = $"Wat in je antwoorden vooral naar voren komt, is {PupilG78CompetencyNarrative.JoinKidLabelList(dims)}.";
        var summaries = dims
            .Select(d => PupilG78StorySummaries.CompetencyStrengthLine(d, answers))
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
        out IReadOnlyList<PupilG78CompetencyExampleAnchor> anchors)
    {
        var anchorList = new List<PupilG78CompetencyExampleAnchor>();
        anchors = anchorList;
        if (storyStrongCategories.Count == 0)
        {
            return "";
        }

        var topDims = storyStrongCategories.Take(3).ToList();
        var intro = $"Wat in je antwoorden vooral naar voren komt, is {PupilG78CompetencyNarrative.JoinKidLabelList(topDims)}.";
        var phrases = new List<string>();
        var byId = clauses.ToDictionary(c => c.ItemId);

        var ordinal = 0;
        foreach (var dim in topDims)
        {
            var itemId = PupilG78CompetencyExampleItems.ForCategory(dim)
                .FirstOrDefault(id =>
                    competencyExampleIds.Contains(id)
                    && byId.TryGetValue(id, out var c)
                    && c.EffectiveScore >= 4);
            if (itemId == 0
                || !byId.TryGetValue(itemId, out var clause)
                || clause.EffectiveScore < 4
                || !PupilG78CompetencyExampleItems.ItemMatchesCategory(itemId, dim))
            {
                continue;
            }

            if (!PupilG78CompetencyZoForms.TryPhrase(itemId, ordinal, out var phrase))
            {
                continue;
            }

            if (SentenceOnPage(phrase, avoidSimilar))
            {
                continue;
            }

            phrases.Add(phrase);
            anchorList.Add(new PupilG78CompetencyExampleAnchor(dim, itemId));
            usedIds.Add(itemId);
            selected.Add(itemId);
            RememberTheme(itemId, usedThemes);
            avoidSimilar.Add(phrase);
            ordinal++;
        }

        if (phrases.Count == 0)
        {
            return intro;
        }

        return intro + " " + string.Join(' ', phrases);
    }

    private static void TryAddOpeningClause(
        IReadOnlyList<G78AnswerClause> clauses,
        int itemId,
        List<string> sentences,
        HashSet<int> selected,
        HashSet<int> usedIds,
        HashSet<string> usedThemes,
        IReadOnlySet<string> growthCategories)
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

        sentences.Add(PupilG78StoryFlow.MainClause(clause));
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

            var extra = i switch
            {
                2 => PupilG78StorySummaries.TopCultureSummary(result.TopCulture),
                3 => PupilG78StorySummaries.RiasecTopTwoSentence(result, growthCategories),
                _ => ""
            };
            if (extra.Length == 0 || paragraphs[i].Contains(extra.TrimEnd('.'), StringComparison.Ordinal))
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
                if (sentence.Length > 30 && paragraph.Contains(sentence, StringComparison.Ordinal))
                {
                    paragraph = paragraph.Replace(sentence, "", StringComparison.Ordinal).Trim();
                }
            }
        }

        return paragraph;
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
}
