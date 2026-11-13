using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Tests.Scholen;

/// <summary>Full g78-2 runs for screenshots and story divergence tests.</summary>
internal static class PupilG78ContrastV2Fixtures
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static (PupilResult Result, PupilProgress Progress) AllTowardA() => BuildProfile(1);

    internal static (PupilResult Result, PupilProgress Progress) AllTowardB() => BuildProfile(5);

    private static (PupilResult Result, PupilProgress Progress) BuildProfile(int answer)
    {
        var bank = new PupilQuestionBankG78V2();
        var answersString = new Dictionary<string, int>(StringComparer.Ordinal);
        var answersNumeric = new Dictionary<int, int>();
        for (var qid = PupilQuestionBank.MinId; qid <= PupilQuestionBank.MaxId; qid++)
        {
            answersString[qid.ToString()] = answer;
            answersNumeric[qid] = answer;
        }

        var items = bank.Questions;
        var competence = PupilG78ContrastLikertScorer.ScoreCompetence(answersNumeric, items);
        var riasec = PupilG78ContrastLikertScorer.ScoreCareer(answersNumeric, items)!;
        var values = PupilG78ContrastLikertScorer.ScoreValues(answersNumeric, items);
        var culture = PupilG78ContrastLikertScorer.ScoreCulture(answersNumeric, items);
        var holland = CareerTestCatalog.HollandCode(riasec);
        var id = Guid.NewGuid();
        var progress = new PupilProgress
        {
            PupilCodeId = id,
            AnswersJson = JsonSerializer.Serialize(answersString),
            QuestionScoringVersion = PupilG78ScoringVersions.V2,
            LikesJson = """["sport","dieren"]""",
            VakScoresJson = """{"natuur-techniek":{"leuk":4,"goed":4},"tekenen":{"leuk":5,"goed":4}}""",
            JourneyThemeKey = PupilJourneyTheme.ZeeExpeditie,
            JourneyThemeChosenAtUtc = DateTime.UtcNow,
        };

        var result = new PupilResult
        {
            PupilCodeId = id,
            ScoringVersion = PupilG78ScoringVersions.V2,
            HollandCode = holland,
            TopValue = TopValueKey(values),
            TopCulture = TopCultureKey(culture),
            CompetenceScoresJson = Serialize(competence),
            RiasecScoresJson = Serialize(riasec),
            ValuesScoresJson = Serialize(values),
            CultureScoresJson = Serialize(culture),
            CompletedAtUtc = DateTime.UtcNow,
        };

        var likes = new List<string> { "sport", "dieren" };
        var keys = PupilStoryTemplates.SelectKeys(result, likes, PupilQuestionSet.Groep78, progress);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);
        return (result, progress);
    }

    private static string Serialize<T>(T? value) where T : class
        => value is null ? "{}" : JsonSerializer.Serialize(value, Json);

    private static string? TopByScores<T>(
        T? scores,
        IReadOnlyList<string> order,
        Func<string, int?> getter) where T : class
    {
        if (scores is null)
        {
            return null;
        }

        string? best = null;
        var bestVal = int.MinValue;
        foreach (var code in order)
        {
            var v = getter(code);
            if (v is null)
            {
                continue;
            }

            if (v.Value > bestVal)
            {
                bestVal = v.Value;
                best = code;
            }
        }

        return best;
    }

    private static string? TopValueKey(SchwartzValuesScores? scores)
        => TopByScores(scores, SchwartzValuesCatalog.CategoryCodes, code => code switch
        {
            SchwartzValuesCatalog.Autonomy => scores?.Autonomy,
            SchwartzValuesCatalog.Connection => scores?.Connection,
            SchwartzValuesCatalog.Achievement => scores?.Achievement,
            SchwartzValuesCatalog.Stability => scores?.Stability,
            SchwartzValuesCatalog.Impact => scores?.Impact,
            _ => null,
        });

    private static string? TopCultureKey(CulturePersonalityScores? scores)
        => TopByScores(scores, CulturePersonalityCatalog.CultureDimensionCodes, code => code switch
        {
            CulturePersonalityCatalog.Autonomy => scores?.Autonomy,
            CulturePersonalityCatalog.Informal => scores?.Informal,
            CulturePersonalityCatalog.Collaboration => scores?.Collaboration,
            CulturePersonalityCatalog.Flexibility => scores?.Flexibility,
            CulturePersonalityCatalog.Innovation => scores?.Innovation,
            CulturePersonalityCatalog.PeopleFirst => scores?.PeopleFirst,
            _ => null,
        });
}
