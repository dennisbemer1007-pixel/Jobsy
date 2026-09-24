using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Paid deep analyses. Competence: 150 Big Five items (30 per trait).
/// Career: 200 RIASEC items (~33–34 per Holland type). No repeated stems.
/// </summary>
public static class DeepAnalysisCatalog
{
    public const int CompetenceQuestionCount = 150;
    public const int CareerQuestionCount = 200;

    /// <summary>Backward-compatible alias for competence question count.</summary>
    public const int QuestionCount = CompetenceQuestionCount;

    public const int CompetenceItemsPerDomain = 30;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    public static readonly string[] BigFiveDomains = DeepAnalysisCompetenceItems.Domains;

    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCompetence = new(BuildCompetenceQuestions);
    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCareer = new(BuildCareerQuestions);

    /// <summary>Competence deep analysis (backward-compatible default).</summary>
    public static IReadOnlyList<DeepAnalysisQuestion> Questions => LazyCompetence.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> CareerQuestions => LazyCareer.Value;

    public static int QuestionCountFor(AssessmentKind kind)
        => kind switch
        {
            AssessmentKind.Career => CareerQuestionCount,
            AssessmentKind.Culture => 0,
            _ => CompetenceQuestionCount
        };

    public static IReadOnlyList<DeepAnalysisQuestion> QuestionsFor(AssessmentKind kind)
        => kind switch
        {
            AssessmentKind.Career => CareerQuestions,
            AssessmentKind.Culture => throw new InvalidOperationException(
                "De cultuurscan heeft geen deep analysis. Gebruik de gratis Quick-Scan (18 vragen)."),
            _ => Questions
        };

    public static bool SupportsDeepAnalysis(AssessmentKind kind)
        => kind is AssessmentKind.Competence or AssessmentKind.Career;

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCompetenceQuestions()
        => Materialize(
            "BigFive",
            DeepAnalysisCompetenceItems.All,
            AssessmentKind.Competence,
            CompetenceQuestionCount,
            expectedPerDomain: CompetenceItemsPerDomain);

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCareerQuestions()
        => Materialize(
            "RIASEC",
            DeepAnalysisCareerItems.All,
            AssessmentKind.Career,
            CareerQuestionCount,
            expectedPerDomain: null);

