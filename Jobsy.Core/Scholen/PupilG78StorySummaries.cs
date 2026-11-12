using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Traceable story lines from scores (no extra question items consumed).</summary>
public static class PupilG78StorySummaries
{
    public static string CompetencyStrengthLine(
        string competencyCategory,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string>? growthCategories = null)
    {
        if (growthCategories is not null && growthCategories.Contains(competencyCategory))
        {
            return "";
        }

        var avg = PupilG78GrowthCompetencies.DimensionAverages(answers);
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
        IReadOnlySet<string>? growthCategories = null)
    {
        var avg = PupilG78GrowthCompetencies.DimensionAverages(answers);
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
        IReadOnlyDictionary<string, int> answers)
    {
        var sentences = new List<string>();
        var valueLine = TopValueSummary(result.TopValue);
        if (valueLine.Length > 0)
        {
            sentences.Add(valueLine);
        }

        var cultureLine = TopCultureSummary(result.TopCulture);
        if (cultureLine.Length > 0)
        {
            sentences.Add(cultureLine);
        }

        if (sentences.Count < 2)
        {
            var extra = ValuesFromAnswersSummary(answers);
            if (extra.Length > 0 && !sentences.Contains(extra))
            {
                sentences.Add(extra);
            }
        }

        sentences = sentences
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (sentences.Count < 2)
        {
            var connection = ValuesFromAnswersSummary(answers);
            if (connection.Length > 0 && !sentences.Contains(connection))
            {
                sentences.Add(connection);
            }
        }

        if (sentences.Count < 2)
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
        IReadOnlySet<string>? growthCategories = null)
    {
        var sentences = new List<string>();
        var vakNames = vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Take(3)
            .Select(e => PupilVakSubjectLabels.NlStoryList(e.SubjectKey))
            .ToList();
        if (vakNames.Count > 0)
        {
            sentences.Add(MaakVakSummary(vak, vakNames));
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

    private static string MaakVakSummary(IReadOnlyList<PupilVakScoreEntry> vak, IReadOnlyList<string> vakNames)
    {
        var maakKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tekenen", "handvaardigheid", "muziek", "natuur-techniek", "techniek"
        };
        if (vak.Any(e => e.Leuk >= 4 && maakKeys.Contains(e.SubjectKey)))
        {
            return $"Op school heb je het meest plezier in {JoinDutchList(vakNames)}, vaak omdat je er iets maakt of uitprobeert.";
        }

        return $"Op school heb je het meest plezier in {JoinDutchList(vakNames)}.";
    }

    public static string TopValueSummary(string? topValue) => topValue?.Trim() switch
    {
        "Connection" => "Verbinding met anderen vind je belangrijk.",
        "Autonomy" => "Je vindt het belangrijk om zelf te kunnen kiezen.",
        "Achievement" => "Je wilt graag iets goed afmaken en trots zijn op wat je bereikt.",
        "Stability" => "Rust en duidelijkheid vind je belangrijk.",
        "Impact" => "Je vindt het belangrijk om verschil te maken voor anderen.",
        _ => ""
    };

    public static string TopCultureSummary(string? topCulture) => topCulture?.Trim() switch
    {
        "Collaboration" => "Je voelt je thuis als iedereen samenwerkt en elkaar steunt.",
        "Informal" => "Je voelt je op je plek als de sfeer open en gezellig is.",
        "Autonomy" => "Je voelt je thuis als je zelf ruimte krijgt om te werken.",
        "Flexibility" => "Je voelt je op je gemak als er ruimte is om dingen anders te doen.",
        "Innovation" => "Je voelt je thuis waar nieuwe ideeën welkom zijn.",
        "PeopleFirst" => "Je voelt je op je plek als mensen centraal staan.",
        _ => ""
    };

    private static string ValuesFromAnswersSummary(IReadOnlyDictionary<string, int> answers)
    {
        var highs = new List<int>();
        for (var id = 9031; id <= 9045; id++)
        {
            if (answers.TryGetValue(id.ToString(), out var raw) && raw >= 4)
            {
                highs.Add(id);
            }
        }

        highs = highs.OrderByDescending(id => answers[id.ToString()]).Take(2).ToList();
        if (highs.Count == 0)
        {
            return "";
        }

        var labels = highs
            .Select(PupilG78ValueShortLabels.ForItem)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.ToLowerInvariant())
            .ToList();
        return labels.Count switch
        {
            0 => "",
            1 => $"In je antwoorden kwam naar voren dat je {labels[0]} belangrijk vindt.",
            _ => $"Je antwoorden laten zien dat je {labels[0]} en {labels[1]} belangrijk vindt."
        };
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

    private static string JoinDutchList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };
}
