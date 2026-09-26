using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Paid deep analyses. Competence is 150 items (30 per Big Five trait);
/// career is 200 unique RIASEC items (~33–34 per Holland type);
/// culture is 150 items (6 culture dims × 15 + 5 personality facets × 12);
/// values is 150 Schwartz workplace drivers (30 per driver). No repeated stems.
/// </summary>
public static class DeepAnalysisCatalog
{
    /// <summary>Competence deep-analysis length (backward-compatible default).</summary>
    public const int QuestionCount = 150;
    public const int CareerQuestionCount = 200;
    public const int ValuesQuestionCount = 150;
    public const int CultureQuestionCount = 150;
    public const int CompetenceItemsPerDomain = 30;
    public const int ValuesItemsPerDomain = 30;
    public const int CultureItemsPerDomainMin = 12;
    public const int CultureItemsPerDomainMax = 15;
    public const int CareerItemsPerDomainMin = 33;
    public const int CareerItemsPerDomainMax = 34;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    public static readonly string[] BigFiveDomains = DeepAnalysisCompetenceItems.Domains;

    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCompetence = new(BuildCompetenceQuestions);
    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCareer = new(BuildCareerQuestions);
    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyValues = new(BuildValuesQuestions);
    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCulture = new(BuildCultureQuestions);

    /// <summary>Competence deep analysis (backward-compatible default).</summary>
    public static IReadOnlyList<DeepAnalysisQuestion> Questions => LazyCompetence.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> CareerQuestions => LazyCareer.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> ValuesQuestions => LazyValues.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> CultureQuestions => LazyCulture.Value;

    public static int QuestionCountFor(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => CareerQuestionCount,
        AssessmentKind.Values => ValuesQuestionCount,
        AssessmentKind.Culture => CultureQuestionCount,
        _ => QuestionCount
    };

    public static IReadOnlyList<DeepAnalysisQuestion> QuestionsFor(AssessmentKind kind)
        => kind switch
        {
            AssessmentKind.Career => CareerQuestions,
            AssessmentKind.Values => ValuesQuestions,
            AssessmentKind.Culture => CultureQuestions,
            _ => Questions
        };

    public static bool SupportsDeepAnalysis(AssessmentKind kind)
        => kind is AssessmentKind.Competence or AssessmentKind.Career or AssessmentKind.Values or AssessmentKind.Culture;

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCompetenceQuestions()
    {
        var list = Materialize("BigFive", DeepAnalysisCompetenceItems.All, AssessmentKind.Competence, CompetenceItemsPerDomain, CompetenceItemsPerDomain)
            .ToList();
        for (var i = 0; i < list.Count; i++)
        {
            list[i] = list[i] with { Facet = DeepAnalysisCompetenceFacets.CodeForIndex(i) };
        }

        return list;
    }

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCareerQuestions()
        => Materialize("RIASEC", DeepAnalysisCareerItems.All, AssessmentKind.Career, CareerItemsPerDomainMin, CareerItemsPerDomainMax);

    private static IReadOnlyList<DeepAnalysisQuestion> BuildValuesQuestions()
        => Materialize("Schwartz", DeepAnalysisValuesItems.All, AssessmentKind.Values, ValuesItemsPerDomain, ValuesItemsPerDomain);

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCultureQuestions()
        => Materialize("Culture", DeepAnalysisCultureItems.All, AssessmentKind.Culture, CultureItemsPerDomainMin, CultureItemsPerDomainMax);

    private static IReadOnlyList<DeepAnalysisQuestion> Materialize(
        string family,
        IReadOnlyList<(string Domain, bool Reverse, string Prompt)> items,
        AssessmentKind kind,
        int minPerDomain,
        int maxPerDomain)
    {
        var expectedTotal = QuestionCountFor(kind);
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

        EnsureQuality(list, kind, minPerDomain, maxPerDomain);
        return list;
    }

