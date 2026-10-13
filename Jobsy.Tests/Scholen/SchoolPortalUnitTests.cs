using System.Text;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class ClassResultsAggregatorTests
{
    [Fact]
    public void Hides_totals_below_k_anonymity()
    {
        var results = Enumerable.Range(0, 4).Select(i => MakeResult($"R", "Helpen", "arts")).ToList();
        var agg = ClassResultsAggregator.Aggregate(results, totalCodes: 28, PupilQuestionSet.Vo);
        Assert.False(agg.TotalsVisible);
        Assert.Empty(agg.RiasecTop3);
    }

    [Fact]
    public void Shows_totals_and_collapses_dream_jobs_under_2()
    {
        var results = new List<PupilResult>
        {
            MakeResult("SAE", "Helpen", "arts"),
            MakeResult("SAE", "Helpen", "arts"),
            MakeResult("RIC", "Vrijheid", "gamer"),
            MakeResult("ESA", "Samen", "kapper"),
            MakeResult("CSE", "Zekerheid", "elektricien"),
        };
        var agg = ClassResultsAggregator.Aggregate(results, totalCodes: 28, PupilQuestionSet.Vo);
        Assert.True(agg.TotalsVisible);
        Assert.Equal(5, agg.CompletedCount);
        Assert.Contains(agg.DreamJobs, d => d.Key == "arts" && d.Count == 2);
        Assert.Contains(agg.DreamJobs, d => d.Key == "Overig");
        var overig = agg.DreamJobs.First(d => d.Key == "Overig");
        Assert.Equal(3, overig.Count);
    }

    [Fact]
    public void Undecided_dream_jobs_stay_visible_when_nobody_picked_a_job()
    {
        var results = Enumerable.Range(0, 5)
            .Select(_ => MakeResult("R", "Helpen", ClassResultsAggregator.UndecidedDreamJobKey))
            .ToList();
        var agg = ClassResultsAggregator.Aggregate(results, totalCodes: 10, PupilQuestionSet.Vo);
        Assert.True(agg.TotalsVisible);
        Assert.Empty(agg.DreamJobs);
        Assert.Equal(5, agg.UndecidedDreamJobCount);
        Assert.Equal(0, agg.NotFilledDreamJobCount);
    }

    [Fact]
    public void Empty_dream_job_is_not_filled_and_only_the_explicit_choice_is_undecided()
    {
        var results = new List<PupilResult>
        {
            MakeResult("SAE", "Helpen", "arts"),
            MakeResult("SAE", "Helpen", "arts"),
            MakeResult("RIC", "Vrijheid", ClassResultsAggregator.UndecidedDreamJobKey),
            MakeResult("ESA", "Samen", ""),
            MakeResult("CSE", "Zekerheid", null!),
        };
        var agg = ClassResultsAggregator.Aggregate(results, totalCodes: 10, PupilQuestionSet.Vo);
        Assert.True(agg.TotalsVisible);
        Assert.Equal(1, agg.UndecidedDreamJobCount);
        Assert.Equal(2, agg.NotFilledDreamJobCount);
        Assert.Contains(agg.DreamJobs, d => d.Key == "arts" && d.Count == 2);
        Assert.DoesNotContain(agg.DreamJobs, d => d.Key == ClassResultsAggregator.UndecidedDreamJobKey);

        var teacher = ClassResultsAggregator.AggregateTeacherGroup(results, PupilQuestionSet.Vo);
        Assert.Equal(1, teacher.UndecidedDreamJobCount);
        Assert.Equal(2, teacher.NotFilledDreamJobCount);
    }

    [Fact]
    public void Guard_throws_when_result_belongs_to_other_test()
    {
        var results = new List<PupilResult>
        {
            MakeResult("SAE", "Helpen", "arts", scoringVersion: "g78-1"),
            MakeResult("RIC", "Vrijheid", "kok", scoringVersion: "1"),
        };
        Assert.Throws<InvalidOperationException>(() =>
            ClassResultsAggregator.Aggregate(results, totalCodes: 28, PupilQuestionSet.Groep78));
    }

    private static PupilResult MakeResult(
        string holland,
        string topValue,
        string dream,
        string scoringVersion = "1") => new()
        {
            PupilCodeId = Guid.NewGuid(),
            SchoolClassId = Guid.NewGuid(),
            CompletedAtUtc = DateTime.UtcNow,
            HollandCode = holland,
            TopValue = topValue,
            DreamJobKey = dream,
            CompetenceScoresJson = "{}",
            RiasecScoresJson = "{}",
            ValuesScoresJson = "{}",
            CultureScoresJson = "{}",
            ScoringVersion = scoringVersion,
            StoryTemplateVersion = "t",
            StoryKeysJson = "[]"
        };
}

