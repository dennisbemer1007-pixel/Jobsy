using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Free competence Quick-Scan: 25 Likert items based on Big Five / OCEAN workplace short form.
/// Five items per dimension: teamwork (A), results (C), stress resilience (ES), innovation (O), extraversion (E).
/// Matching uses the four workplace competencies; extraversion is stored as a tag/score.
/// Career interests live in <see cref="CareerTestCatalog"/>.
/// </summary>
public static class CompetencyTestCatalog
{
    public const int QuestionCount = 25;
    public const int BigFiveQuestionCount = 25;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;
    public const int CategoryQuestionCount = 5;

    public const string Samenwerken = "Samenwerken";
    public const string Resultaatgerichtheid = "Resultaatgerichtheid";
    public const string Stressbestendigheid = "Stressbestendigheid";
    public const string Innovatie = "Innovatie";
    public const string Extraversie = "Extraversie";

    public const string RiasecRealistic = "Realistic";
    public const string RiasecInvestigative = "Investigative";
    public const string RiasecArtistic = "Artistic";
    public const string RiasecSocial = "Social";
    public const string RiasecEnterprising = "Enterprising";
    public const string RiasecConventional = "Conventional";

    /// <summary>Workplace competencies used by vacancy matching.</summary>
    public static readonly string[] CategoryCodes =
    [
        Samenwerken,
        Resultaatgerichtheid,
        Stressbestendigheid,
        Innovatie
    ];

    /// <summary>All Quick-Scan fieldsets including Extraversie (OCEAN completeness).</summary>
    public static readonly string[] QuickScanCategories =
    [
        Samenwerken,
        Resultaatgerichtheid,
        Stressbestendigheid,
        Innovatie,
        Extraversie
    ];