    private static void EnsureQuality(
        List<DeepAnalysisQuestion> list,
        AssessmentKind kind,
        int minPerDomain,
        int maxPerDomain)
    {
        var expectedTotal = QuestionCountFor(kind);
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
            var count = group.Count();
            if (count < minPerDomain || count > maxPerDomain)
            {
                throw new InvalidOperationException(
                    $"DeepAnalysisCatalog ({kind}) domain {group.Key} has {count} items, expected {minPerDomain}–{maxPerDomain}.");
            }

            if (group.Count(q => q.Reverse) < Math.Max(1, minPerDomain / 5))
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

    /// <summary>
    /// 0-100 per IPIP-NEO facet from reverse-corrected competence answers. Only the competence
    /// deep analysis carries facet codes, so this always scores <see cref="Questions"/>.
    /// </summary>
    public static IReadOnlyList<(string Facet, string Domain, int Percent, int Count)> ScoreFacets(
        IReadOnlyDictionary<int, int> answers)
    {
        var buckets = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        var domainByFacet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var question in Questions)
        {
            if (string.IsNullOrEmpty(question.Facet))
            {
                continue;
            }

            if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
            {
                continue;
            }

            var value = question.Reverse ? LikertMax + LikertMin - raw : raw;
            if (!buckets.TryGetValue(question.Facet, out var list))
            {
                list = [];
                buckets[question.Facet] = list;
            }

            list.Add(value);
            domainByFacet[question.Facet] = question.Domain;
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
                return (kv.Key, domainByFacet[kv.Key], percent, kv.Value.Count);
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
        SchwartzValuesCatalog.Autonomy =>
            "Eigen regie: rollen met ruimte om zelf te plannen en nieuwe aanpakken te proberen passen bij jouw drijfveren.",
        SchwartzValuesCatalog.Connection =>
            "Verbinding: teams met warme sfeer, klantcontact en collegiale hulp benutten wat jij belangrijk vindt.",
        SchwartzValuesCatalog.Achievement =>
            "Prestatie: meetbare doelen, targets en zichtbare groei geven je energie op de werkvloer.",
        SchwartzValuesCatalog.Stability =>
            "Zekerheid: vaste afspraken, veilige procedures en voorspelbare roosters sluiten aan op jouw waarden.",
        SchwartzValuesCatalog.Impact =>
            "Impact: organisaties die eerlijk, duurzaam of maatschappelijk nuttig werken versterken jouw fit.",
        CulturePersonalityCatalog.Informal =>
            "Informele sfeer ligt je: korte lijnen, directe toon en weinig hiërarchie voelen prettig.",
        CulturePersonalityCatalog.Collaboration =>
            "Samenwerken geeft je energie. Ploegenwerk en gedeelde doelen benutten dat.",
        CulturePersonalityCatalog.Flexibility =>
            "Flexibel meebewegen past: wisselende taken en snelle bijsturing liggen je.",
        CulturePersonalityCatalog.Innovation =>
            "Nieuwe dingen proberen past: verbeterideeën en frisse werkwijzen geven je energie.",
        CulturePersonalityCatalog.PeopleFirst =>
            "Mensen voorop zetten past: zorg, begeleiding en een warme teamcultuur sluiten aan.",
        CulturePersonalityCatalog.Openness =>
            "Openstaan voor nieuw: wisselende taken en leren op de vloer benutten jouw kracht.",
        CulturePersonalityCatalog.Conscientiousness =>
            "Netjes en betrouwbaar werken is een voorsprong bij kwaliteit en afronden.",
        CulturePersonalityCatalog.Extraversion =>
            "Energie van mensen: balie, teamvloer en klantcontact passen sterk.",
        CulturePersonalityCatalog.Agreeableness =>
            "Prettig samen optrekken: behulpzaamheid en sfeer wegen zwaar in jouw fit.",
        CulturePersonalityCatalog.EmotionalStability =>
            "Kalm onder druk: piekdagen en snelle wisselingen zijn haalbaarder voor jou.",
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
        SchwartzValuesCatalog.Autonomy => SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Autonomy),
        SchwartzValuesCatalog.Connection => SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Connection),
        SchwartzValuesCatalog.Achievement => SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Achievement),
        SchwartzValuesCatalog.Stability => SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Stability),
        SchwartzValuesCatalog.Impact => SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Impact),
        CulturePersonalityCatalog.Informal => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Informal),
        CulturePersonalityCatalog.Collaboration => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Collaboration),
        CulturePersonalityCatalog.Flexibility => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Flexibility),
        CulturePersonalityCatalog.Innovation => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Innovation),
        CulturePersonalityCatalog.PeopleFirst => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.PeopleFirst),
        CulturePersonalityCatalog.Openness => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Openness),
        CulturePersonalityCatalog.Conscientiousness => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Conscientiousness),
        CulturePersonalityCatalog.Extraversion => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Extraversion),
        CulturePersonalityCatalog.Agreeableness => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.Agreeableness),
        CulturePersonalityCatalog.EmotionalStability => CulturePersonalityCatalog.EverydayLabel(CulturePersonalityCatalog.EmotionalStability),
        _ => domain.ToLowerInvariant()
    };

    public static SchwartzValuesScores ToSchwartzScores(IReadOnlyList<DeepAnalysisDomainScore> scores)
    {
        int Get(string code) =>
            scores.FirstOrDefault(s => s.Domain.Equals(code, StringComparison.OrdinalIgnoreCase))?.Percent ?? 0;

        return new SchwartzValuesScores(
            Get(SchwartzValuesCatalog.Autonomy),
            Get(SchwartzValuesCatalog.Connection),
            Get(SchwartzValuesCatalog.Achievement),
            Get(SchwartzValuesCatalog.Stability),
            Get(SchwartzValuesCatalog.Impact));
    }

    public static CulturePersonalityScores ToCulturePersonalityScores(IReadOnlyList<DeepAnalysisDomainScore> scores)
    {
        int Get(string code) =>
            scores.FirstOrDefault(s => s.Domain.Equals(code, StringComparison.OrdinalIgnoreCase))?.Percent ?? 0;

        return new CulturePersonalityScores(
            Get(CulturePersonalityCatalog.Autonomy),
            Get(CulturePersonalityCatalog.Informal),
            Get(CulturePersonalityCatalog.Collaboration),
            Get(CulturePersonalityCatalog.Flexibility),
            Get(CulturePersonalityCatalog.Innovation),
            Get(CulturePersonalityCatalog.PeopleFirst),
            Get(CulturePersonalityCatalog.Openness),
            Get(CulturePersonalityCatalog.Conscientiousness),
            Get(CulturePersonalityCatalog.Extraversion),
            Get(CulturePersonalityCatalog.Agreeableness),
            Get(CulturePersonalityCatalog.EmotionalStability));
    }

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
    string PromptNl,
    string Facet = "");

public sealed record DeepAnalysisDomainScore(string Domain, int Percent, int AnsweredCount);
