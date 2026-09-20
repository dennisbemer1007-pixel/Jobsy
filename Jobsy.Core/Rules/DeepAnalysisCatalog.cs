using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Paid 150-item deep analyses — competence (Big Five + skills) and career (RIASEC + orientation) are separate catalogs.
/// </summary>
public static class DeepAnalysisCatalog
{
    public const int QuestionCount = 150;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCompetence = new(BuildCompetenceQuestions);
    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyCareer = new(BuildCareerQuestions);

    /// <summary>Competence deep analysis (backward-compatible default).</summary>
    public static IReadOnlyList<DeepAnalysisQuestion> Questions => LazyCompetence.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> CareerQuestions => LazyCareer.Value;

    public static IReadOnlyList<DeepAnalysisQuestion> QuestionsFor(AssessmentKind kind)
        => kind == AssessmentKind.Career ? CareerQuestions : Questions;

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCompetenceQuestions()
    {
        var list = new List<DeepAnalysisQuestion>(QuestionCount);
        var id = 1;

        // Big Five facets: 5 traits × 20 = 100
        string[] bigFive =
        [
            "Openheid", "Consciëntieusheid", "Extraversie", "Inschikkelijkheid", "EmotioneleStabiliteit"
        ];
        string[] bigFivePrompts =
        [
            "Ik zoek graag nieuwe ideeën en werkwijzen.",
            "Ik werk systematisch en houd afspraken bij.",
            "Ik krijg energie van contact met collega's.",
            "Ik houd rekening met de gevoelens van anderen.",
            "Ik blijf rustig onder tijdsdruk."
        ];
        for (var t = 0; t < bigFive.Length; t++)
        {
            for (var i = 0; i < 20; i++)
            {
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "BigFive",
                    bigFive[t],
                    Reverse: i % 5 == 4,
                    $"{bigFivePrompts[t]} (variant {i + 1})"));
            }
        }

        // Practical capacity / skills: 10 × 5 = 50
        string[] practicalThemes =
        [
            "FysiekeBelasting", "MentaleBelasting", "Klantcontact", "Zelfstandigheid",
            "Ploegendienst", "Leervermogen", "Nauwkeurigheid", "Tempo",
            "Verantwoordelijkheid", "Aanpassingsvermogen"
        ];
        string[] practicalPrompts =
        [
            "Ik kan fysiek zwaar of staand werk lang volhouden.",
            "Ik kan complexe informatie combineren zonder overprikkeld te raken.",
            "Ik voel me op mijn gemak bij klant- of gastcontact.",
            "Ik werk goed zelfstandig zonder voortdurende sturing.",
            "Ik kan wisselende of onregelmatige diensten aan.",
            "Ik leer nieuwe systemen of taken snel.",
            "Ik controleer mijn werk zorgvuldig op fouten.",
            "Ik houd een hoog werktempo vol zonder kwaliteit in te leveren.",
            "Ik neem verantwoordelijkheid als iets misgaat.",
            "Ik schakel snel als de planning of de taak verandert."
        ];
        for (var t = 0; t < practicalThemes.Length; t++)
        {
            for (var i = 0; i < 5; i++)
            {
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "Practical",
                    practicalThemes[t],
                    Reverse: i == 4,
                    $"{practicalPrompts[t]} (variant {i + 1})"));
            }
        }

        EnsureCount(list, AssessmentKind.Competence);
        return list;
    }

    private static IReadOnlyList<DeepAnalysisQuestion> BuildCareerQuestions()
    {
        var list = new List<DeepAnalysisQuestion>(QuestionCount);
        var id = 1;

        // RIASEC: 6 × 20 = 120
        string[] riasec = CareerTestCatalog.RiasecCodes.ToArray();
        string[] riasecPrompts =
        [
            "Ik werk graag met machines, gereedschap of buiten.",
            "Ik onderzoek graag hoe iets werkt of waarom iets zo is.",
            "Ik bedenk graag creatieve of visuele oplossingen.",
            "Ik help graag mensen of werk in een team.",
            "Ik neem graag initiatief en overtuig anderen.",
            "Ik houd van orde, administratie en vaste procedures."
        ];
        for (var t = 0; t < riasec.Length; t++)
        {
            for (var i = 0; i < 20; i++)
            {
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "RIASEC",
                    riasec[t],
                    Reverse: i % 5 == 4,
                    $"{riasecPrompts[t]} (variant {i + 1})"));
            }
        }

        // Career orientation: 6 × 5 = 30
        string[] careerThemes =
        [
            "Werkwaarden", "Autonomie", "Zekerheid", "Groei", "Impact", "Variatie"
        ];
        string[] careerPrompts =
        [
            "Ik wil werk dat aansluit bij wat ik belangrijk vind in het leven.",
            "Ik wil zelf kunnen bepalen hoe ik mijn werk aanpak.",
            "Ik zoek zekerheid en een voorspelbaar rooster.",
            "Ik wil doorgroeien en nieuwe vaardigheden leren.",
            "Ik wil werk waarmee ik iets bijdraag voor anderen.",
            "Ik zoek afwisseling in taken en werkomgeving."
        ];
        for (var t = 0; t < careerThemes.Length; t++)
        {
            for (var i = 0; i < 5; i++)
            {
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "Career",
                    careerThemes[t],
                    Reverse: i == 4,
                    $"{careerPrompts[t]} (variant {i + 1})"));
            }
        }

        EnsureCount(list, AssessmentKind.Career);
        return list;
    }

    private static void EnsureCount(List<DeepAnalysisQuestion> list, AssessmentKind kind)
    {
        if (list.Count != QuestionCount)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog ({kind}) must contain {QuestionCount} questions, got {list.Count}.");
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
}

public sealed record DeepAnalysisQuestion(
    int Id,
    string Family,
    string Domain,
    bool Reverse,
    string PromptNl);
