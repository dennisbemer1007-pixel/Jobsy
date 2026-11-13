using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Traceable story lines from scores (no extra question items consumed).</summary>
public static class PupilG78StorySummaries
{
    public static string CompetencyStrengthLine(
        string competencyCategory,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string>? growthCategories = null,
        bool contrastV2 = false)
    {
        if (contrastV2)
        {
            return "";
        }

        if (growthCategories is not null && growthCategories.Contains(competencyCategory))
        {
            return "";
        }

        var avg = PupilG78GrowthCompetencies.DimensionAverages(answers, contrastV2);
        if (!avg.TryGetValue(competencyCategory, out var mean) || mean < 3.25)
        {
            return "";
        }

        return competencyCategory switch
        {
            CompetencyTestCatalog.Samenwerken =>
                "Je bent vaak bezig met de mensen om je heen.",
            CompetencyTestCatalog.Resultaatgerichtheid =>
                "Je houdt graag overzicht en werkt met een plan.",
            CompetencyTestCatalog.Stressbestendigheid =>
                "Je blijft meestal rustig als iets even tegenzit.",
            CompetencyTestCatalog.Innovatie =>
                "Je probeert graag nieuwe manieren uit.",
            CompetencyTestCatalog.Extraversie =>
                "Je praat makkelijk met anderen in een groep.",
            _ => ""
        };
    }

    public static string SocialOverviewLine(
        PupilResult result,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string>? growthCategories = null,
        bool contrastV2 = false)
    {
        if (contrastV2)
        {
            return "";
        }

        var avg = PupilG78GrowthCompetencies.DimensionAverages(answers, contrastV2);
        if (growthCategories is null || !growthCategories.Contains(CompetencyTestCatalog.Samenwerken))
        {
            if (avg.TryGetValue(CompetencyTestCatalog.Samenwerken, out var sw) && sw >= 3.5)
            {
                return "Je bent vaak bezig met de mensen om je heen.";
            }
        }

        if (growthCategories is null || !growthCategories.Contains(CompetencyTestCatalog.Extraversie))
        {
            if (avg.TryGetValue(CompetencyTestCatalog.Extraversie, out var ex) && ex >= 3.5)
            {
                return "In een groep vind je het fijn als er contact is.";
            }
        }

        var holland = (result.HollandCode ?? "").ToUpperInvariant();
        if (holland.Contains('S'))
        {
            return "Je let in de groep vaak op hoe het met anderen gaat.";
        }

        return "";
    }

    public static string ComposeValuesCultureParagraph(
        PupilResult result,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlyList<string>? avoidTileSentences = null,
        IReadOnlySet<string>? growthCategories = null,
        bool contrastV2 = false)
    {
        var sentences = new List<string>();
        var valueLine = TopValueSummary(result.TopValue);
        if (valueLine.Length > 0 && !ConflictsWithTiles(valueLine, avoidTileSentences))
        {
            sentences.Add(valueLine);
        }

        var cultureLine = TopCultureSummary(result.TopCulture);
        if (cultureLine.Length > 0
            && !ConflictsWithTiles(cultureLine, avoidTileSentences)
            && !CultureLineClashesChangeTile(cultureLine, avoidTileSentences))
        {
            sentences.Add(cultureLine);
        }

        foreach (var clause in ValueMainClausesFromAnswers(answers, growthCategories, maxCount: 2, contrastV2))
        {
            if (ConflictsWithTiles(clause, avoidTileSentences))
            {
                continue;
            }

            if (!sentences.Any(s => PupilG78JeNarrative.TooSimilar(s, clause)))
            {
                sentences.Add(clause);
            }
        }

        sentences = sentences
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (sentences.Count < 2 && !contrastV2)
        {
            sentences.Add("Je weet goed wat je belangrijk vindt en waar je je op je gemak voelt.");
        }

        return PupilG78StoryFlow.FlowJoin(sentences.Take(3).ToList());
    }