public class SchoolTodoBuilderTests
{
    [Fact]
    public void Builds_parental_teacher_and_retention_items()
    {
        var today = new DateOnly(2026, 7, 15);
        var input = new SchoolTodoInput(
            DateTime.UtcNow,
            today,
            new DateOnly(2026, 7, 31),
            [
                new SchoolTodoClassInput(
                    Guid.NewGuid(), "2B", HasTeacher: false, ParentalConfirmed: false,
                    TestWindow: Core.Enums.TestWindowState.NotOpen, TestWindowClosesOn: null,
                    CodeCount: 28, CompletedCount: 0, LoginPausedUntilUtc: null)
            ],
            [
                new SchoolTodoInviteInput(Guid.NewGuid(), Guid.NewGuid(), "K. Mulder", DateTime.UtcNow.AddDays(-4))
            ]);

        var items = SchoolTodoBuilder.Build(input);
        Assert.Contains(items, i => i.Kind == SchoolTodoKind.ParentalConfirmationMissing);
        Assert.Contains(items, i => i.Kind == SchoolTodoKind.ClassWithoutTeacher);
        Assert.Contains(items, i => i.Kind == SchoolTodoKind.TeacherInvitePending);
        Assert.Contains(items, i => i.Kind == SchoolTodoKind.RetentionCutoffSoon);
    }
}

public class SchoolCodeListCsvTests
{
    [Fact]
    public void Header_exact_and_name_column_empty()
    {
        var bytes = SchoolCodeListCsv.Build(
        [
            new SchoolCodeListRow(1, "K7Q-M2P"),
            new SchoolCodeListRow(2, "B4X-T9R")
        ]);
        var text = Encoding.UTF8.GetString(bytes);
        // Strip BOM
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(SchoolCodeListCsv.Header, lines[0]);
        Assert.Equal(3, lines.Length);
        foreach (var line in lines.Skip(1))
        {
            var cols = line.Split(';');
            Assert.Equal(3, cols.Length);
            Assert.True(string.IsNullOrEmpty(cols[2]), $"Name column must be empty: '{line}'");
        }
    }
}

public class SchoolCodeListPdfTests
{
    [Fact]
    public void Pdf_is_valid_pdf_and_copy_uses_empty_name_column()
    {
        Assert.Equal("Naam (vul zelf in)", SchoolCodeListPdfCopy.NameColumnHeader);
        Assert.Contains("Lobsy bewaart geen namen", SchoolCodeListPdfCopy.NoNamesFooter, StringComparison.Ordinal);

        var pdf = new Infrastructure.Scholen.SchoolCodeListPdfService();
        var rows = new[]
        {
            new SchoolCodeListRow(1, "K7Q-M2P"),
            new SchoolCodeListRow(2, "B4X-T9R")
        };
        var bytes = pdf.Render(
            "Voorbeeld College",
            "2B",
            "2026–2027",
            rows,
            includeCutoutCards: true);

        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        // Source contract: name header constant is what the table renders (empty cells, no pupil names).
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure", "Scholen", "SchoolCodeListPdfService.cs"));
        Assert.Contains("SchoolCodeListPdfCopy.NameColumnHeader", src, StringComparison.Ordinal);
        Assert.Contains("SchoolCodeListPdfCopy.NoNamesFooter", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Leerlingnaam", src, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }
}