    private static IReadOnlyList<DeepAnalysisQuestion> Materialize(
        string family,
        IReadOnlyList<(string Domain, bool Reverse, string Prompt)> items,
        AssessmentKind kind,
        int expectedTotal,
        int? expectedPerDomain)
    {
        if (items.Count != expectedTotal)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) source must contain {expectedTotal} items, got {items.Count}.");
        }

        var list = new List<DeepAnalysisQuestion>(expectedTotal);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            list.Add(new DeepAnalysisQuestion(i + 1, family, item.Domain, item.Reverse, item.Prompt.Trim()));
        }

        EnsureQuality(list, kind, expectedTotal, expectedPerDomain);
        return list;
    }

    private static void EnsureQuality(
        List<DeepAnalysisQuestion> list,
        AssessmentKind kind,
        int expectedTotal,
        int? expectedPerDomain)
    {
        if (list.Count != expectedTotal)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) must contain {expectedTotal} questions, got {list.Count}.");
        }

        var prompts = list.Select(q => q.PromptNl).ToList();
        if (prompts.Any(p => p.Contains("variant", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"DeepAnalysisCatalog ({kind}) still contains repeated 'variant' stems.");
        }

        var distinct = prompts.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinct != expectedTotal)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) prompts must be unique, got {distinct} distinct of {expectedTotal}.");
        }

        foreach (var group in list.GroupBy(q => q.Domain, StringComparer.OrdinalIgnoreCase))
        {
            if (expectedPerDomain is int expected)
            {
                if (group.Count() != expected)
                {
                    throw new InvalidOperationException(
                        $"DeepAnalysisCatalog ({kind}) domain {group.Key} has {group.Count()} items, expected {expected}.");
                }
            }
            else if (group.Count() is < 32 or > 35)
            {
                throw new InvalidOperationException(
                    $"DeepAnalysisCatalog ({kind}) domain {group.Key} has {group.Count()} items, expected 32–35.");
            }

            var minReverse = Math.Max(2, group.Count() / 5);
            if (group.Count(q => q.Reverse) < minReverse)
            {
                throw new InvalidOperationException(
                    $"DeepAnalysisCatalog ({kind}) domain {group.Key} needs more reverse-keyed items.");
            }
        }
    }

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(
        IReadOnlyDictionary<int, int> answers,
        bool requireComplete,
        AssessmentKind kind = AssessmentKind.Competence)
    {
        var max = QuestionCountFor(kind);
        foreach (var (qid, value) in answers)
        {
            if (qid < 1 || qid > max)
            {
                return "Onbekend vraagnummer in de diepte-analyse.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 en 5 liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers, kind))
        {
            return $"Beantwoord alle {max} vragen om de diepte-analyse af te ronden.";
        }

        return null;
    }

    public static bool IsComplete(
        IReadOnlyDictionary<int, int> answers,
        AssessmentKind kind = AssessmentKind.Competence)
    {
        var max = QuestionCountFor(kind);
        if (answers.Count < max)
        {
            return false;
        }

        for (var i = 1; i <= max; i++)
        {
            if (!answers.TryGetValue(i, out var value) || !IsValidAnswer(value))
            {
                return false;
            }
        }

        return true;
    }

    public static Dictionary<int, int> ParseAnswersJson(
        string? json,
        AssessmentKind kind = AssessmentKind.Competence)
        => LikertAnswerJson.Parse(json, QuestionCountFor(kind));

    public static string SerializeAnswers(
        IReadOnlyDictionary<int, int> answers,
        AssessmentKind kind = AssessmentKind.Competence)
        => LikertAnswerJson.Serialize(answers, QuestionCountFor(kind));

    public static IReadOnlyList<string> DeriveEnrichedTags(
        IReadOnlyDictionary<int, int> answers,
        AssessmentKind kind = AssessmentKind.Competence)
    {
        var domainAverages = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in QuestionsFor(kind))
        {
            if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
            {
                continue;
            }

            var value = question.Reverse ? LikertMax + LikertMin - raw : raw;
            if (!domainAverages.TryGetValue(question.Domain, out var bucket))
            {
                bucket = [];
                domainAverages[question.Domain] = bucket;
            }

            bucket.Add(value);
        }

        var tags = new List<string>();
        foreach (var (domain, values) in domainAverages)
        {
            if (values.Count == 0)
            {
                continue;
            }

            if (values.Average() >= 4.0)
            {
                tags.Add(domain);
            }
        }

        return tags.OrderBy(t => t, StringComparer.Ordinal).ToList();
    }

    /// <summary>0–100 per domain from reverse-corrected Likert answers.</summary>
    public static IReadOnlyList<DeepAnalysisDomainScore> ScoreDomains(
        IReadOnlyDictionary<int, int> answers,
        AssessmentKind kind)
    {
        var buckets = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in QuestionsFor(kind))
        {
            if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
            {
                continue;
            }

            var value = question.Reverse ? LikertMax + LikertMin - raw : raw;
            if (!buckets.TryGetValue(question.Domain, out var list))
            {
                list = [];
                buckets[question.Domain] = list;
            }

            list.Add(value);
        }

        return buckets
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                var avg = kv.Value.Average();
                var percent = (int)Math.Clamp(
                    Math.Round(100 * (avg - LikertMin) / (LikertMax - LikertMin), MidpointRounding.AwayFromZero),
                    0,
                    100);
                return new DeepAnalysisDomainScore(kv.Key, percent, kv.Value.Count);
            })
            .ToList();
    }

    public static RiasecScores ToRiasecScores(IReadOnlyList<DeepAnalysisDomainScore> scores)
    {
        int Get(string code) =>
            scores.FirstOrDefault(s => s.Domain.Equals(code, StringComparison.OrdinalIgnoreCase))?.Percent ?? 0;

        return new RiasecScores(
            Get(CareerTestCatalog.Realistic),
            Get(CareerTestCatalog.Investigative),
            Get(CareerTestCatalog.Artistic),
            Get(CareerTestCatalog.Social),
            Get(CareerTestCatalog.Enterprising),
            Get(CareerTestCatalog.Conventional));
    }

    public static IReadOnlyList<string> CareerAdviceParagraphs(IReadOnlyList<DeepAnalysisDomainScore> scores)
    {
        var top = scores
            .OrderByDescending(s => s.Percent)
            .ThenBy(s => s.Domain, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        if (top.Count == 0)
        {
            return
            [
            "Rond de 200 vragen af. Dan maken we een helder beeld van welk werk bij je past, plus advies voor Den Haag en het Westland."
            ];
        }

        var lines = new List<string>
        {
            $"Je uitgebreide test wijst het sterkst naar {JoinNl(top.Select(s => $"{CareerCompassBuilder.TypeLabel(s.Domain)} ({s.Percent}%)").ToList())}."
        };
        foreach (var score in top)
        {
            lines.Add(AdviceFor(score.Domain));
        }

        lines.Add(
            "Gebruik dit advies samen met je harde criteria (reistijd, vervoer, beschikbaarheid) op de banenkaart. Werkgevers zien alleen anonieme tags tot jij contact deelt.");
        return lines;
    }

    private static string AdviceFor(string domain) => domain switch
    {
        CareerTestCatalog.Realistic =>
            "Aanpakken met je handen past bij je — kas, logistiek, keuken, bouw, onderhoud. Zoek vacatures met tastbaar resultaat en duidelijke veiligheid.",
        CareerTestCatalog.Investigative =>
            "Uitzoeken hoe het zit geeft je energie. Kijk naar kwaliteitscontrole, teelttechniek, metingen, data in de keten of verbetertrajecten op de vestiging.",
        CareerTestCatalog.Artistic =>
            "Iets moois of nieuws maken telt. Denk aan winkelpresentatie, content, bloemen/groen of seizoensconcepten.",
        CareerTestCatalog.Social =>
            "Mensen helpen geeft richting. Zorg, horeca, retail, begeleiding en inwerken van seizoenscollega’s sluiten aan.",
        CareerTestCatalog.Enterprising =>
            "Aanjagen en verkopen ligt je. Filiaalverkoop, ploegaansturing, horeca-shiftleiding of acquisitie in de regio past beter dan puur uitvoerend werk.",
        CareerTestCatalog.Conventional =>
            "Netjes organiseren is je kracht. Planning, kassa, orderpicking-systemen, administratie en kwaliteitsregistratie matchen sterk.",
        DeepAnalysisCompetenceItems.Openheid =>
            "Openheid: je leert en verbetert graag. Vacatures met wisselende taken en inwerken op nieuwe systemen benutten dat.",
        DeepAnalysisCompetenceItems.Consciëntieusheid =>
            "Consciëntieusheid: betrouwbaarheid en afronden zijn jouw voorsprong bij werkgeversfilters.",
        DeepAnalysisCompetenceItems.Extraversie =>
            "Extraversie: klant- en teamcontact past; kijk naar balie, vloer en ploegen met veel overleg.",
        DeepAnalysisCompetenceItems.Vriendelijkheid =>
            "Vriendelijkheid: samenwerking en gastvrijheid zijn een sterke match-tag in de talentpool.",
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit =>
            "Emotionele stabiliteit: piekdruk (seizoen, horeca, logistiek) is haalbaarder als de rest van het profiel klopt.",
        _ => $"Op {Label(domain)} scoor je hoog; weeg dat mee bij branchevorkeur en matching."
    };

    private static string Label(string domain) => domain switch
    {
        CareerTestCatalog.Realistic => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Realistic),
        CareerTestCatalog.Investigative => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Investigative),
        CareerTestCatalog.Artistic => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Artistic),
        CareerTestCatalog.Social => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Social),
        CareerTestCatalog.Enterprising => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Enterprising),
        CareerTestCatalog.Conventional => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Conventional),
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit => "emotionele stabiliteit",
        DeepAnalysisCompetenceItems.Vriendelijkheid => "vriendelijkheid",
        _ => domain.ToLowerInvariant()
    };

    private static string JoinNl(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };
}

public sealed record DeepAnalysisQuestion(
    int Id,
    string Family,
    string Domain,
    bool Reverse,
    string PromptNl);

public sealed record DeepAnalysisDomainScore(string Domain, int Percent, int AnsweredCount);
