using Jobsy.Core.Enums;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class TestReflectionMirrorScreenshotPlaywrightTests
{
    private const string OutputDir = "/opt/cursor/artifacts/reflectie-r1";

    [Fact]
    public async Task Capture_mirror_preview_nl_and_en()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_REFLECTION") ?? "").Trim() != "1")
        {
            return;
        }

        Directory.CreateDirectory(OutputDir);
        var cssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web", "wwwroot", "css", "features", "testresultaten.css"));
        var css = File.Exists(cssPath) ? await File.ReadAllTextAsync(cssPath) : "";

        var snapshot = new TestReflectionScoreSnapshot(
            AssessmentKind.Competence,
            AssessmentTestVersions.V3,
            25,
            [
                new(CompetencyTestCatalog.Samenwerken, 73, "Samen"),
                new(CompetencyTestCatalog.Resultaatgerichtheid, 55, "Afmaken"),
                new(CompetencyTestCatalog.Stressbestendigheid, 48, "Kalm"),
                new(CompetencyTestCatalog.Innovatie, 41, "Nieuw"),
                new(CompetencyTestCatalog.Extraversie, 60, "Mensen")
            ],
            "dierenarts");

        foreach (var lang in new[] { "nl", "en" })
        {
            var mirror = TestReflectionTemplateBuilder.BuildFull(snapshot, lang == "en" ? "how you work" : "hoe je werkt", lang);
            var htmlPath = Path.Combine(OutputDir, $"preview-{lang}.html");
            await File.WriteAllTextAsync(htmlPath, BuildPreviewHtml(mirror, css, lang));
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (w, h, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1024, 900, "1024"),
                     (390, 844, "390")
                 })
        {
            foreach (var lang in new[] { "nl", "en" })
            {
                var htmlPath = Path.Combine(OutputDir, $"preview-{lang}.html");
                await using var ctx = await browser.NewContextAsync(new()
                {
                    ViewportSize = new() { Width = w, Height = h }
                });
                var page = await ctx.NewPageAsync();
                await page.GotoAsync($"file://{htmlPath}", new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.WaitForTimeoutAsync(400);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(OutputDir, $"mirror-{lang}-{suffix}.png"),
                    FullPage = true
                });
            }
        }
    }

    private static string BuildPreviewHtml(TestReflectionFull mirror, string css, string lang)
    {
        var themeLabel = lang == "en" ? "Theme" : "Thema";
        var themes = string.Join("", mirror.Themes.Select((t, i) =>
            "<article class=\"test-mirror__theme test-mirror__theme--" + i + "\"><small>" + themeLabel
            + "</small><h3>" + Esc(t.Title) + "</h3><p>" + Esc(t.Summary) + "</p></article>"));
        var practice = string.Join("", mirror.PracticeExamples.Select(p => "<li>" + Esc(p) + "</li>"));
        var watch = string.Join("", mirror.WatchPoints.Select(p => "<li>" + Esc(p) + "</li>"));
        var gives = string.Join("", mirror.EnergyGives.Select(p => "<li>" + Esc(p) + "</li>"));
        var drains = string.Join("", mirror.EnergyDrains.Select(p => "<li>" + Esc(p) + "</li>"));
        var dream = string.IsNullOrWhiteSpace(mirror.DreamCallout)
            ? ""
            : "<p class=\"test-mirror__dream\">" + Esc(mirror.DreamCallout) + "</p>";
        var givesLabel = lang == "en" ? "Gives energy" : "Geeft energie";
        var drainsLabel = lang == "en" ? "Drains energy" : "Kost energie";
        var bridgeSmall = lang == "en" ? "One layer deeper" : "Nog een laag dieper";
        var bridgeTitle = lang == "en" ? "Want to know what is underneath?" : "Wil je weten wat hier nog onder zit?";

        return "<!doctype html><html lang=\"" + lang + "\"><head><meta charset=\"utf-8\"><style>"
               + ":root { --brand:#0f2d5c; --muted:#5a6a7d; --border:#e3d9cd; --surface:#fffcfa; }"
               + "body { margin:0; font-family: system-ui, sans-serif; background:#f7f1e8; color:#122033; }"
               + ".panel-page { max-width: 1100px; margin: 0 auto; padding: 1rem; }"
               + css
               + "</style></head><body><div class=\"panel-page test-detail-page\">"
               + "<section class=\"test-mirror\"><div class=\"test-mirror__hero\"><div class=\"test-mirror__hero-inner\">"
               + "<span class=\"test-mirror__badge\"><span>●</span> " + Esc(mirror.BadgeLabel) + "</span>"
               + "<h2 class=\"test-mirror__title\">" + Esc(mirror.HeroTitle) + "</h2>"
               + "<p class=\"test-mirror__subtitle\">" + Esc(mirror.HeroSubtitle) + "</p></div></div>"
               + "<div class=\"test-mirror__themes\">" + themes + "</div>"
               + "<div class=\"test-mirror__grid\"><div class=\"test-mirror__col\">"
               + "<article class=\"test-mirror__card\"><small>" + Esc(mirror.PracticeTitle) + "</small><h3>" + Esc(mirror.PracticeTitle) + "</h3>"
               + "<ul class=\"test-mirror__list test-mirror__list--ok\">" + practice + "</ul></article>"
               + "<article class=\"test-mirror__card test-mirror__card--watch\"><small>" + Esc(mirror.WatchTitle) + "</small><h3>" + Esc(mirror.WatchTitle) + "</h3>"
               + "<ul class=\"test-mirror__list test-mirror__list--watch\">" + watch + "</ul></article></div>"
               + "<div class=\"test-mirror__col\"><article class=\"test-mirror__card\"><div class=\"test-mirror__energy\">"
               + "<div><h4>" + givesLabel + "</h4><ul>" + gives + "</ul></div>"
               + "<div class=\"test-mirror__energy--drain\"><h4>" + drainsLabel + "</h4><ul>" + drains + "</ul></div></div></article>"
               + "<article class=\"test-mirror__card\"><h3>" + Esc(mirror.EnvironmentTitle) + "</h3><p>" + Esc(mirror.EnvironmentBody) + "</p>" + dream + "</article>"
               + "</div></div></section>"
               + "<section class=\"test-mirror-bridge\"><div class=\"test-mirror-bridge__copy\"><small>" + bridgeSmall + "</small>"
               + "<h3>" + bridgeTitle + "</h3></div></section>"
               + "</div></body></html>";
    }

    private static string Esc(string? s)
        => System.Net.WebUtility.HtmlEncode(s ?? "");
}
