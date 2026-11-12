using System.Text.RegularExpressions;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Turns scored answer clauses into flowing B1 paragraphs (deterministic).</summary>
public static class PupilG78StoryComposer
{
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
        var samenLow = PickLow(clauses, G78StoryBeat.Samen, usedIds, 1, skipSimilarTo: samen);
        if (samen.Count + samenLow.Count > 0)
        {
            paragraphs.Add(ComposeSamen(samen, samenLow));
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
            parts.Add(MergeJeSentences(highs[0].Text, highs[1].Text));
        }
        else if (highs.Count == 1)
        {
            parts.Add(LeadIn(highs[0].Text, "Het liefst"));
        }

        if (lows.Count > 0)
        {
            parts.Add(ContrastLine(lows[0].Text));
        }
        else if (highs.Count >= 1 && parts.Count == 1)
        {
            parts.Add("Daardoor weet je steeds beter wat bij je past.");
        }

        return string.Join(' ', parts);
    }

    private static string ComposeSamen(IReadOnlyList<G78AnswerClause> highs, IReadOnlyList<G78AnswerClause> lows)
    {
        var parts = new List<string>();
        if (highs.Count >= 2)
        {
            parts.Add(MergeJeSentences(highs[0].Text, highs[1].Text));
        }
        else if (highs.Count == 1)
        {
            parts.Add(LeadIn(highs[0].Text, "Met anderen om je heen"));
        }

        if (lows.Count > 0)
        {
            parts.Add(ContrastLine(lows[0].Text));
        }

        return string.Join(' ', parts);
    }

    private static string ComposeWaarden(IReadOnlyList<G78AnswerClause> highs)
    {
        if (highs.Count >= 2)
        {
            var first = Sentence(highs[0].Text).TrimEnd('.');
            var second = Sentence(highs[1].Text).TrimEnd('.');
            return $"{first}, en {JeContinuation(second)}.";
        }

        return LeadIn(highs[0].Text, "Wat voor jou telt");
    }

    private static string MergeJeSentences(string a, string b)
    {
        var first = Sentence(a).TrimEnd('.');
        var tail = JeContinuation(b);
        return $"{first}, en {tail}.";
    }

    private static string Sentence(string text) => PupilQuestionStoryParaphrase.PolishJeForm(text.Trim().TrimEnd('.') + ".");

    private static string JeContinuation(string text)
    {
        var s = text.Trim().TrimEnd('.');
        if (s.StartsWith("Je ", StringComparison.Ordinal))
        {
            return "je " + s[3..];
        }

        return s;
    }

    private static string LeadIn(string jeSentence, string prefix)
    {
        if (!jeSentence.Trim().StartsWith("Je ", StringComparison.Ordinal))
        {
            return Sentence(jeSentence);
        }

        var rest = jeSentence.Trim().TrimEnd('.')[3..];
        var m = Regex.Match(rest, @"^(\S+)\s+(.*)$");
        if (!m.Success)
        {
            return $"{prefix} {rest.ToLowerInvariant()}.";
        }

        var verb = m.Groups[1].Value;
        var remainder = m.Groups[2].Value;
        var stem = InvertFiniteVerb(verb);
        return $"{prefix} {stem} je {remainder}.";
    }

    private static string ContrastLine(string lowSentence)
    {
        var s = lowSentence.Trim().TrimEnd('.');
        if (s.StartsWith("Dit zei je minder vaak over jezelf: ", StringComparison.Ordinal))
        {
            var inner = s["Dit zei je minder vaak over jezelf: ".Length..].Trim().TrimEnd('.');
            if (!inner.StartsWith("je ", StringComparison.Ordinal) && !inner.StartsWith("Je ", StringComparison.Ordinal))
            {
                inner = "je " + inner;
            }

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

        return $"Tegelijk past dat minder bij jou: {s.ToLowerInvariant()}.";
    }

    private static string ContrastFromJeInner(string inner)
    {
        inner = inner.Trim().TrimEnd('.');
        if (inner.StartsWith("Je ", StringComparison.Ordinal))
        {
            inner = "je " + inner[3..];
        }

        if (inner.StartsWith("je ", StringComparison.Ordinal))
        {
            var rest = inner[3..];
            var m = Regex.Match(rest, @"^(\S+)\s+(.*)$");
            if (m.Success)
            {
                var verb = m.Groups[1].Value;
                var remainder = m.Groups[2].Value;
                var stem = InvertFiniteVerb(verb);
                if (!string.Equals(stem, verb, StringComparison.OrdinalIgnoreCase))
                {
                    return $"Tegelijk {stem} je {remainder}.";
                }
            }

            return $"Tegelijk {inner}.";
        }

        return $"Tegelijk past {inner} minder bij jou.";
    }

    private static void RememberAvoid(List<string> avoid, IReadOnlyList<G78AnswerClause> picked)
    {
        foreach (var c in picked)
        {
            avoid.Add(c.Text);
        }
    }

    private static string InvertFiniteVerb(string verb) => verb.ToLowerInvariant() switch
    {
        "helpt" => "help",
        "vindt" => "vind",
        "neemt" => "neem",
        "wilt" => "wil",
        "houdt" => "houd",
        "maakt" => "maak",
        "werkt" => "werk",
        "zoekt" => "zoek",
        "leert" => "leer",
        "doet" => "doe",
        "voelt" => "voel",
        "blijft" => "blijf",
        "durft" => "durv",
        "probeert" => "probeer",
        "overtuigt" => "overtuig",
        "bedenkt" => "bedenk",
        "speelt" => "speel",
        "raakt" => "raak",
        "past" => "pas",
        "deelt" => "deel",
        "laat" => "laat",
        "praat" => "praat",
        "luistert" => "luister",
        "kiest" => "kies",
        "wordt" => "word",
        _ => verb
    };

    private static bool TooSimilar(string a, string b)
    {
        static IEnumerable<string> Tokens(string t) => t
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4);

        var overlap = Tokens(a).Intersect(Tokens(b)).ToList();
        return overlap.Count >= 2
               || (a.Contains("help", StringComparison.OrdinalIgnoreCase)
                   && b.Contains("help", StringComparison.OrdinalIgnoreCase))
               || (a.Contains("niet snapt", StringComparison.OrdinalIgnoreCase)
                   && b.Contains("niet snapt", StringComparison.OrdinalIgnoreCase));
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
            if (similarFilter && picked.Count > 0 && TooSimilar(picked[0].Text, c.Text))
            {
                continue;
            }

            if (avoidSimilarTexts.Any(t => TooSimilar(t, c.Text)))
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
            if (skipSimilarTo.Count > 0 && skipSimilarTo.Any(h => TooSimilar(h.Text, c.Text)))
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
                $"Voor school en later geldt: op school vind je vooral {JoinDutchList(vakNames)} leuk, en in je vrije tijd {JoinDutchList(hobbies)}.");
        }
        else if (vakNames.Count > 0)
        {
            bits.Add($"Op school vind je vooral {JoinDutchList(vakNames)} leuk om te doen.");
        }
        else if (hobbies.Count > 0)
        {
            bits.Add($"In je vrije tijd gaat je voorkeur naar {JoinDutchList(hobbies)}.");
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
                'R' => "graag met je handen bezig bent",
                'I' => "graag wil weten hoe iets werkt",
                'A' => "graag creatief bezig bent",
                'S' => "anderen graag helpt",
                'E' => "het voortouw durft nemen",
                'C' => "van overzicht en duidelijke stappen houdt",
                _ => null
            };
            if (bit is not null && !riasecBits.Contains(bit))
            {
                riasecBits.Add(bit);
            }
        }

        riasecBits = riasecBits.Take(2).ToList();
        var value = ValueClosingNoun(result.TopValue);
        if (riasecBits.Count == 0 && value is null)
        {
            return "";
        }

        if (riasecBits.Count >= 2 && value is not null)
        {
            return $"Al met al sluit dit aan bij iemand die {riasecBits[0]} en {riasecBits[1]}, en voor wie {value} telt.";
        }

        if (riasecBits.Count == 1 && value is not null)
        {
            return $"Al met al sluit dit aan bij iemand die {riasecBits[0]}, en voor wie {value} telt.";
        }

        return riasecBits.Count switch
        {
            >= 2 => $"Al met al past dat bij iemand die {riasecBits[0]} en {riasecBits[1]}.",
            1 => $"Al met al past dat bij iemand die {riasecBits[0]}.",
            _ => value is null ? "" : $"Al met al past dat bij iemand voor wie {value} telt."
        };
    }

    private static string? ValueClosingNoun(string? topValue) => topValue?.Trim() switch
    {
        "Connection" => "verbinding met anderen",
        "Autonomy" => "zelf mogen kiezen",
        "Achievement" => "iets goed afmaken",
        "Stability" => "rust en duidelijkheid",
        "Impact" => "verschil maken voor anderen",
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