    public static string ComposeSchoolHobbiesRiasecParagraph(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language,
        IReadOnlySet<string>? growthCategories = null,
        bool contrastV2 = false)
    {
        _ = contrastV2;
        var sentences = new List<string>();
        var vakNames = BuildVakStoryNames(vak);
        if (vakNames.Count > 0)
        {
            sentences.Add(MaakVakSummary(vakNames));
        }

        var hobbies = HobbyLabels(progress, language);
        if (hobbies.Count > 0)
        {
            sentences.Add($"In je vrije tijd ben je graag bezig met {JoinDutchList(hobbies.Select(h => h.ToLowerInvariant()).ToList())}.");
        }

        var riasec = RiasecTopTwoSentence(result, growthCategories);
        if (riasec.Length > 0)
        {
            sentences.Add(riasec);
        }

        if (sentences.Count == 0)
        {
            return "";
        }

        return PupilG78StoryFlow.FlowJoin(sentences);
    }

    public static string RiasecTopTwoSentence(
        PupilResult result,
        IReadOnlySet<string>? growthCategories = null)
    {
        var holland = (result.HollandCode ?? "").Trim().ToUpperInvariant();
        var bits = new List<string>();
        foreach (var ch in holland.Take(3))
        {
            if (growthCategories is not null)
            {
                if (ch == 'C' && growthCategories.Contains(CompetencyTestCatalog.Resultaatgerichtheid))
                {
                    continue;
                }

                if (ch == 'S' && growthCategories.Contains(CompetencyTestCatalog.Samenwerken))
                {
                    continue;
                }

                if (ch == 'A' && growthCategories.Contains(CompetencyTestCatalog.Innovatie))
                {
                    continue;
                }

                if (ch == 'E' && growthCategories.Contains(CompetencyTestCatalog.Extraversie))
                {
                    continue;
                }
            }

            var bit = ch switch
            {
                'R' => "praktisch werken",
                'I' => "uitzoeken",
                'A' => "creatief bezig zijn",
                'S' => "anderen helpen",
                'E' => "het voortouw nemen",
                'C' => "overzicht en duidelijke stappen",
                _ => null
            };
            if (bit is not null && !bits.Contains(bit))
            {
                bits.Add(bit);
            }

            if (bits.Count >= 2)
            {
                break;
            }
        }

        return bits.Count switch
        {
            0 => "",
            1 => $"In de beroepentest kwam vooral {bits[0]} naar voren.",
            _ => $"In de beroepentest kwam vooral {bits[0]} en {bits[1]} naar voren."
        };
    }

