using System.Reflection;
using Jobsy.Infrastructure.Data.Migrations;

namespace Jobsy.Tests.Scholen;

public class AddQuestionSetToAggregatesMigrationTests
{
    [Fact]
    public void Migration_backfills_scoring_version_and_aggregate_question_set()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure",
            "Data",
            "Migrations",
            "20261003050300_AddQuestionSetToAggregates.cs");
        Assert.True(File.Exists(path), path);
        var src = File.ReadAllText(path);
        Assert.Contains("""SET "ScoringVersion" = 'g78-1'""", src, StringComparison.Ordinal);
        Assert.Contains("""WHERE "QuestionSet" = 1""", src, StringComparison.Ordinal);
        Assert.Contains("""CASE WHEN "Level" = 8 THEN 1 ELSE 2 END""", src, StringComparison.Ordinal);
        Assert.Contains("""UPDATE "SchoolYearAggregates""", src, StringComparison.Ordinal);
        Assert.Contains(
            "IX_SchoolYearAggregates_SchoolId_SchoolYearStart_QuestionSet",
            src,
            StringComparison.Ordinal);
        Assert.Equal(typeof(AddQuestionSetToAggregates), typeof(AddQuestionSetToAggregates));
        Assert.NotNull(typeof(AddQuestionSetToAggregates).GetMethod(
            "Up", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
    }

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
}
