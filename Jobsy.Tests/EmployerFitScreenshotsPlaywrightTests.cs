using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real UI screenshots for employer-fit questionnaires (1280 + 390).
/// Run: JOBSY_E2E_EMPLOYER_FIT_SCREENSHOTS=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 ConnectionStrings__JobsyDb=...
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class EmployerFitScreenshotsPlaywrightTests
{
    private const string EnvRun = "JOBSY_E2E_EMPLOYER_FIT_SCREENSHOTS";

    private static readonly (int Width, int Height, string Tag)[] Viewports =
    [
        (1280, 900, "1280"),
        (390, 844, "390")
    ];

    private static string OutputDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT"))
            ? "/opt/cursor/artifacts/screenshots"
            : Path.Combine(
                Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")!,
                "screenshots");

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Capture_employer_fit_screenshots_at_1280_and_390()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        await EnableEmployerFitFlagsAsync(baseUrl);
        Directory.CreateDirectory(OutputDir);

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await CaptureViewportAsync(browser, baseUrl, width, height, tag);
        }

        foreach (var file in Directory.GetFiles(OutputDir, "employer-fit-*.png"))
        {
            AssertPngValid(file);
        }

        var expected = Viewports.Length * 5;
        var count = Directory.GetFiles(OutputDir, "employer-fit-*.png").Length;
        Assert.Equal(expected, count);
    }

    private static async Task EnableEmployerFitFlagsAsync(string baseUrl)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            Assert.Fail("ConnectionStrings__JobsyDb required for employer-fit screenshot run");
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = baseUrl.TrimEnd('/'),
                ["ConnectionStrings:JobsyDb"] = conn
            })
            .Build();

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var platform = new PlatformFeatureService(
            db,
            Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            config);

        await platform.UpdateAsync(new PlatformFeatureUpdate(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            PassportPdfV2Enabled: true,
            EmployerFitQuestionnairesEnabled: true,
            MatchCalibrationV1Enabled: true));

        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        for (var i = 0; i < 24; i++)
        {
            try
            {
                using var doc = JsonDocument.Parse(await http.GetStringAsync(api + "/api/settings/feature-flags"));
                var root = doc.RootElement;
                if (root.GetProperty("employersEnabled").GetBoolean()
                    && root.TryGetProperty("employerFitQuestionnairesEnabled", out var fit)
                    && fit.GetBoolean())
                {
                    await Task.Delay(12_000);
                    return;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(2_000);
        }

        Assert.Fail("Employer-fit feature flags did not become active on the API");
    }

    private static async Task CaptureViewportAsync(
        IBrowser browser,
        string baseUrl,
        int width,
        int height,
        string tag)
    {
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        // 1) Platform settings — EmployerFit flag visible and ON
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, "admin@jobsy.local", KbE2e.DefaultPassword));
        await page.GotoAsync(baseUrl + "/admin/instellingen", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);
        var fitRow = page.Locator(".admin-toggle-row").Filter(new()
        {
            HasTextRegex = new Regex("Werkgever fit-vragenlijsten|Employer fit questionnaires", RegexOptions.IgnoreCase)
        }).First;
        await fitRow.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await fitRow.ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(400);
        await ShotAsync(page, $"employer-fit-platform-settings-{tag}.png");

        // 2) Werkgever team culture T1–T6
        await page.Context.ClearCookiesAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, "ondernemer@jobsy.local", KbE2e.DefaultPassword));
        await page.GotoAsync(baseUrl + "/werkgever/organisatie/profiel?tab=cultuur", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await KbE2e.WaitForStyledAppAsync(page);
        await page.Locator("[data-testid=employer-fit-team-culture]").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid=employer-fit-team-culture]").ScrollIntoViewIfNeededAsync();
        await ShotAsync(page, tag == "1280" ? "employer-fit-team-culture-1280-v2.png" : $"employer-fit-team-culture-{tag}.png");

        // 3) Vacancy editor — werkcontext accordion open
        await page.GotoAsync(baseUrl + "/werkgever/vacatures/nieuw", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);
        var accordion = page.Locator("details.vacancy-match-optional");
        await accordion.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await accordion.EvaluateAsync("el => { el.open = true; }");
        await accordion.ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(500);
        await ShotAsync(page, tag == "1280" ? "employer-fit-vacancy-workcontext-1280-v2.png" : $"employer-fit-vacancy-workcontext-{tag}.png");

        // 4) Candidate passport — module E chips (shared section)
        await page.Context.ClearCookiesAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, KbE2e.DefaultPassword));
        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=data", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);
        var sharedBtn = page.Locator("#passport-data-btn-shared, [id='passport-data-btn-shared']");
        if (await sharedBtn.CountAsync() == 0)
        {
            sharedBtn = page.GetByRole(AriaRole.Button).Filter(new() { HasTextRegex = new Regex("werkgevers|shared|delen", RegexOptions.IgnoreCase) });
        }

        await sharedBtn.First.ClickAsync();
        await page.Locator("[data-testid=shared-with-employers]").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid=shared-with-employers]").ScrollIntoViewIfNeededAsync();
        await ShotAsync(page, tag == "390" ? "employer-fit-passport-module-e-390-v2.png" : $"employer-fit-passport-module-e-{tag}.png");

        // 5) Match explain — confidence / work context (desktop split list on banenkaart)
        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart + "?employerFitScreenshot=1", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await KbE2e.WaitForStyledAppAsync(page);
        await page.Locator("#discovery-match-explain-title").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000
        });
        var confidence = page.Locator(".match-confidence-hint, [data-testid=vacancy-match-indicative]").First;
        if (await confidence.CountAsync() > 0)
        {
            await confidence.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        }

        await ShotAsync(page, tag == "390" ? "employer-fit-match-explain-390-v2.png" : $"employer-fit-match-explain-{tag}.png");
    }

    private static async Task ShotAsync(IPage page, string fileName)
    {
        var path = Path.Combine(OutputDir, fileName);
        await page.ScreenshotAsync(new() { Path = path, FullPage = false });
    }

    private static void AssertPngValid(string path)
    {
        var info = new FileInfo(path);
        Assert.True(info.Length > 20_000, $"PNG too small (likely blank/login): {path} ({info.Length} bytes)");

        var py = new ProcessStartInfo
        {
            FileName = "python3",
            ArgumentList =
            {
                "-c",
                "import sys; from PIL import Image; Image.open(sys.argv[1]).verify()",
                path
            },
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(py);
        if (proc is not null)
        {
            proc.WaitForExit(30_000);
            if (proc.ExitCode == 0)
            {
                return;
            }
        }

        var fileOut = Process.Start(new ProcessStartInfo("file", path) { RedirectStandardOutput = true });
        Assert.NotNull(fileOut);
        fileOut.WaitForExit(5_000);
        var kind = fileOut.StandardOutput.ReadToEnd();
        Assert.Contains("PNG image data", kind, StringComparison.OrdinalIgnoreCase);
    }
}