    private static IReadOnlyList<string> BuildVakStoryNames(IReadOnlyList<PupilVakScoreEntry> vak)
    {
        var ranked = vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Select(e => e.SubjectKey)
            .ToList();

        var picked = new List<string>();
        foreach (var key in ranked)
        {
            if (string.Equals(key, "techniek", StringComparison.OrdinalIgnoreCase)
                && ranked.Any(k => string.Equals(k, "natuur-techniek", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (string.Equals(key, "handvaardigheid", StringComparison.OrdinalIgnoreCase)
                && ranked.Any(k => string.Equals(k, "tekenen", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (picked.Count >= 3)
            {
                break;
            }

            picked.Add(key);
        }

        var hasHandvaardigheid = ranked.Any(k =>
            string.Equals(k, "handvaardigheid", StringComparison.OrdinalIgnoreCase));

        var names = new List<string>();
        foreach (var key in picked)
        {
            if (string.Equals(key, "handvaardigheid", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (hasHandvaardigheid
                && string.Equals(key, "tekenen", StringComparison.OrdinalIgnoreCase))
            {
                names.Add(PupilVakSubjectLabels.NlStoryList("tekenen"));
                names.Add(PupilVakSubjectLabels.NlStoryList("handvaardigheid"));
                continue;
            }

            names.Add(FormatVakStoryName(key));
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n.StartsWith("het vak", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ToList();
    }

    private static string FormatVakStoryName(string subjectKey)
    {
        if (string.Equals(subjectKey, "natuur-techniek", StringComparison.OrdinalIgnoreCase))
        {
            return "het vak natuur en techniek";
        }

        return PupilVakSubjectLabels.NlStoryList(subjectKey);
    }

    private static string MaakVakSummary(IReadOnlyList<string> vakNames)
        => $"Op school heb je het meest plezier in {JoinDutchList(vakNames)}.";

    public static string TopValueSummary(string? topValue) => topValue?.Trim() switch
    {
        "Connection" => "Verbinding met anderen vind je belangrijk.",
        "Autonomy" => "Je vindt het belangrijk om zelf te kunnen kiezen.",
        "Achievement" => "Je wilt graag dingen helemaal afmaken.",
        "Stability" => "Rust en duidelijkheid vind je belangrijk.",
        "Impact" => "Je vindt het belangrijk om verschil te maken voor anderen.",
        _ => ""
    };

    public static string TopCultureSummary(string? topCulture) => topCulture?.Trim() switch
    {
        "Collaboration" => "Je voelt je thuis als iedereen samenwerkt en elkaar steunt.",
        "Informal" => "Je voelt je op je plek als de sfeer open en gezellig is.",
        "Autonomy" => "Je voelt je thuis als je zelf ruimte krijgt om te werken.",
        "Flexibility" => "Je voelt je je op je gemak als er ruimte is om dingen anders te doen.",
        "Innovation" => "Je voelt je thuis waar nieuwe ideeën welkom zijn.",
        "PeopleFirst" => "Je voelt je op je plek als mensen centraal staan.",
        _ => ""
    };

    private static IReadOnlyList<string> ValueMainClausesFromAnswers(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string>? growthCategories,
        int maxCount = 2,
        bool contrastV2 = false)
    {
        growthCategories ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bank = contrastV2 ? (IPupilQuestionBank)new PupilQuestionBankG78V2() : new PupilQuestionBank();
        var ranked = new List<(string Sentence, int Id, int Eff)>();
        foreach (var q in bank.Questions.Where(q => q.Model == AssessmentKind.Values))
        {
            if (!answers.TryGetValue(q.Id.ToString(), out var raw))
            {
                continue;
            }

            string? sentence = null;
            int eff;
            if (contrastV2 && q.IsContrast)
            {
                var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw);
                if (fact is null)
                {
                    continue;
                }

                eff = fact.Value.EffectiveScore;
                sentence = PupilG78StoryFlow.MainClause(
                    new G78AnswerClause(q.Id, fact.Value.Category, q.Model, G78StoryBeat.Waarden, fact.Value.Text, eff),
                    contrastV2: true);
            }
            else
            {
                eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
                if (eff < 4)
                {
                    continue;
                }

                var je = PupilG78QuestionJeForms.ForItem(q.Id);
                if (string.IsNullOrWhiteSpace(je))
                {
                    continue;
                }

                sentence = je.TrimEnd();
                if (!sentence.EndsWith('.'))
                {
                    sentence += ".";
                }
            }

            if (eff < 4
                || string.IsNullOrWhiteSpace(sentence)
                || PupilG78StrengthGuard.IsForbiddenStrength(q.Id, eff, growthCategories))
            {
                continue;
            }

            ranked.Add((sentence, q.Id, eff));
        }

        return ranked
            .OrderByDescending(c => c.Eff)
            .ThenBy(c => c.Id)
            .Select(c => c.Sentence)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(maxCount)
            .ToList();
    }

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

    private static bool CultureLineClashesChangeTile(string cultureLine, IReadOnlyList<string>? tileSentences)
    {
        if (!cultureLine.Contains("dingen anders", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return tileSentences?.Any(t =>
                   t.Contains("saai als alles altijd hetzelfde", StringComparison.OrdinalIgnoreCase))
               ?? false;
    }

    private static bool ConflictsWithTiles(string line, IReadOnlyList<string>? tileSentences)
    {
        if (tileSentences is null || tileSentences.Count == 0)
        {
            return false;
        }

        return tileSentences.Any(t => PupilG78JeNarrative.TooSimilar(t, line));
    }

    private static string JoinDutchList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };
}
