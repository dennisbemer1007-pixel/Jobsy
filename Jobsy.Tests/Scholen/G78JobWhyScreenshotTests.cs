using Jobsy.Core.Scholen;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78JobWhyScreenshotTests
{
    [Fact]
    public async Task Capture_passport_job_card_with_why_panel_open()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        var root = RepoRoot();
        var cssPath = Path.Combine(root, "Jobsy.Web/wwwroot/css/features/leerling-werelden-kies.css");
        var jsPath = Path.Combine(root, "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js");
        var domSafe = Path.Combine(root, "Jobsy.Web/wwwroot/js/jobsy-dom-safe.js");

        var artistic = PupilG78JobMatchExplanationTestsFixtures.ArtisticResult();
        var progress = PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(
            ["tekenen"],
            """{"9018":5}""");
        var why = PupilG78JobMatchExplanation.Build("hovenier", PupilG78JobMatchExplanationTestsFixtures.TechResult(),
            PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(["techniek"], """{"9016":5,"9028":5}"""));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        foreach (var (width, suffix) in new[] { (390, "390"), (1440, "1440") })
        {
            var page = await browser.NewPageAsync(new()
            {
                ViewportSize = new() { Width = width, Height = 900 },
                DeviceScaleFactor = 2
            });
            var maxW = Math.Min(width, 520);
            var html = $$"""
                <!DOCTYPE html><html lang="nl"><head>
                <link rel="stylesheet" href="file://{{cssPath}}" />
                <style>body { margin:24px;background:#f5f2ee;max-width:{{maxW}}px; }</style>
                </head><body class="ll-g78-shell lp-g78">
                <h2 style="font-family:system-ui;color:#0f2d5c">Beroepen om eens te bekijken</h2>
                <article class="lp-g78__card lp-g78__job">
                <div class="g78-job-why-wrap">
                <button type="button" class="ll-info-btn g78-job-why-btn" aria-label="Waarom past dit bij jou?" aria-expanded="true" aria-controls="g78-job-why-hovenier">i</button>
                <div id="g78-job-why-hovenier" class="ll-info g78-job-why-panel is-open">
                <button type="button" class="ll-info__close" aria-label="Sluiten">×</button>
                <p class="g78-job-why-panel__title"><b>Waarom past dit bij jou?</b></p>
                <p class="g78-job-why-panel__body">{{System.Net.WebUtility.HtmlEncode(why)}}</p>
                </div></div>
                <div class="lp-g78__job-k">Natuur en buiten</div>
                <h4>Hovenier</h4>
                <p>Je ontwerpt en maakt tuinen en parken. Je bent bijna de hele dag buiten.</p>
                </article>
                </body></html>
                """;
            await page.SetContentAsync(html);
            await page.AddScriptTagAsync(new() { Path = domSafe });
            await page.AddScriptTagAsync(new() { Path = jsPath });
            Directory.CreateDirectory("/opt/cursor/artifacts");
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/g78-paspoort-job-why-{suffix}.png",
                FullPage = true
            });
            _ = artistic;
            _ = progress;
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Capture_pdf_style_job_row_mock()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var why = PupilG78JobMatchExplanation.Build("bioloog",
            PupilG78JobMatchExplanationTestsFixtures.ArtisticResult(),
            PupilG78JobMatchExplanationTestsFixtures.ProgressWithLikes(["gamen"], """{"9025":5}"""));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 400 },
            DeviceScaleFactor = 2
        });
        var html = $"""
            <!DOCTYPE html><html lang="nl"><body style="font-family:Segoe UI,sans-serif;background:#f7f4f0;padding:20px">
            <h3 style="color:#0f2d5c;margin:0 0 12px">Beroepen op je paspoort (PDF)</h3>
            <div style="display:flex;gap:8px">
            <div style="flex:1;background:#faf6e8;border:1px solid #ddd5cc;border-radius:8px;padding:12px;font-size:11px;color:#122033">
            <strong>Bioloog</strong><br/>
            <span style="color:#5a6a7d;font-size:10px">Dieren en leven</span>
            <p style="margin:8px 0 0;line-height:1.35;color:#5a6a7d;font-size:9px">{System.Net.WebUtility.HtmlEncode(why)}</p>
            </div>
            </div>
            </body></html>
            """;
        await page.SetContentAsync(html);
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await page.ScreenshotAsync(new()
        {
            Path = "/opt/cursor/artifacts/g78-pdf-jobs-why-1440.png",
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

internal static class PupilG78JobMatchExplanationTestsFixtures
{
    public static Jobsy.Core.Entities.Scholen.PupilResult ArtisticResult() => new()
    {
        HollandCode = "AI",
        RiasecScoresJson =
            """{"realistic":40,"investigative":55,"artistic":90,"social":50,"enterprising":35,"conventional":30}""",
    };

    public static Jobsy.Core.Entities.Scholen.PupilResult TechResult() => new()
    {
        HollandCode = "RI",
        RiasecScoresJson =
            """{"realistic":88,"investigative":70,"artistic":30,"social":45,"enterprising":40,"conventional":35}""",
    };

    public static Jobsy.Core.Entities.Scholen.PupilProgress ProgressWithLikes(IReadOnlyList<string> likes, string answersJson)
        => new()
        {
            LikesJson = System.Text.Json.JsonSerializer.Serialize(likes),
            VakScoresJson = """{"tekenen":{"leuk":5,"goed":5},"natuur-techniek":{"leuk":5,"goed":5}}""",
            AnswersJson = answersJson
        };
}
