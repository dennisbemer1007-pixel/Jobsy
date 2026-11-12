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
        IReadOnlySet<int> reservedItemIds,
        IReadOnlyList<string> tileTexts,
        IReadOnlyList<int> tileLowItemIds,
        IReadOnlyList<string> storyStrongCategories,
        out IReadOnlyList<int> usedItemIds)
    {
        var pool = clauses.Where(c => !reservedItemIds.Contains(c.ItemId)).ToList();
        var usedIds = new HashSet<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var avoidSimilar = new List<string>(tileTexts);
        var paragraphs = new List<string>();
        var suppressHelpInStory = tileLowItemIds.Contains(9019)
            || tileTexts.Any(t => t.Contains("uitleggen is minder", StringComparison.OrdinalIgnoreCase));

        var mensen = Pick(pool, G78StoryBeat.Samen, usedCategories, usedIds, avoidSimilar, 2, similarFilter: true, tileLowItemIds, suppressHelpInStory);
        RememberAvoid(avoidSimilar, mensen);
        var mensenExtra = PickCareerSocial(pool, usedIds, usedCategories, avoidSimilar, tileLowItemIds, suppressHelpInStory, Math.Max(0, 3 - mensen.Count));
        RememberAvoid(avoidSimilar, mensenExtra);
        var p1Bits = mensen.Concat(mensenExtra).Take(2).ToList();
        if (p1Bits.Count > 0)
        {
            paragraphs.Add(BuildFactParagraph(p1Bits));
        }

        var dimAverages = PupilG78CompetencyNarrative.DimensionAverages(clauses);
        var competencyPara = ComposeCompetencyParagraph(
            pool,
            storyStrongCategories,
            dimAverages,
            usedIds,
            usedCategories,
            avoidSimilar,
            tileLowItemIds,
            suppressHelpInStory);
        if (!string.IsNullOrWhiteSpace(competencyPara))
        {
            paragraphs.Add(competencyPara);
        }

        var waarden = Pick(pool, G78StoryBeat.Waarden, usedCategories, usedIds, avoidSimilar, 2, tileLowItemIds: tileLowItemIds, suppressHelpInStory: suppressHelpInStory);
        RememberAvoid(avoidSimilar, waarden);
        var career = PickCareerHighs(pool, usedIds, usedCategories, avoidSimilar, tileLowItemIds, suppressHelpInStory, 2);
        RememberAvoid(avoidSimilar, career);
        var p3Bits = waarden.Concat(career).Take(4).ToList();
        if (p3Bits.Count > 0)
        {
            paragraphs.Add(BuildFactParagraph(p3Bits));
        }

        var p4 = ComposeLevenOpSchoolParagraph(result, progress, vak, language, pool, usedIds, usedCategories, avoidSimilar, tileLowItemIds, suppressHelpInStory);
        if (!string.IsNullOrWhiteSpace(p4))
        {
            paragraphs.Add(p4);
        }

        PadParagraphsFromPool(paragraphs, pool, usedIds, usedCategories, avoidSimilar, tileLowItemIds, suppressHelpInStory);

        while (paragraphs.Count > 4)
        {
            paragraphs.RemoveAt(paragraphs.Count - 1);
        }

        paragraphs = paragraphs
            .Select(p => ScrubTileOverlap(p, tileTexts))
            .Where(p => p.Length > 0)
            .Select(EnsureRichParagraph)
            .Where(p => p.Length > 0)
            .ToList();

        usedItemIds = usedIds.ToList();
        return paragraphs
            .Select(PupilG78StoryFlow.PolishConnectors)
            .Select(PupilQuestionStoryParaphrase.PolishJeForm)
            .ToList();
    }

    private static string BuildFactParagraph(IReadOnlyList<G78AnswerClause> bits)
    {
        var sentences = bits
            .Select(PupilG78StoryFlow.MainClause)
            .Where(s => s.Length > 0)
            .ToList();
        return PupilG78StoryFlow.FlowJoin(sentences);
    }

    private static string ComposeCompetencyParagraph(
        List<G78AnswerClause> pool,
        IReadOnlyList<string> storyStrongCategories,
        Dictionary<string, double> dimAverages,
        HashSet<int> usedIds,
        HashSet<string> usedCategories,
        List<string> avoidSimilar,
        IReadOnlyList<int> tileLowItemIds,
        bool suppressHelpInStory)
    {
        if (dimAverages.Count == 0 || storyStrongCategories.Count == 0)
        {
            return "";
        }

        var topDims = storyStrongCategories.Take(3).ToList();
        var labels = topDims.Select(PupilG78CompetencyNarrative.KidLabel).ToList();
        if (labels.Count == 0)
        {
            return "";
        }

        var intro = labels.Count switch
        {
            >= 2 => $"Wat in je antwoorden vooral naar voren komt, is {JoinDutchList(labels)}.",
            1 => $"Wat in je antwoorden vooral naar voren komt, is {labels[0]}.",
            _ => ""
        };

        var examples = new List<string>();
        foreach (var dim in topDims)
        {
            var pick = pool.FirstOrDefault(c =>
                c.Model == AssessmentKind.Competence
                && c.ItemId <= PupilG78CompetencyNarrative.CompetenceItemMax
                && string.Equals(c.Category, dim, StringComparison.OrdinalIgnoreCase)
                && c.EffectiveScore >= 4
                && !c.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
                && !usedIds.Contains(c.ItemId)
                && (!suppressHelpInStory || !PupilG78AnswerConflicts.ContainsHelpHigh(c.Text))
                && !tileLowItemIds.Any(id => PupilG78AnswerConflicts.TileLowConflictsWithText(id, c.Text)));
            if (pick is null)
            {
                continue;
            }

            usedIds.Add(pick.ItemId);
            usedCategories.Add(pick.Category);
            avoidSimilar.Add(pick.Text);
            var main = PupilG78StoryFlow.MainClause(pick);
            if (main.Length > 0)
            {
                examples.Add(main.TrimEnd('.'));
            }
        }

        if (examples.Count == 0)
        {
            return intro;
        }

        return $"{intro} Bijvoorbeeld: {string.Join(". ", examples).TrimEnd('.')}.";
    }

    private static string ComposeLevenOpSchoolParagraph(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language,
        List<G78AnswerClause> pool,
        HashSet<int> usedIds,
        HashSet<string> usedCategories,
        List<string> avoidSimilar,
        IReadOnlyList<int> tileLowItemIds,
        bool suppressHelpInStory)
    {
        var sentences = new List<string>();
        var vakNames = vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Take(3)
            .Select(e => PupilVakSubjectLabels.NlStoryList(e.SubjectKey))
            .ToList();
        var hobbies = HobbyLabels(progress, language)
            .Select(h => h.ToLowerInvariant())
            .ToList();
        if (vakNames.Count > 0)
        {
            sentences.Add($"Op school vind je vooral {JoinDutchList(vakNames)} leuk.");
        }

        if (hobbies.Count > 0)
        {
            sentences.Add($"In je vrije tijd ben je graag bezig met {JoinDutchList(hobbies)}.");
        }

        var culture = Pick(pool, G78StoryBeat.SchoolLater, usedCategories, usedIds, avoidSimilar, 2, tileLowItemIds: tileLowItemIds, suppressHelpInStory: suppressHelpInStory);
        RememberAvoid(avoidSimilar, culture);
        foreach (var c in culture.Take(Math.Max(0, 4 - sentences.Count)))
        {
            var sent = PupilG78StoryFlow.MainClause(c);
            if (!string.IsNullOrWhiteSpace(sent))
            {
                sentences.Add(sent);
            }
        }

        var dream = DreamPhrase(result, progress, language);
        if (!string.IsNullOrWhiteSpace(dream) && sentences.Count < 4)
        {
            sentences.Add(dream);
        }

        return sentences.Count == 0 ? "" : PupilG78StoryFlow.FlowJoin(sentences);
    }

    private static IReadOnlyList<G78AnswerClause> PickCareerSocial(
        List<G78AnswerClause> pool,
        HashSet<int> usedIds,
        HashSet<string> usedCategories,
        List<string> avoidSimilar,
        IReadOnlyList<int> tileLowItemIds,
        bool suppressHelpInStory,
        int max)
    {
        var picked = new List<G78AnswerClause>();
        foreach (var c in pool
                     .Where(c => c.Model == AssessmentKind.Career
                                 && c.Beat == G78StoryBeat.Samen
                                 && c.EffectiveScore >= 4
                                 && !usedIds.Contains(c.ItemId))
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

            if (tileLowItemIds.Any(id => PupilG78AnswerConflicts.TileLowConflictsWithText(id, c.Text)))
            {
                continue;
            }

            if (suppressHelpInStory && PupilG78AnswerConflicts.ContainsHelpHigh(c.Text))
            {
                continue;
            }

            picked.Add(c);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            if (picked.Count >= max)
            {
                break;
            }
        }

        return picked;
    }

    private static IReadOnlyList<G78AnswerClause> PickCareerHighs(
        List<G78AnswerClause> pool,
        HashSet<int> usedIds,
        HashSet<string> usedCategories,
        List<string> avoidSimilar,
        IReadOnlyList<int> tileLowItemIds,
        bool suppressHelpInStory,
        int max)
    {
        var picked = new List<G78AnswerClause>();
        foreach (var c in pool
                     .Where(c => c.Model == AssessmentKind.Career
                                 && c.Beat == G78StoryBeat.Doen
                                 && c.EffectiveScore >= 4
                                 && !usedIds.Contains(c.ItemId))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (picked.Count > 0 && PupilG78JeNarrative.TooSimilar(picked[0].Text, c.Text))
            {
                continue;
            }

            if (avoidSimilar.Any(t => PupilG78JeNarrative.TooSimilar(t, c.Text)))
            {
                continue;
            }

            if (suppressHelpInStory && PupilG78AnswerConflicts.ContainsHelpHigh(c.Text))
            {
                continue;
            }

            if (tileLowItemIds.Any(id => PupilG78AnswerConflicts.TileLowConflictsWithText(id, c.Text)))
            {
                continue;
            }

            picked.Add(c);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            if (picked.Count >= max)
            {
                break;
            }
        }

        return picked;
    }

    private static void PadParagraphsFromPool(
        List<string> paragraphs,
        List<G78AnswerClause> pool,
        HashSet<int> usedIds,
        HashSet<string> usedCategories,
        List<string> avoidSimilar,
        IReadOnlyList<int> tileLowItemIds,
        bool suppressHelpInStory)
    {
        while (paragraphs.Count < 3)
        {
            var extra = Pick(pool, G78StoryBeat.Doen, usedCategories, usedIds, avoidSimilar, 2, tileLowItemIds: tileLowItemIds, suppressHelpInStory: suppressHelpInStory);
            if (extra.Count == 0)
            {
                break;
            }

            paragraphs.Add(BuildFactParagraph(extra));
            RememberAvoid(avoidSimilar, extra);
        }

        for (var i = 0; i < paragraphs.Count && paragraphs.Count <= 4; i++)
        {
            if (SentenceCount(paragraphs[i]) >= 3)
            {
                continue;
            }

            var need = 3 - SentenceCount(paragraphs[i]);
            var fill = Pick(pool, G78StoryBeat.Doen, usedCategories, usedIds, avoidSimilar, need, tileLowItemIds: tileLowItemIds, suppressHelpInStory: suppressHelpInStory);
            if (fill.Count == 0)
            {
                continue;
            }

            var extraSentences = fill
                .Select(PupilG78StoryFlow.MainClause)
                .Where(s => s.Length > 0)
                .ToList();
            if (extraSentences.Count == 0)
            {
                continue;
            }

            var merged = PupilG78StoryFlow.FlowJoin([paragraphs[i].TrimEnd('.'), ..extraSentences]);
            paragraphs[i] = merged;
            RememberAvoid(avoidSimilar, fill);
        }
    }

    private static string EnsureRichParagraph(string paragraph)
    {
        return SentenceCount(paragraph) >= 2 ? paragraph : paragraph;
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

        var doen = Pick(clauses, G78StoryBeat.Doen, usedCategories, usedIds, avoidSimilar, 2, similarFilter: true);
        RememberAvoid(avoidSimilar, doen);
        var doenLow = PickLow(clauses, G78StoryBeat.Doen, usedIds, 1, skipSimilarTo: doen);
        if (doen.Count > 0)
        {
            paragraphs.Add(ComposeDoen(doen, doenLow));
        }

        var samen = Pick(clauses, G78StoryBeat.Samen, usedCategories, usedIds, avoidSimilar, 2, similarFilter: true);
        RememberAvoid(avoidSimilar, samen);
        if (samen.Count > 0)
        {
            paragraphs.Add(ComposeSamen(samen));
        }

        var waarden = Pick(clauses, G78StoryBeat.Waarden, usedCategories, usedIds, avoidSimilar, 2);
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
        IReadOnlyList<G78AnswerClause> all,
        G78StoryBeat beat,
        HashSet<string> usedCategories,
        HashSet<int> usedIds,
        IReadOnlyList<string> avoidSimilarTexts,
        int max,
        bool similarFilter = false,
        IReadOnlyList<int>? tileLowItemIds = null,
        bool suppressHelpInStory = false)
    {
        var picked = new List<G78AnswerClause>();
        foreach (var c in all
                     .Where(c => c.Beat == beat
                                 && c.EffectiveScore >= 4
                                 && !c.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
                                 && !usedCategories.Contains(c.Category)
                                 && !usedIds.Contains(c.ItemId))
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

            if (tileLowItemIds is not null
                && tileLowItemIds.Any(lowId => PupilG78AnswerConflicts.TileLowConflictsWithText(lowId, c.Text)))
            {
                continue;
            }

            if (suppressHelpInStory && PupilG78AnswerConflicts.ContainsHelpHigh(c.Text))
            {
                continue;
            }

            picked.Add(c);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            if (picked.Count >= max)
            {
                break;
            }
        }

        return picked;
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
