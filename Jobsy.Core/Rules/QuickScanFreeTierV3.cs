using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Quick-Scan v3 free tier: max 25 v2 item ids per instrument (subset of v2 banks).
/// Stored as <see cref="AssessmentTestVersions.V3"/> when <c>TestsV2Enabled</c> is on.
/// Legacy v2 rows keep full 40/48/48/40 banks.
/// </summary>
public static class QuickScanFreeTierV3
{
    public const int QuestionCount = 25;

    /// <summary>Five items per workplace dimension (3 forward + 2 reverse each).</summary>
    public static readonly int[] CompetenceQuestionIds =
    [
        1, 2, 3, 4, 5,
        9, 10, 11, 12, 13,
        17, 18, 19, 20, 21,
        25, 26, 27, 28, 29,
        33, 34, 35, 36, 37
    ];

    /// <summary>Four items per RIASEC type plus one extra Artistic stem.</summary>
    public static readonly int[] CareerQuestionIds =
    [
        1, 2, 3, 4,
        9, 10, 11, 12,
        17, 18, 19, 20, 21,
        25, 26, 27, 28,
        33, 34, 35, 36,
        41, 42, 43, 44
    ];

    /// <summary>Two items per culture/personality dimension (12 dims) plus one Curiosity stem.</summary>
    public static readonly int[] CultureQuestionIds =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
        25, 26, 29, 30, 33, 34, 37, 38, 41, 42, 45, 46, 47
    ];

    /// <summary>Five items per Schwartz workplace driver (balanced forward/reverse).</summary>
    public static readonly int[] ValuesQuestionIds =
    [
        1, 2, 3, 4, 5,
        9, 10, 11, 12, 13,
        17, 18, 19, 20, 21,
        25, 26, 27, 28, 29,
        33, 34, 35, 36, 37
    ];

    public static int[] QuestionIds(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Competence => CompetenceQuestionIds,
        AssessmentKind.Career => CareerQuestionIds,
        AssessmentKind.Culture => CultureQuestionIds,
        AssessmentKind.Values => ValuesQuestionIds,
        _ => CompetenceQuestionIds
    };

    /// <summary>Five onboarding items; culture uses only ids from the v3 free set.</summary>
    public static int[] MiniIds(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Culture => [1, 3, 5, 7, 9],
        _ => OnboardingWizardCatalogV2.MiniIds(kind)
    };

    public static int[] DeeperIds(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Culture => [2, 4, 6, 8, 10],
        _ => OnboardingWizardCatalogV2.DeeperIds(kind)
    };

    public static int MaxStoredQuestionId(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => CareerTestCatalogV2.QuestionCount,
        AssessmentKind.Culture => CulturePersonalityCatalogV2.QuestionCount,
        AssessmentKind.Competence => CompetencyTestCatalogV2.QuestionCount,
        AssessmentKind.Values => SchwartzValuesCatalogV2.QuestionCount,
        _ => CompetencyTestCatalogV2.QuestionCount
    };

    public static IReadOnlyList<CompetencyQuestion> CompetenceQuestions()
        => Filter(CompetencyTestCatalogV2.Questions, CompetenceQuestionIds);

    public static IReadOnlyList<CareerQuestion> CareerQuestions()
        => Filter(CareerTestCatalogV2.Questions, CareerQuestionIds);

    public static IReadOnlyList<CompetencyQuestion> CultureQuestions()
        => Filter(CulturePersonalityCatalogV2.Questions, CultureQuestionIds);

    public static IReadOnlyList<CompetencyQuestion> ValuesQuestions()
        => Filter(SchwartzValuesCatalogV2.Questions, ValuesQuestionIds);

    public static string? ValidateAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool requireComplete)
    {
        var allowed = QuestionIds(kind).ToHashSet();
        foreach (var (id, value) in answers)
        {
            if (!allowed.Contains(id))
            {
                return kind switch
                {
                    AssessmentKind.Culture => "Onbekend vraagnummer in de cultuurscan.",
                    AssessmentKind.Career => "Onbekend vraagnummer in de beroepentest.",
                    AssessmentKind.Values => "Onbekend vraagnummer in de waardenscan.",
                    _ => "Onbekend vraagnummer in de competentietest."
                };
            }

            if (!LikertAnswerJson.IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(kind, answers))
        {
            return kind switch
            {
                AssessmentKind.Culture => "Beantwoord alle 25 stellingen om de cultuurscan af te ronden.",
                AssessmentKind.Career => "Beantwoord alle 25 vragen om de beroepentest af te ronden.",
                AssessmentKind.Values => "Beantwoord alle 25 stellingen om de waardenscan af te ronden.",
                _ => "Beantwoord alle 25 vragen om de Quick-Scan af te ronden."
            };
        }

        return null;
    }

    public static bool IsComplete(AssessmentKind kind, IReadOnlyDictionary<int, int> answers)
    {
        foreach (var id in QuestionIds(kind))
        {
            if (!answers.TryGetValue(id, out var v) || !LikertAnswerJson.IsValidAnswer(v))
            {
                return false;
            }
        }

        return true;
    }

    public static CompetencyScores? ScoreCompetence(IReadOnlyDictionary<int, int> answers)
        => CompetencyTestCatalogV2.Score(answers, CompetenceQuestions());

    public static RiasecScores? ScoreCareer(IReadOnlyDictionary<int, int> answers)
        => CareerTestCatalogV2.Score(answers, CareerQuestions());

    public static CulturePersonalityScores? ScoreCulture(IReadOnlyDictionary<int, int> answers)
        => CulturePersonalityCatalogV2.Score(answers, CultureQuestions());

    public static SchwartzValuesScores? ScoreValues(IReadOnlyDictionary<int, int> answers)
        => SchwartzValuesCatalogV2.Score(answers, ValuesQuestions());

    private static IReadOnlyList<CompetencyQuestion> Filter(
        IReadOnlyList<CompetencyQuestion> all,
        int[] ids)
    {
        var map = all.ToDictionary(q => q.Id);
        return ids.Select(id => map[id]).ToList();
    }

    private static IReadOnlyList<CareerQuestion> Filter(
        IReadOnlyList<CareerQuestion> all,
        int[] ids)
    {
        var map = all.ToDictionary(q => q.Id);
        return ids.Select(id => map[id]).ToList();
    }
}
