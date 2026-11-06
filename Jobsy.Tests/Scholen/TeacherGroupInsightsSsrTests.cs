using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class TeacherGroupInsightsSsrTests
{
    private static readonly Guid ClassId = Guid.Parse("1d95bd92-3414-455f-b5f0-48e4cde3e4ee");

    [Fact]
    public void Group_page_renders_chart_sections_with_aria_labels()
    {
        var culture = new CultureState(null!, null!, null!);
        var group = SampleGroep78Group();
        var html = TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, group, culture);

        Assert.Contains("data-testid=\"leraar-group-insights\"", html, StringComparison.Ordinal);
        Assert.Contains("sch-group-bars", html, StringComparison.Ordinal);
        Assert.Contains("sch-chart-donut__wrap", html, StringComparison.Ordinal);
        Assert.Contains("sch-chart-columns__svg", html, StringComparison.Ordinal);
        Assert.Contains("sch-group-discussion", html, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Mbo_group_page_has_no_dream_jobs_block()
    {
        var culture = new CultureState(null!, null!, null!);
        var group = new TeacherGroupInsightsDto(
            true,
            5,
            [new RiasecBarDto("S", "Leraar.Riasec.S", 4), new RiasecBarDto("R", "Leraar.Riasec.R", 2)],
            [new NamedCountDto("Betekenis (Impact)", 2)],
            [],
            [new NamedCountDto("Samenwerking (Collaboration)", 2)],
            [new NamedCountDto("Laag", 2), new NamedCountDto("Hoog", 3)],
            ["Welke richting past bij jullie interesses?"],
            PupilQuestionSet.MboHbo);
        var html = TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, group, culture);

        Assert.DoesNotContain("sch-chart-columns__svg", html, StringComparison.Ordinal);
        Assert.Contains("sch-group-discussion", html, StringComparison.Ordinal);
        Assert.Contains("Helpen (S)", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Hidden_group_shows_threshold_message_without_charts()
    {
        var culture = new CultureState(null!, null!, null!);
        var group = new TeacherGroupInsightsDto(
            false,
            2,
            [],
            [],
            [],
            [],
            [],
            [],
            PupilQuestionSet.Groep78);
        var html = TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, group, culture);

        Assert.Contains("Zichtbaar vanaf 5", html, StringComparison.Ordinal);
        Assert.DoesNotContain("leraar-group-insights", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Vertical_columns_empty_state_is_text_only()
    {
        var svg = TeacherOverviewChartSvg.VerticalColumns([], "Geen data");
        Assert.Contains("Geen data", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<rect", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Dump_preview_html_when_env_set()
    {
        var dir = Environment.GetEnvironmentVariable("JOBSY_DUMP_GROUP_HTML");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        var culture = new CultureState(null!, null!, null!);
        var g78 = SampleGroep78Group();
        var mbo = new TeacherGroupInsightsDto(
            true,
            5,
            [
                new RiasecBarDto("S", "Leraar.Riasec.S", 4),
                new RiasecBarDto("R", "Leraar.Riasec.R", 3),
                new RiasecBarDto("I", "Leraar.Riasec.I", 3)
            ],
            [new NamedCountDto("Betekenis (Impact)", 2), new NamedCountDto("Prestatie (Achievement)", 1)],
            [],
            [new NamedCountDto("Samenwerking (Collaboration)", 2), new NamedCountDto("Flexibiliteit (Flexibility)", 1)],
            [new NamedCountDto("Laag", 2), new NamedCountDto("Midden", 2), new NamedCountDto("Hoog", 1)],
            ["Welke richting past bij Helpen én Maken?"],
            PupilQuestionSet.MboHbo);

        WritePreview(Path.Combine(dir, "groep-g78.html"), TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, g78, culture));
        WritePreview(Path.Combine(dir, "groep-mbo.html"), TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, mbo, culture));
    }

    private static void WritePreview(string path, string body)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var cssPath = Path.Combine(root, "Jobsy.Web/wwwroot/css/features/scholen.css");
        var css = File.ReadAllText(cssPath);
        var html = "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"/><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>"
            + "<style>" + css + "body{background:#f4f6f9;margin:0;padding:24px;}.sch-shell{max-width:1100px;margin:0 auto;}</style></head><body><div class=\"sch-shell\">"
            + body
            + "</div></body></html>";
        File.WriteAllText(path, html);
    }

    private static TeacherGroupInsightsDto SampleGroep78Group()
        => new(
            true,
            6,
            [
                new RiasecBarDto("S", "Leraar.Riasec.S", 4),
                new RiasecBarDto("R", "Leraar.Riasec.R", 3),
                new RiasecBarDto("E", "Leraar.Riasec.E", 1)
            ],
            [
                new NamedCountDto("Impact", 2),
                new NamedCountDto("Achievement", 1)
            ],
            [new NamedCountDto("kok", 3), new NamedCountDto("dierenarts", 2)],
            [new NamedCountDto("Collaboration", 2), new NamedCountDto("Flexibility", 1)],
            [new NamedCountDto("Laag", 2), new NamedCountDto("Midden", 2), new NamedCountDto("Hoog", 2)],
            ["Welke richting past bij Helpen én Maken?", "Wat betekent betekenis voor jullie?"],
            PupilQuestionSet.Groep78,
            1,
            0);
}
