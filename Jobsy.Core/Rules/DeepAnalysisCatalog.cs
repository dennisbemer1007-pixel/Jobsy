using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Paid 150-item deep analyses. Competence is 30 unique Likert items per Big Five trait;
/// career is 25 unique items per RIASEC type. No repeated stems / “variant N”.
/// </summary>
public static class DeepAnalysisCatalog
{
    public const int QuestionCount = 150;
    public const int CompetenceItemsPerDomain = 30;
    public const int CareerItemsPerDomain = 25;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    public static readonly string[] BigFiveDomains = DeepAnalysisCompetenceItems.Domains;

    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCompetence = new(BuildCompetenceQuestions);
    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCareer = new(BuildCareerQuestions);

    /// <summary>Competence deep analysis (backward-compatible default).</summary>
    public static IReadOnlyList<DeepAnalysisQuestion> Questions => LazyCompetence.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> CareerQuestions => LazyCareer.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> QuestionsFor(AssessmentKind kind)
        => kind == AssessmentKind.Career ? CareerQuestions : Questions;

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCompetenceQuestions()
        => Materialize("BigFive", DeepAnalysisCompetenceItems.All, AssessmentKind.Competence, CompetenceItemsPerDomain);

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCareerQuestions()
        => Materialize("RIASEC", DeepAnalysisCareerItems.All, AssessmentKind.Career, CareerItemsPerDomain);

    private static IReadOnlyList<DeepAnalysisQuestion> Materialize(
        string family,
        IReadOnlyList<(string Domain, bool Reverse, string Prompt)> items,
        AssessmentKind kind,
        int expectedPerDomain)
    {
        if (items.Count != QuestionCount)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) source must contain {QuestionCount} items, got {items.Count}.");
        }

        var list = new List<DeepAnalysisQuestion>(QuestionCount);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            list.Add(new DeepAnalysisQuestion(i + 1, family, item.Domain, item.Reverse, item.Prompt.Trim()));
        }

        EnsureQuality(list, kind, expectedPerDomain);
        return list;
    }

    private static void EnsureQuality(
        List<DeepAnalysisQuestion> list,
        AssessmentKind kind,
        int expectedPerDomain)
    {
        if (list.Count != QuestionCount)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) must contain {QuestionCount} questions, got {list.Count}.");
        }

        var prompts = list.Select(q => q.PromptNl).ToList();
        if (prompts.Any(p => p.Contains("variant", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"DeepAnalysisCatalog ({kind}) still contains repeated 'variant' stems.");
        }

        var distinct = prompts.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinct != QuestionCount)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) prompts must be unique, got {distinct} distinct of {QuestionCount}.");
        }

        foreach (var group in list.GroupBy(q => q.Domain, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() != expectedPerDomain)
            {
                throw new InvalidOperationException(
                    $"DeepAnalysisCatalog ({kind}) domain {group.Key} has {group.Count()} items, expected {expectedPerDomain}.");
            }

            if (group.Count(q => q.Reverse) < expectedPerDomain / 5)
            {
                throw new InvalidOperationException(
                    $"DeepAnalysisCatalog ({kind}) domain {group.Key} needs more reverse-keyed items.");
            }
        }
    }

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (qid, value) in answers)
        {
            if (qid is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de diepte-analyse.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 en 5 liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 150 vragen om de diepte-analyse af te ronden.";
        }

        return null;
    }

    public static bool IsComplete(IReadOnlyDictionary<int, int> answers)
    {
        if (answers.Count < QuestionCount)
        {
            return false;
        }

        for (var i = 1; i <= QuestionCount; i++)
        {
            if (!answers.TryGetValue(i, out var value) || !IsValidAnswer(value))
            {
                return false;
            }
        }

        return true;
    }

    public static Dictionary<int, int> ParseAnswersJson(string? json)
        => LikertAnswerJson.Parse(json, QuestionCount);

    public static string SerializeAnswers(IReadOnlyDictionary<int, int> answers)
        => LikertAnswerJson.Serialize(answers, QuestionCount);

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
                "Rond de 150 vragen af. Dan maken we een Holland-profiel en bijpassend carrière-advies voor Den Haag en het Westland."
            ];
        }

        var lines = new List<string>
        {
            $"Je diepte-analyse wijst het sterkst naar {JoinNl(top.Select(s => $"{Label(s.Domain)} ({s.Percent}%)").ToList())}."
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
            "Realistic: praktijkomgevingen passen bij je — kassen, logistiek, keuken, bouw, onderhoud. Zoek vacatures met tastbaar resultaat en duidelijke veiligheid.",
        CareerTestCatalog.Investigative =>
            "Investigative: je wilt weten waarom iets werkt. Kijk naar kwaliteitscontrole, teelttechniek, lab-achtige taken, data in de keten of verbetertrajecten op de vestiging.",
        CareerTestCatalog.Artistic =>
            "Artistic: presentatie en eigen inbreng tellen. Denk aan horeca-styling, winkelpresentatie, content, bloemen/groen of seizoensconcepten.",
        CareerTestCatalog.Social =>
            "Social: mensen helpen geeft richting. Zorg, horeca, retail, begeleiding en inwerken van seizoenscollega’s sluiten aan bij je RIASEC-profiel.",
        CareerTestCatalog.Enterprising =>
            "Enterprising: jij trekt, verkoopt en organiseert. Filiaalverkoop, ploegaansturing, horeca-shiftleiding of acquisitie in de regio past beter dan puur uitvoerend werk.",
        CareerTestCatalog.Conventional =>
            "Conventional: structuur is je kracht. Planning, kassa, orderpicking-systemen, administratie en kwaliteitsregistratie in Den Haag/Westland matchen sterk.",
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
        CareerTestCatalog.Realistic => "Realistic (doen / maken)",
        CareerTestCatalog.Investigative => "Investigative (onderzoeken)",
        CareerTestCatalog.Artistic => "Artistic (creëren)",
        CareerTestCatalog.Social => "Social (helpen)",
        CareerTestCatalog.Enterprising => "Enterprising (ondernemen)",
        CareerTestCatalog.Conventional => "Conventional (organiseren)",
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