    public static readonly string[] RiasecCodes =
    [
        RiasecRealistic,
        RiasecInvestigative,
        RiasecArtistic,
        RiasecSocial,
        RiasecEnterprising,
        RiasecConventional
    ];

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        new(1, Samenwerken, Reverse: false, "Competency.Q01"),
        new(2, Samenwerken, Reverse: false, "Competency.Q02"),
        new(3, Samenwerken, Reverse: false, "Competency.Q03"),
        new(4, Samenwerken, Reverse: true, "Competency.Q04"),
        new(5, Samenwerken, Reverse: true, "Competency.Q05"),
        new(6, Resultaatgerichtheid, Reverse: false, "Competency.Q06"),
        new(7, Resultaatgerichtheid, Reverse: false, "Competency.Q07"),
        new(8, Resultaatgerichtheid, Reverse: false, "Competency.Q08"),
        new(9, Resultaatgerichtheid, Reverse: true, "Competency.Q09"),
        new(10, Resultaatgerichtheid, Reverse: true, "Competency.Q10"),
        new(11, Stressbestendigheid, Reverse: false, "Competency.Q11"),
        new(12, Stressbestendigheid, Reverse: false, "Competency.Q12"),
        new(13, Stressbestendigheid, Reverse: false, "Competency.Q13"),
        new(14, Stressbestendigheid, Reverse: true, "Competency.Q14"),
        new(15, Stressbestendigheid, Reverse: true, "Competency.Q15"),
        new(16, Innovatie, Reverse: false, "Competency.Q16"),
        new(17, Innovatie, Reverse: false, "Competency.Q17"),
        new(18, Innovatie, Reverse: false, "Competency.Q18"),
        new(19, Innovatie, Reverse: true, "Competency.Q19"),
        new(20, Innovatie, Reverse: true, "Competency.Q20"),
        new(21, Extraversie, Reverse: false, "Competency.Q21"),
        new(22, Extraversie, Reverse: false, "Competency.Q22"),
        new(23, Extraversie, Reverse: false, "Competency.Q23"),
        new(24, Extraversie, Reverse: true, "Competency.Q24"),
        new(25, Extraversie, Reverse: true, "Competency.Q25")
    ];

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (id, value) in answers)
        {
            if (id is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de competentietest.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 25 vragen om de Quick-Scan af te ronden.";
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

    /// <summary>
    /// Converts Likert answers to 0–100% per Big Five workplace category.
    /// Extraversion is scored separately and does not change the four matching competencies.
    /// </summary>
    public static CompetencyScores? Score(IReadOnlyDictionary<int, int> answers)
    {
        int? ScoreCategory(string category)
        {
            var items = Questions.Where(q => q.Category == category).ToList();
            var scored = new List<int>();
            foreach (var question in items)
            {
                if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
                {
                    continue;
                }

                scored.Add(question.Reverse ? LikertMax + LikertMin - raw : raw);
            }

            return scored.Count == 0 ? null : LikertAnswerJson.ToPercent(scored);
        }

        var samenwerken = ScoreCategory(Samenwerken);
        var resultaat = ScoreCategory(Resultaatgerichtheid);
        var stress = ScoreCategory(Stressbestendigheid);
        var innovatie = ScoreCategory(Innovatie);
        var extraversie = ScoreCategory(Extraversie);
        if (samenwerken is null && resultaat is null && stress is null && innovatie is null && extraversie is null)
        {
            return null;
        }

        return new CompetencyScores(samenwerken, resultaat, stress, innovatie, extraversie);
    }

    /// <summary>
    /// Legacy helper: compact RIASEC probes used to live on Q21–Q25 of the combined Quick-Scan.
    /// New career interests come from <see cref="CareerTestCatalog"/>.
    /// </summary>
    public static IReadOnlyList<string> DeriveRiasecTags(IReadOnlyDictionary<int, int> answers)
        => CareerTestCatalog.DeriveLegacyCompactRiasecTags(answers);

    public static IReadOnlyList<string> DeriveMatchTags(
        CompetencyScores? scores,
        IReadOnlyList<string>? extraTags = null)
    {
        var tags = new List<string>();
        if (scores is not null)
        {
            foreach (var category in CategoryCodes)
            {
                if (scores.TryGet(category) is >= 70)
                {
                    tags.Add(category);
                }
            }

            if (scores.Extraversie is >= 70)
            {
                tags.Add(Extraversie);
            }
        }

        if (extraTags is not null)
        {
            foreach (var tag in extraTags)
            {
                if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    tags.Add(tag);
                }
            }
        }

        return tags;
    }

    public static string SerializeTags(IEnumerable<string> tags)
        => LikertAnswerJson.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
        => LikertAnswerJson.ParseTags(json);

    public static CompetencyScores? CompletedScoresOrNull(
        string? status,
        int? samenwerken,
        int? resultaat,
        int? stress,
        int? innovatie,
        int? extraversie = null)
    {
        if (!CandidateCompetencyStatuses.IsCompleted(status)
            || samenwerken is null
            || resultaat is null
            || stress is null
            || innovatie is null)
        {
            return null;
        }

        return new CompetencyScores(samenwerken, resultaat, stress, innovatie, extraversie);
    }
}

public sealed record CompetencyQuestion(
    int Id,
    string Category,
    bool Reverse,
    string TextKey,
    bool IsRiasec = false);

/// <summary>Workplace competencies as 0–100 percentages (null = not enough answers).</summary>
public sealed record CompetencyScores(
    int? Samenwerken,
    int? Resultaatgerichtheid,
    int? Stressbestendigheid,
    int? Innovatie,
    int? Extraversie = null)
{
    public bool IsComplete =>
        Samenwerken is not null
        && Resultaatgerichtheid is not null
        && Stressbestendigheid is not null
        && Innovatie is not null;

    public int Get(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => Samenwerken ?? 0,
        CompetencyTestCatalog.Resultaatgerichtheid => Resultaatgerichtheid ?? 0,
        CompetencyTestCatalog.Stressbestendigheid => Stressbestendigheid ?? 0,
        CompetencyTestCatalog.Innovatie => Innovatie ?? 0,
        CompetencyTestCatalog.Extraversie => Extraversie ?? 0,
        _ => 0
    };

    public int? TryGet(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => Samenwerken,
        CompetencyTestCatalog.Resultaatgerichtheid => Resultaatgerichtheid,
        CompetencyTestCatalog.Stressbestendigheid => Stressbestendigheid,
        CompetencyTestCatalog.Innovatie => Innovatie,
        CompetencyTestCatalog.Extraversie => Extraversie,
        _ => null
    };
}
