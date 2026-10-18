using Microsoft.Playwright;
using System.Diagnostics;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class StijlAScreenshotMatrixTests : IAsyncLifetime
{
    private static string OutRoot
        => Environment.GetEnvironmentVariable("STIJL_A_OUT") ?? "/opt/cursor/artifacts/stijl-A-r4";

    private static string CaptureSubdir
        => Environment.GetEnvironmentVariable("STIJL_A_SUBDIR") ?? "after";

    private static bool LooseBaseline
        => string.Equals(Environment.GetEnvironmentVariable("STIJL_A_LOOSE"), "1", StringComparison.Ordinal)
           || string.Equals(CaptureSubdir, "before", StringComparison.OrdinalIgnoreCase);

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Capture_stijl_a_matrix_with_illustrated_stubs()
    {
        var sub = CaptureSubdir.Trim('/');
        var root = Path.Combine(OutRoot, sub);
        Directory.CreateDirectory(root);

        var shots = new (string Slug, string Route, int W, int H, string? Role, string WaitSelector, int ExtraMs)[]
        {
            ("start-390x844", "/", 390, 844, null, "[data-testid=landing-hero-passport], .pub-landing", 2400),
            ("carriere-1440x900", "/carriere", 1440, 900, "candidate", ".career-page .career-card:not(.career-card--skeleton)", 2200),
            ("carriere-390x844", "/carriere", 390, 844, "candidate", ".career-page .career-card:not(.career-card--skeleton)", 2200),
            ("paspoort-390x844", "/candidate/paspoort", 390, 844, "candidate", ".passport-layout", 2200),
            ("sollicitaties-390x844", "/candidate/applications", 390, 844, "candidate",
                ".kb-apps .application-card, .kb-apps__layout", 2200),
            ($"vacature-390x844", $"/vacancies/{IllustratedPagesApiStub.SampleVacancyId:D}", 390, 844, "candidate",
                "[data-testid=kb-detail]", 2600),
            ("werkgever-binnenkort-1440x900", "/werkgevers/binnenkort", 1440, 900, "employer", "#wg-soon-title, .pub-section", 2000),
            ("werkgever-vacature-390x844", "/werkgever/vacatures/nieuw", 390, 844, "employer", ".vac-wizard-field, [data-testid=vacancy-wizard]", 2600),
            ("werkgever-sollicitaties-1440x900", "/werkgever/sollicitaties", 1440, 900, "employer",
                "[data-testid=wg-app-pipeline]", 2800),
        };

        foreach (var shot in shots)
        {
            await CaptureAsync(root, shot.Slug, shot.Route, shot.W, shot.H, shot.Role, shot.WaitSelector, shot.ExtraMs);
        }

        var pngs = Directory.GetFiles(root, "*.png");
        Assert.True(pngs.Length >= shots.Length, $"Expected {shots.Length} PNGs under {root}, got {pngs.Length}.");
    }

    [Fact]
    public async Task Build_stijl_a_pairs_when_before_and_after_present()
    {
        var beforeDir = Path.Combine(OutRoot, "before");
        var afterDir = Path.Combine(OutRoot, "after");
        if (!Directory.Exists(beforeDir) || !Directory.Exists(afterDir))
        {
            return;
        }

        var pairPy = LocatePairScript();
        var pairsDir = Path.Combine(OutRoot, "pairs");
        Directory.CreateDirectory(pairsDir);

        foreach (var after in Directory.GetFiles(afterDir, "*.png"))
        {
            var name = Path.GetFileName(after);
            var before = Path.Combine(beforeDir, name);
            if (!File.Exists(before))
            {
                continue;
            }

            var pairOut = Path.Combine(pairsDir, name);
            var psi = new ProcessStartInfo
            {
                FileName = "python3",
                ArgumentList = { pairPy, before, after, pairOut },
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            Assert.NotNull(proc);
            await proc.WaitForExitAsync();
            Assert.Equal(0, proc.ExitCode);
        }
    }

    private async Task CaptureAsync(
        string root,
        string slug,
        string route,
        int w,
        int h,
        string? role,
        string waitSelector,
        int extraMs)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            Locale = "nl-NL",
            IsMobile = w < 500,
            HasTouch = w < 500
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (role is not null)
        {
            var signIn =
                $"{_web!.ServerAddress}/__test/sign-in?role={Uri.EscapeDataString(role)}&return={Uri.EscapeDataString(route)}";
            await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        await page.GotoAsync($"{_web!.ServerAddress}{route}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForFunctionAsync(
            """
            () => {
              const busy = document.querySelector(
                '.page-content-skeleton, .career-card--skeleton, .detail-card--pending, [aria-busy="true"].career-card--skeleton');
              return !busy;
            }
            """,
            null,
            new() { Timeout = 90_000 });
        await page.AddStyleTagAsync(new()
        {
            Content = """
                .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                """
        });

        if (LooseBaseline)
        {
            await page.Locator("body").WaitForAsync(new() { Timeout = 30_000 });
            await page.WaitForTimeoutAsync(1200);
        }
        else
        {
            foreach (var sel in waitSelector.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                await page.Locator(sel).First.WaitForAsync(new() { Timeout = 90_000 });
            }
        }

        if (!LooseBaseline)
        {
            var error = page.Locator(".state-message.error, .career-card .state-message.error");
            if (await error.CountAsync() > 0)
            {
                var text = await error.First.InnerTextAsync();
                Assert.Fail($"{slug}: error state visible: {text}");
            }
        }

        if (!LooseBaseline && route.Contains("/paspoort", StringComparison.OrdinalIgnoreCase))
        {
            var skeleton = page.Locator(".passport-page .page-content-skeleton, .passport-page [aria-busy=true]");
            Assert.Equal(0, await skeleton.CountAsync());
            var body = await page.Locator(".passport-page").InnerTextAsync();
            Assert.Contains("Sanne", body, StringComparison.OrdinalIgnoreCase);
        }

        await page.WaitForTimeoutAsync(extraMs);

        var file = Path.Combine(root, $"{slug}.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = false });
        Assert.True(File.Exists(file));
    }

    private static string LocatePairScript()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "screenshots", "stijl-a-pair.py");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("tools/screenshots/stijl-a-pair.py not found.");
    }
}
