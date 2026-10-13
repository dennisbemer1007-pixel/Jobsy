using System.Text.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Freezes adult Likert category scores before extracting <see cref="LikertCategoryScorer"/>.
/// Regenerate with JOBSY_UPDATE_LIKERT_GOLDEN=1 if the intentional math changes.
/// </summary>
public class LikertCategoryScorerGoldenTests
{
    private const string RelativeBaseline = "Jobsy.Tests/Baselines/likert-category-scorer-golden.json";
    private const int RandomSets = 200;
    private const int Seed = 42;

    [Fact]
    public void Adult_catalog_scores_match_golden_snapshot()
    {
        var snapshot = BuildSnapshot();
        var json = Serialize(snapshot);
        var root = FindRepoRoot();
        var path = Path.Combine(root, RelativeBaseline);

        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_LIKERT_GOLDEN"),
            "1",
            StringComparison.Ordinal);

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        Assert.True(File.Exists(path), $"Missing {RelativeBaseline}");
        var onDisk = Normalize(File.ReadAllText(path));
        Assert.True(
            string.Equals(Normalize(json), onDisk, StringComparison.Ordinal),
            $"{RelativeBaseline} differs from live adult scores. " +
            "If intentional: JOBSY_UPDATE_LIKERT_GOLDEN=1 dotnet test --filter FullyQualifiedName~LikertCategoryScorerGolden");
    }

    [Fact]
    public void Edge_cases_all_1_all_5_all_3_and_missing_are_stable()
    {
        var snap = BuildSnapshot();
        Assert.Contains(snap.Cases, c => c.Name == "competency-all-1");
        Assert.Contains(snap.Cases, c => c.Name == "career-all-5");
        Assert.Contains(snap.Cases, c => c.Name == "schwartz-all-3");
        Assert.Contains(snap.Cases, c => c.Name == "culture-missing-half");
        Assert.Equal(50, snap.Cases.First(c => c.Name == "competency-all-3").Scores["Samenwerken"]);
        Assert.Equal(50, snap.Cases.First(c => c.Name == "career-all-3").Scores["Realistic"]);
        Assert.Equal(50, snap.Cases.First(c => c.Name == "schwartz-all-3").Scores["Autonomy"]);
        Assert.Equal(50, snap.Cases.First(c => c.Name == "culture-all-3").Scores["Autonomy"]);
    }

    private static GoldenSnapshot BuildSnapshot()
    {
        var cases = new List<GoldenCase>();
        cases.AddRange(EdgeCases());
        var rng = new Random(Seed);
        for (var i = 0; i < RandomSets; i++)
        {
            cases.Add(CompetencyCase($"competency-rand-{i}", RandomAnswers(rng, CompetencyTestCatalog.QuestionCount)));
            cases.Add(CareerCase($"career-rand-{i}", RandomAnswers(rng, CareerTestCatalog.QuestionCount)));
            cases.Add(SchwartzCase($"schwartz-rand-{i}", RandomAnswers(rng, SchwartzValuesCatalog.QuestionCount)));
            cases.Add(CultureCase($"culture-rand-{i}", RandomAnswers(rng, CulturePersonalityCatalog.QuestionCount)));
        }

        return new GoldenSnapshot(cases);
    }

    private static IEnumerable<GoldenCase> EdgeCases()
    {
        yield return CompetencyCase("competency-all-1", Fill(CompetencyTestCatalog.QuestionCount, 1));
        yield return CompetencyCase("competency-all-5", Fill(CompetencyTestCatalog.QuestionCount, 5));
        yield return CompetencyCase("competency-all-3", Fill(CompetencyTestCatalog.QuestionCount, 3));
        yield return CompetencyCase("competency-missing-odd",
            Enumerable.Range(1, CompetencyTestCatalog.QuestionCount).Where(i => i % 2 == 0)
                .ToDictionary(i => i, _ => 4));

        yield return CareerCase("career-all-1", Fill(CareerTestCatalog.QuestionCount, 1));
        yield return CareerCase("career-all-5", Fill(CareerTestCatalog.QuestionCount, 5));
        yield return CareerCase("career-all-3", Fill(CareerTestCatalog.QuestionCount, 3));
        yield return CareerCase("career-missing-odd",
            Enumerable.Range(1, CareerTestCatalog.QuestionCount).Where(i => i % 2 == 0)
                .ToDictionary(i => i, _ => 2));

        yield return SchwartzCase("schwartz-all-1", Fill(SchwartzValuesCatalog.QuestionCount, 1));
        yield return SchwartzCase("schwartz-all-5", Fill(SchwartzValuesCatalog.QuestionCount, 5));
        yield return SchwartzCase("schwartz-all-3", Fill(SchwartzValuesCatalog.QuestionCount, 3));
        yield return SchwartzCase("schwartz-missing-odd",
            Enumerable.Range(1, SchwartzValuesCatalog.QuestionCount).Where(i => i % 2 == 0)
                .ToDictionary(i => i, _ => 5));

        yield return CultureCase("culture-all-1", Fill(CulturePersonalityCatalog.QuestionCount, 1));
        yield return CultureCase("culture-all-5", Fill(CulturePersonalityCatalog.QuestionCount, 5));
        yield return CultureCase("culture-all-3", Fill(CulturePersonalityCatalog.QuestionCount, 3));
        yield return CultureCase("culture-missing-half",
            Enumerable.Range(1, CulturePersonalityCatalog.QuestionCount).Where(i => i <= 9)
                .ToDictionary(i => i, _ => 4));
    }

    private static GoldenCase CompetencyCase(string name, Dictionary<int, int> answers)
    {
        var s = CompetencyTestCatalog.Score(answers);
        return new GoldenCase(name, "competency", ScoresFrom(s));
    }

    private static GoldenCase CareerCase(string name, Dictionary<int, int> answers)
    {
        var s = CareerTestCatalog.Score(answers);
        return new GoldenCase(name, "career", ScoresFrom(s));
    }

    private static GoldenCase SchwartzCase(string name, Dictionary<int, int> answers)
    {
        var s = SchwartzValuesCatalog.Score(answers);
        return new GoldenCase(name, "schwartz", ScoresFrom(s));
    }

    private static GoldenCase CultureCase(string name, Dictionary<int, int> answers)
    {
        var s = CulturePersonalityCatalog.Score(answers);
        return new GoldenCase(name, "culture", ScoresFrom(s));
    }

    private static Dictionary<string, int?> ScoresFrom(CompetencyScores? s) => s is null
        ? new(StringComparer.Ordinal) { ["null"] = null }
        : new(StringComparer.Ordinal)
        {
            ["Samenwerken"] = s.Samenwerken,
            ["Resultaatgerichtheid"] = s.Resultaatgerichtheid,
            ["Stressbestendigheid"] = s.Stressbestendigheid,
            ["Innovatie"] = s.Innovatie,
            ["Extraversie"] = s.Extraversie
        };

    private static Dictionary<string, int?> ScoresFrom(RiasecScores? s) => s is null
        ? new(StringComparer.Ordinal) { ["null"] = null }
        : new(StringComparer.Ordinal)
        {
            ["Realistic"] = s.Realistic,
            ["Investigative"] = s.Investigative,
            ["Artistic"] = s.Artistic,
            ["Social"] = s.Social,
            ["Enterprising"] = s.Enterprising,
            ["Conventional"] = s.Conventional
        };

    private static Dictionary<string, int?> ScoresFrom(SchwartzValuesScores? s) => s is null
        ? new(StringComparer.Ordinal) { ["null"] = null }
        : new(StringComparer.Ordinal)
        {
            ["Autonomy"] = s.Autonomy,
            ["Connection"] = s.Connection,
            ["Achievement"] = s.Achievement,
            ["Stability"] = s.Stability,
            ["Impact"] = s.Impact
        };

    private static Dictionary<string, int?> ScoresFrom(CulturePersonalityScores? s) => s is null
        ? new(StringComparer.Ordinal) { ["null"] = null }
        : new(StringComparer.Ordinal)
        {
            ["Autonomy"] = s.Autonomy,
            ["Informal"] = s.Informal,
            ["Collaboration"] = s.Collaboration,
            ["Flexibility"] = s.Flexibility,
            ["Innovation"] = s.Innovation,
            ["PeopleFirst"] = s.PeopleFirst,
            ["Openness"] = s.Openness,
            ["Conscientiousness"] = s.Conscientiousness,
            ["Extraversion"] = s.Extraversion,
            ["Agreeableness"] = s.Agreeableness,
            ["EmotionalStability"] = s.EmotionalStability
        };

    private static Dictionary<int, int> Fill(int count, int value)
        => Enumerable.Range(1, count).ToDictionary(i => i, _ => value);

    private static Dictionary<int, int> RandomAnswers(Random rng, int count)
        => Enumerable.Range(1, count).ToDictionary(i => i, _ => rng.Next(1, 6));

    private static string Serialize(GoldenSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) + "\n";

    private static string Normalize(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found from " + AppContext.BaseDirectory);
    }

    private sealed record GoldenSnapshot(IReadOnlyList<GoldenCase> Cases);

    private sealed record GoldenCase(string Name, string Catalog, Dictionary<string, int?> Scores);
}
