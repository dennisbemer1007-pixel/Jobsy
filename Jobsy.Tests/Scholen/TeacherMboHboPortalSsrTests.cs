using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class TeacherMboHboPortalSsrTests
{
    private static readonly Guid ClassId = Guid.Parse("1d95bd92-3414-455f-b5f0-48e4cde3e4ee");

    [Fact]
    public void Group_page_shows_mbo_question_set_label()
    {
        var culture = new CultureState(null!, null!, null!);
        var group = new TeacherGroupInsightsDto(
            true,
            5,
            [],
            [new NamedCountDto("Zelfstandigheid (Autonomy)", 5)],
            [],
            [new NamedCountDto("Mensgericht", 5)],
            [],
            [],
            PupilQuestionSet.MboHbo);
        var html = TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, group, culture);
        Assert.Contains("Vragenlijst MBO/HBO (5 testen)", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Vragenlijst VO (100 vragen)", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Overview_hides_dream_jobs_block_for_mbo()
    {
        var culture = new CultureState(null!, null!, null!);
        var overview = new TeacherClassOverviewDto(
            ClassId,
            "Demo mbo",
            SchoolLevel.Mbo2,
            2,
            "2026-2027",
            5,
            5,
            0,
            0,
            100,
            30,
            TestWindowState.Open,
            null,
            true,
            null,
            [],
            new TeacherGroupInsightsDto(
                true,
                5,
                [],
                [new NamedCountDto("Verbinding (Connection)", 3)],
                [],
                [new NamedCountDto("Samenwerking (Collaboration)", 2)],
                [],
                [],
                PupilQuestionSet.MboHbo,
                0,
                5),
            PupilQuestionSet.MboHbo);
        var html = TeacherKlasOverviewSsrHtml.BuildBody(ClassId, overview, 0, [], null, culture);
        Assert.DoesNotContain("Leraar.Group.DreamJobs", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Nog niet ingevuld", html, StringComparison.Ordinal);
        Assert.Contains("Verbinding (Connection)", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Mbo_progress_table_uses_responsive_card_markup()
    {
        var culture = new CultureState(null!, null!, null!);
        var model = new TeacherMboHboClassProgressDto(
            ClassId,
            "Demo mbo",
            PupilQuestionSet.MboHbo,
            [
                new TeacherMboHboCodeRowDto(
                    Guid.NewGuid(),
                    "ABCD-1234",
                    [PupilMboHboTileStatus.Completed, PupilMboHboTileStatus.Completed, PupilMboHboTileStatus.Completed, PupilMboHboTileStatus.Completed, PupilMboHboTileStatus.Completed],
                    5,
                    true,
                    false)
            ],
            new TeacherMboHboGroupChartsDto([], []));
        var html = TeacherPortalReadOnlySsrHtml.BuildMboHboBody(model, culture);
        Assert.Contains("data-testid=\"mhbo-teacher-progress-table\"", html, StringComparison.Ordinal);
        Assert.Contains("data-label=\"Paspoort\"", html, StringComparison.Ordinal);
        Assert.Contains("mhbo-teacher-progress", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<table style=\"width:100%", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1440)]
    [InlineData(390)]
    public void Mbo_progress_css_supports_viewport(int width)
    {
        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web/wwwroot/css/features/scholen.css");
        var css = File.ReadAllText(cssPath);
        Assert.Contains(".mhbo-teacher-progress", css, StringComparison.Ordinal);
        if (width <= 400)
        {
            Assert.Contains(".sch-shell .mhbo-teacher-progress tr", css, StringComparison.Ordinal);
        }
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
