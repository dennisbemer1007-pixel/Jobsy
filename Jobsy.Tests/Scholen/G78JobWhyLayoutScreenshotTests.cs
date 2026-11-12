using Jobsy.Core.Scholen;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78JobWhyLayoutScreenshotTests
{
    [Fact]
    public async Task Capture_before_after_panel_layout()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        var root = RepoRoot();
        var cssPath = Path.Combine(root, "Jobsy.Web/wwwroot/css/features/leerling-werelden-kies.css");
        var progress = PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
            ["bouwen"],
            """{"9016":5,"9028":5,"9002":5,"9007":4,"9003":2,"9008":1}""");
        var why = PupilG78JobMatchExplanation.BuildHtml("hovenier",
            PupilG78JobMatchExplanationTestsFixtures.TechResult(), progress);

        var beforeBody = """
            <p class="g78-job-why-panel__section-title"><b>Wat vraagt dit werk?</b></p>
            <p class="g78-job-why-panel__section-body">• Netjes en precies werken Je antwoorden laten zien dat je hier sterk in bent.</p>
            <p class="g78-job-why-panel__section-body">• kalm blijven als het druk is daar scoorde je lager.</p>
            """;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory("/opt/cursor/artifacts");

        foreach (var (width, suffix) in new[] { (390, "390"), (768, "768"), (1440, "1440") })
        {
            var page = await browser.NewPageAsync(new()
            {
                ViewportSize = new() { Width = width, Height = 900 },
                DeviceScaleFactor = 2
            });
            var maxW = Math.Min(width, 520);

            await page.SetContentAsync(await PanelHtml(cssPath, maxW, beforeBody, cramped: true));
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/g78-why-panel-before-{suffix}.png",
                FullPage = true
            });

            await page.SetContentAsync(await PanelHtml(cssPath, maxW, why, cramped: false));
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/g78-why-panel-after-{suffix}.png",
                FullPage = true
            });
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Capture_pdf_style_job_card_with_why()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var progress = PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
            ["bouwen"],
            """{"9016":5,"9028":5,"9002":5,"9007":5,"9003":2,"9008":2}""");
        var why = PupilG78JobMatchExplanation.Build("hovenier",
            PupilG78JobMatchExplanationTestsFixtures.TechResult(), progress);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new()
        {
            ViewportSize = new() { Width = 595, Height = 420 },
            DeviceScaleFactor = 2
        });
        var html = $$"""
            <!DOCTYPE html><html lang="nl"><body style="margin:0;background:#f7f4f0;font-family:'Segoe UI',system-ui,sans-serif">
            <div style="padding:16px;max-width:180px;background:#faf6e8;border:1px solid #ddd5cc;border-radius:4px;color:#122033">
            <div style="text-align:center;font-size:10px;color:#5a6a7d;margin-bottom:4px">Natuur en buiten</div>
            <div style="text-align:center;font-weight:600;font-size:11px">Hovenier</div>
            <p style="font-size:7px;line-height:1.25;color:#5a6a7d;margin:8px 0 0;white-space:pre-wrap">{{System.Net.WebUtility.HtmlEncode(why)}}</p>
            </div></body></html>
            """;
        await page.SetContentAsync(html);
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await page.ScreenshotAsync(new()
        {
            Path = "/opt/cursor/artifacts/g78-pdf-job-card-why.png",
            FullPage = true
        });
    }

    private static Task<string> PanelHtml(string cssPath, int maxW, string bodyHtml, bool cramped)
    {
        var crampedStyle = cramped
            ? "line-height:1.2;font-size:11px;padding:6px;"
            : "";
        var html = $$"""
            <!DOCTYPE html><html lang="nl"><head>
            <link rel="stylesheet" href="file://{{cssPath}}" />
            <style>body{margin:20px;background:#f5f2ee;max-width:{{maxW}}px;font-family:system-ui}</style>
            </head><body class="ll-g78-shell lp-g78">
            <article class="lp-g78__card lp-g78__job" style="padding:12px">
            <div class="g78-job-why-wrap">
            <div class="ll-info g78-job-why-panel is-open" style="{{crampedStyle}}">
            <p class="g78-job-why-panel__title"><b>Meer info over dit beroep</b></p>
            <div class="g78-job-why-panel__body">{{bodyHtml}}</div>
            </div></div>
            <h4 style="margin:8px 0 4px;color:#10284a">Hovenier</h4>
            </article></body></html>
            """;
        return Task.FromResult(html);
    }

    private static string RepoRoot()
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
