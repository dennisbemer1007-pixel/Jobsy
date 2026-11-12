using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Turns scored answer clauses into flowing B1 paragraphs (deterministic).</summary>
public static class PupilG78StoryComposer
{
    /// <summary>Short connecting overview; detail lives in the four tiles.</summary>
    public static IReadOnlyList<string> ComposeOverview(
        IReadOnlyList<G78AnswerClause> clauses,
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language,
        IReadOnlySet<int> reservedItemIds,
        out IReadOnlyList<int> usedItemIds)
    {
        _ = clauses;
        _ = reservedItemIds;
        usedItemIds = [];
        var paragraphs = new List<string>();

        var schoolLater = ComposeSchoolLater(result, progress, vak, language);
        if (!string.IsNullOrWhiteSpace(schoolLater))
        {
            paragraphs.Add(schoolLater);
        }

        var closing = ClosingSentence(result, language);
        if (!string.IsNullOrWhiteSpace(closing) && paragraphs.Count > 0)
        {
            paragraphs[^1] = paragraphs[^1].TrimEnd() + " " + closing;
        }
        else if (!string.IsNullOrWhiteSpace(closing))
        {
            paragraphs.Add(closing);
        }

        if (paragraphs.Count == 0)
        {
            paragraphs.Add(PupilVerhaalLocalized.IsEnglish(language)
                ? "You answered the questions yourself. There is no right or wrong."
                : "Je gaf zelf antwoord op de vragen. Er is geen goed of fout.");
        }

        while (paragraphs.Count > 3)
        {
            paragraphs.RemoveAt(0);
        }

        return paragraphs.Select(PupilQuestionStoryParaphrase.PolishJeForm).ToList();
    }

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

        var closing = ClosingSentence(result, language);
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
        bool similarFilter = false)
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
        return $"Later wil je misschien eens kijken of iets als {title} bij je past.";
    }

    private static string ClosingSentence(PupilResult result, string language)
    {
        if (PupilVerhaalLocalized.IsEnglish(language))
        {
            return "";
        }

        var holland = (result.HollandCode ?? "").Trim().ToUpperInvariant();
        var riasecBits = new List<string>();
        foreach (var ch in holland)
        {
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
