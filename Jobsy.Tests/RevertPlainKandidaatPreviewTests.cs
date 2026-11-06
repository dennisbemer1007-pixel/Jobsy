using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// JOBSY_REVERT_PLAIN_CAPTURE=1 dotnet test --filter Revert_plain_kandidaat_preview_capture
/// Writes side-by-side baseline (9055770f) vs revert screenshots under JOBSY_REVERT_PLAIN_ARTIFACT_DIR.
/// </summary>
public sealed class RevertPlainKandidaatPreviewTests : IAsyncDisposable
{
    private static readonly (string Slug, string Path, bool CandidateAuth)[] Pages =
    [
        ("landing", "/", false),
        ("login", "/login", false),
        ("account-maken", "/account-maken", false),
        ("wachtwoord-vergeten", "/wachtwoord-vergeten", false),
        ("paspoort", "/candidate/paspoort", true),
        ("banenkaart-gast", "/banenkaart", false),
        ("banenkaart-ingelogd", "/banenkaart", true),
        ("sollicitaties", "/candidate/applications", true),
    ];

    private static readonly (int Width, int Height, string Tag)[] Viewports =
    [
        (1440, 900, "1440"),
        (390, 844, "390"),
    ];

    private IllustratedPagesWebFactory? _web;

    public async ValueTask DisposeAsync()
    {
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Fact]
    public async Task Revert_plain_kandidaat_preview_capture()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_REVERT_PLAIN_CAPTURE") != "1")
        {
            return;
        }

        _web = new IllustratedPagesWebFactory();
        var baseUrl = _web.ServerAddress;
        var artifactRoot = Environment.GetEnvironmentVariable("JOBSY_REVERT_PLAIN_ARTIFACT_DIR")
            ?? "/opt/cursor/artifacts/revert-plain-kandidaat";
        Directory.CreateDirectory(artifactRoot);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (slug, path, needsAuth) in Pages)
        {
            foreach (var (width, height, tag) in Viewports)
            {
                await CaptureAsync(browser, baseUrl, artifactRoot, slug, path, needsAuth, width, height, tag, "after-revert");
            }
        }
    }

    private static async Task CaptureAsync(
        IBrowser browser,
        string baseUrl,
        string artifactRoot,
        string slug,
        string path,
        bool needsAuth,
        int width,
        int height,
        string tag,
        string phase)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        if (needsAuth)
        {
            await page.GotoAsync(
                $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(path)}",
                new() { Timeout = 60_000, WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForURLAsync($"**{path}**", new() { Timeout = 60_000 });
        }
        else
        {
            await page.GotoAsync($"{baseUrl}{path}", new() { Timeout = 60_000, WaitUntil = WaitUntilState.NetworkIdle });
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(800);

        var outPath = Path.Combine(artifactRoot, phase, $"{slug}-{tag}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        await page.ScreenshotAsync(new() { Path = outPath, FullPage = true });
    }

    [Fact]
    public async Task Revert_plain_kandidaat_baseline_9055770f_capture()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_REVERT_PLAIN_BASELINE_CAPTURE") != "1")
        {
            return;
        }

        _web = new IllustratedPagesWebFactory();
        var baseUrl = _web.ServerAddress;
        var artifactRoot = Environment.GetEnvironmentVariable("JOBSY_REVERT_PLAIN_ARTIFACT_DIR")
            ?? "/opt/cursor/artifacts/revert-plain-kandidaat";

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (slug, path, needsAuth) in Pages)
        {
            foreach (var (width, height, tag) in Viewports)
            {
                await CaptureAsync(browser, baseUrl, artifactRoot, slug, path, needsAuth, width, height, tag, "baseline-9055770f");
            }
        }
    }
}
