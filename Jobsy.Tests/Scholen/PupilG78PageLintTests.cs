using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78PageLintTests
{
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Realistic_pages_pass_lint((PupilResult Result, PupilProgress Progress) fixture)
    {
        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(fixture.Progress.AnswersJson!)!;
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        var issues = PupilG78PageLint.Run(pkg, answers);
        Assert.True(issues.Count == 0, FormatLint(issues));
    }

    [Theory]
    [InlineData(200)]
    public void Random_pages_pass_lint(int seedCount)
    {
        var allIssues = new List<string>();
        for (var seed = 0; seed < seedCount; seed++)
        {
            var (result, progress) = PupilG78RandomFixtures.ForSeed(seed);
            var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(progress.AnswersJson!)!;
            var pkg = PupilG78FactStory.Build(result, progress, "nl");
            var issues = PupilG78PageLint.Run(pkg, answers);
            if (issues.Count > 0)
            {
                allIssues.Add($"seed {seed}: {FormatLint(issues)}");
            }
        }

        Assert.True(allIssues.Count == 0, string.Join("\n", allIssues.Take(12)));
    }

    public static IEnumerable<object[]> Fixtures() =>
    [
        [PupilG78RealisticFixtures.HelpsOthers()],
        [PupilG78RealisticFixtures.LeadsGroup()],
        [PupilG78RealisticFixtures.Maker()],
    ];

    private static string FormatLint(IReadOnlyList<PupilG78PageLintIssue> issues)
        => string.Join("; ", issues.Select(i => $"{i.Code}:{i.Detail}"));
}
