using Jobsy.Core.Scholen;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78JobWhyLayoutScreenshotTests
{
    [Fact]
    public async Task Capture_all_example_jobs_styled_at_390_and_1440()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        var root = RepoRoot();
        Directory.CreateDirectory("/opt/cursor/artifacts");

        var scenarios = new[]
        {
            ("fotograaf", "Fotograaf", "Kunst en media", PupilG78JobMatchExplanationTestsFixtures.ArtisticResult(),
                PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
                    ["tekenen"],
                    """{"9018":5,"9029":5,"9021":5,"9004":5,"9006":4,"9007":3,"9002":3,"9003":3}""")),
            ("hovenier", "Hovenier", "Natuur en buiten", PupilG78JobMatchExplanationTestsFixtures.TechResult(),
                PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
                    ["bouwen"],
                    """{"9016":5,"9028":5,"9002":5,"9012":1,"9011":1,"9003":2,"9008":1,"9001":3}""")),
            ("bioloog", "Bioloog", "Dieren en leven", PupilG78JobMatchExplanationTestsFixtures.ArtisticResult(),
                PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
                    ["gamen"],
                    """{"9017":5,"9025":5,"9021":4,"9009":5,"9002":4,"9007":3}""")),
        };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (key, title, domain, result, progress) in scenarios)
        {
            var why = PupilG78JobMatchExplanation.BuildHtml(key, result, progress);
            foreach (var (width, suffix) in new[] { (390, "390"), (1440, "1440") })
            {
                var cardW = Math.Min(width, 520);
                var body = G78JobWhyScreenshotHarness.BuildPaspoortJobCardHtml(title, domain, why, cardW);
                var html = G78JobWhyScreenshotHarness.FullHtmlDocument(root, body);
                var page = await browser.NewPageAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = 960 },
                    DeviceScaleFactor = 2
                });
                await page.SetViewportSizeAsync(width, 960);
                await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });
                await AssertStyledPanelAsync(page);
                await page.ScreenshotAsync(new()
                {
                    Path = $"/opt/cursor/artifacts/g78-paspoort-job-why-{key}-{suffix}.png",
                    FullPage = true
                });
                await page.CloseAsync();
            }
        }

        // Legacy names (hovenier) for earlier review threads
        File.Copy("/opt/cursor/artifacts/g78-paspoort-job-why-hovenier-390.png",
            "/opt/cursor/artifacts/g78-paspoort-job-why-390.png", overwrite: true);
        File.Copy("/opt/cursor/artifacts/g78-paspoort-job-why-hovenier-1440.png",
            "/opt/cursor/artifacts/g78-paspoort-job-why-1440.png", overwrite: true);
    }

    [Fact]
    public async Task Capture_pdf_style_job_cards_for_three_examples()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var items = new[]
        {
            ("fotograaf", "Fotograaf", "Kunst en media"),
            ("hovenier", "Hovenier", "Natuur en buiten"),
            ("bioloog", "Bioloog", "Dieren en leven"),
        };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new()
        {
            ViewportSize = new() { Width = 800, Height = 500 },
            DeviceScaleFactor = 2
        });
        var cards = new List<string>();
        foreach (var (key, title, domain) in items)
        {
            var progress = key switch
            {
                "fotograaf" => PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(["tekenen"], """{"9018":5,"9021":5,"9004":5}"""),
                "hovenier" => PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(["bouwen"], """{"9016":5,"9002":5,"9003":2}"""),
                _ => PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(["gamen"], """{"9017":5,"9009":5}""")
            };
            var result = key == "hovenier"
                ? PupilG78JobMatchExplanationTestsFixtures.TechResult()
                : PupilG78JobMatchExplanationTestsFixtures.ArtisticResult();
            var why = System.Net.WebUtility.HtmlEncode(PupilG78JobMatchExplanation.Build(key, result, progress));
            cards.Add($$"""
                <div style="flex:1;max-width:200px;background:#faf6e8;border:1px solid #e2c27a;border-radius:8px;padding:12px;font-family:Lexend,'Segoe UI',sans-serif;font-size:9px;color:#14213a;line-height:1.35">
                <div style="text-align:center;font-size:9px;color:#0f5b66;margin-bottom:4px">{{domain}}</div>
                <div style="text-align:center;font-family:Fraunces,Georgia,serif;font-weight:700;font-size:11px;color:#10284a">{{title}}</div>
                <p style="margin:8px 0 0;color:#46546a;white-space:pre-wrap">{{why}}</p>
                </div>
                """);
        }

        var html = $"""
            <!DOCTYPE html><html lang="nl"><head>
            <link href="https://fonts.googleapis.com/css2?family=Fraunces:wght@700&family=Lexend:wght@400;600&display=swap" rel="stylesheet" />
            </head><body style="margin:0;background:#f7f4f0;padding:20px;font-family:Lexend,sans-serif">
            <div style="display:flex;gap:12px;align-items:flex-start">{string.Join("", cards)}</div>
            </body></html>
            """;
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.ScreenshotAsync(new()
        {
            Path = "/opt/cursor/artifacts/g78-pdf-job-card-why.png",
            FullPage = true
        });
    }

    private static async Task AssertStyledPanelAsync(IPage page)
    {
        var titleFont = await page.Locator(".g78-job-why-panel__title").EvaluateAsync<string>(
            "el => getComputedStyle(el).fontFamily");
        Assert.Contains("Fraunces", titleFont, StringComparison.OrdinalIgnoreCase);

        var sectionColor = await page.Locator(".g78-job-why-panel__section-title").First.EvaluateAsync<string>(
            "el => getComputedStyle(el).color");
        Assert.Contains("16", sectionColor, StringComparison.Ordinal);
        Assert.Contains("40", sectionColor, StringComparison.Ordinal);

        var rowBg = await page.Locator(".g78-job-why-panel__comp-row").First.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");
        Assert.False(string.Equals(rowBg, "rgba(0, 0, 0, 0)", StringComparison.Ordinal));
        Assert.False(string.Equals(rowBg, "transparent", StringComparison.OrdinalIgnoreCase));

        var listStyle = await page.Locator(".g78-job-why-panel__comp-list").EvaluateAsync<string>(
            "el => getComputedStyle(el).listStyleType");
        Assert.Equal("none", listStyle);

        var bodyFont = await page.Locator(".lp-g78").EvaluateAsync<string>(
            "el => getComputedStyle(el).fontFamily");
        Assert.Contains("Lexend", bodyFont, StringComparison.OrdinalIgnoreCase);

        var panelMaxHeight = await page.Locator(".g78-job-why-panel.is-open").EvaluateAsync<string>(
            "el => getComputedStyle(el).maxHeight");
        Assert.Equal("none", panelMaxHeight);

        var toggleLabel = await page.Locator(".g78-job-why-btn").InnerTextAsync();
        Assert.Equal("×", toggleLabel.Trim());

        var lastSection = page.Locator(".g78-job-why-panel__section--last");
        Assert.True(await lastSection.IsVisibleAsync());
        var box = await lastSection.BoundingBoxAsync();
        Assert.NotNull(box);
        var pageHeight = await page.EvaluateAsync<float>("() => document.documentElement.scrollHeight");
        Assert.True(box!.Y + box.Height <= pageHeight + 2, "Answer section should not be clipped by panel");
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
