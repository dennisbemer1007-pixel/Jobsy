using Jobsy.Core.Scholen;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78JobWhyLayoutScreenshotTests
{
    [Fact]
    public async Task Capture_paspoort_panel_with_real_stylesheets()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        var root = RepoRoot();
        var wwwroot = Path.Combine(root, "Jobsy.Web/wwwroot");

        var progress = PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
            ["bouwen"],
            """{"9016":5,"9028":5,"9001":3,"9006":3,"9002":5,"9007":3,"9003":2,"9008":1,"9004":3,"9009":3,"9005":3,"9010":3}""");
        var why = PupilG78JobMatchExplanation.BuildHtml("hovenier",
            PupilG78JobMatchExplanationTestsFixtures.TechResult(), progress);

        var sheetHrefs = PageAssetCatalog.StylesheetsForPupilSsr("/leerling/paspoort")
            .Where(s => s.Contains("leerling-werelden-kies", StringComparison.Ordinal)
                        || s.Contains("leerling-paspoort-g78", StringComparison.Ordinal))
            .Select(s => s.Split('?')[0])
            .ToList();
        var linkTags = string.Join("\n", sheetHrefs.Select(h =>
            $"""<link rel="stylesheet" href="https://jobsy.test{h}" />"""));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 900 },
            DeviceScaleFactor = 2
        });

        await context.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            var path = uri.AbsolutePath.TrimStart('/');
            if (path.StartsWith("css/", StringComparison.Ordinal))
            {
                var file = Path.Combine(wwwroot, path.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(file))
                {
                    await route.FulfillAsync(new() { Path = file });
                    return;
                }
            }

            await route.ContinueAsync();
        });

        Directory.CreateDirectory("/opt/cursor/artifacts");

        foreach (var (width, suffix) in new[] { (390, "390"), (1440, "1440") })
        {
            var page = await context.NewPageAsync();
            await page.SetViewportSizeAsync(width, 860);
            var html = $$"""
                <!DOCTYPE html><html lang="nl"><head>
                <link rel="preconnect" href="https://fonts.googleapis.com" />
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
                <link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,700&family=Lexend:wght@400;600&display=swap" rel="stylesheet" />
                {{linkTags}}
                </head>
                <body class="ll-g78-shell">
                <div class="lp-g78" style="max-width:{{Math.Min(width, 520)}}px;margin:16px auto">
                <article class="lp-g78__card lp-g78__job" style="position:relative;padding:12px 14px">
                <div class="g78-job-why-wrap">
                <div class="ll-info g78-job-why-panel is-open">
                <button type="button" class="ll-info__close" aria-label="Sluiten">×</button>
                <p class="g78-job-why-panel__title"><b>Hovenier</b></p>
                <div class="g78-job-why-panel__body">{{why}}</div>
                </div></div>
                <div class="lp-g78__job-k">Natuur en buiten</div>
                <h4>Hovenier</h4>
                </article></div></body></html>
                """;
            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });
            var headingColor = await page.Locator(".g78-job-why-panel__section-title").First
                .EvaluateAsync<string>("el => getComputedStyle(el).color");
            Assert.Contains("16", headingColor, StringComparison.Ordinal); // navy rgb ~16,40,74
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/g78-paspoort-job-why-{suffix}.png",
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
            <!DOCTYPE html><html lang="nl"><body style="margin:0;background:#f7f4f0;font-family:Lexend,'Segoe UI',sans-serif">
            <div style="padding:16px;max-width:180px;background:#faf6e8;border:1px solid #ddd5cc;border-radius:4px;color:#122033">
            <div style="text-align:center;font-size:10px;color:#5a6a7d;margin-bottom:4px">Natuur en buiten</div>
            <div style="text-align:center;font-weight:600;font-size:11px">Hovenier</div>
            <p style="font-size:7px;line-height:1.25;color:#5a6a7d;margin:8px 0 0;white-space:pre-wrap">{{System.Net.WebUtility.HtmlEncode(why)}}</p>
            </div>
            </body></html>
            """;
        await page.SetContentAsync(html);
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await page.ScreenshotAsync(new()
        {
            Path = "/opt/cursor/artifacts/g78-pdf-job-card-why.png",
            FullPage = true
        });
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
