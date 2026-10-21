using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Captures Acceptatie evidence for illustrated Paspoort mobile sticky tabs (390×844 viewport).
/// Does not fail the build on known pre-fix Acc gap — records metrics + screenshot for PRs.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class PaspoortStickyTabsAccDiagnosticPlaywrightTests
{
    private const string AccUrl = "https://acceptatie.lobsy.nl";
    private const string Email = "onboarding.e2e@jobsy.local";
    private const string Password = "Jobsy123!";

    [Fact]
    public async Task Capture_acceptatie_paspoort_sticky_tabs_at_390()
    {
        if (!await KbE2e.IsReachableAsync(AccUrl))
        {
            return;
        }

        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "passport-mobile-tabs-acc");
        Directory.CreateDirectory(outDir);
        var artifactDir = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(artifactDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync(AccUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.FillAsync("input[name='email']", Email);
        await page.FillAsync("input[name='password']", Password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });

        await page.GotoAsync(AccUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await page.Locator(".passport-page--illustrated .passport-tabs-shell").WaitForAsync(new() { Timeout = 90_000 });

        await page.EvaluateAsync("""
            () => {
              const shell = document.querySelector('.passport-page--illustrated .passport-tabs-shell');
              if (shell) shell.scrollIntoView({ block: 'start' });
              window.scrollBy(0, 480);
            }
            """);
        await page.WaitForTimeoutAsync(500);

        for (var pass = 0; pass < 3; pass++)
        {
            await page.EvaluateAsync("""
                () => {
                  window.scrollBy(0, 320);
                }
                """);
            await page.WaitForTimeoutAsync(200);
        }

        var metricsJson = await page.EvaluateAsync<string>("""
            () => {
              const shell = document.querySelector('.passport-page--illustrated .passport-tabs-shell');
              const header = document.querySelector('.app-header');
              if (!shell) return JSON.stringify({ error: 'no-shell' });
              const cs = getComputedStyle(shell);
              const box = shell.getBoundingClientRect();
              const headerBox = header ? header.getBoundingClientRect() : null;
              let contentAbove = false;
              const probeY = Math.min(Math.max(box.top * 0.5, 2), box.top - 2);
              if (probeY > 0) {
                const hit = document.elementFromPoint(Math.min(window.innerWidth * 0.5, 200), probeY);
                if (hit && !shell.contains(hit)) {
                  const hb = hit.getBoundingClientRect();
                  if (hb.height > 6 && hb.width > 6 && hb.bottom > box.top + 1) {
                    contentAbove = true;
                  }
                }
              }
              const link = [...document.querySelectorAll('link[rel=stylesheet]')]
                .map(l => l.href)
                .find(h => /kompas-illustrated/i.test(h)) || '';
              return JSON.stringify({
                scrollY: window.scrollY,
                top: box.top,
                stickyTop: parseFloat(cs.top) || 0,
                position: cs.position,
                headerBottom: headerBox ? headerBox.bottom : null,
                contentAbove,
                cssVersion: link
              });
            }
            """);

        var shotPath = Path.Combine(outDir, "paspoort-sticky-tabs-acc-390-stuck.png");
        await page.ScreenshotAsync(new() { Path = shotPath, FullPage = false });
        File.Copy(shotPath, Path.Combine(artifactDir, "paspoort-sticky-tabs-acc-390-stuck.png"), overwrite: true);

        await File.WriteAllTextAsync(Path.Combine(outDir, "metrics.txt"), metricsJson + "\n");
    }

    private static string FindRepoRoot()
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
