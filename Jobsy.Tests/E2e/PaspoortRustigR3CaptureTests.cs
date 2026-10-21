using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class PaspoortRustigR3CaptureTests
{
    private const string ArtifactsDir = "/opt/cursor/artifacts";

    [Theory]
    [InlineData(1280, 900, false, "1280")]
    [InlineData(390, 844, true, "390")]
    [InlineData(412, 915, true, "412")]
    public async Task Capture_paspoort_rustig_r3(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_PASPOORT_R3") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        Directory.CreateDirectory(ArtifactsDir);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        await page.GotoAsync($"{baseUrl}/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        if (!await KbE2e.TryLoginAsync(page, baseUrl))
        {
            Assert.Fail($"Could not log in seed candidate at {baseUrl}.");
        }

        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000,
        });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 120_000 });
        if (!page.Url.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail($"Expected /candidate/paspoort but landed on {page.Url}");
        }

        await page.Locator(".passport-page.passport-page--rustig-r3").WaitForAsync(new()
        {
            State = WaitForSelectorState.Attached,
            Timeout = 90_000,
        });
        await page.Locator(".passport-page.passport-page--rustig-r3").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000,
        });
        await page.Locator("[data-testid=\"passport-rustig-hero-img\"]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000,
        });
        await page.WaitForTimeoutAsync(600);

        var heroHeight = await page.EvaluateAsync<double>(
            """
            () => {
              const h = document.querySelector('.cand-hero--paspoort-r3-rustig');
              return h ? h.getBoundingClientRect().height : 0;
            }
            """);
        if (mobile)
        {
            Assert.InRange(heroHeight, 88, 92);
        }
        else
        {
            Assert.InRange(heroHeight, 145, 155);
        }

        await page.Locator(".passport-tabs-shell").ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(300);

        var tabsTop = await page.EvaluateAsync<double>(
            """
            () => {
              const shell = document.querySelector('.passport-tabs-shell');
              return shell ? shell.getBoundingClientRect().top : -1;
            }
            """);
        await page.EvaluateAsync("() => window.scrollBy(0, 120)");
        await page.WaitForTimeoutAsync(400);
        var tabsTopAfter = await page.EvaluateAsync<double>(
            """
            () => {
              const shell = document.querySelector('.passport-tabs-shell');
              return shell ? shell.getBoundingClientRect().top : -1;
            }
            """);
        Assert.True(tabsTopAfter <= tabsTop + 2, "Sticky tabs shell should stay pinned while scrolling.");

        var blockingConsoleErrors = consoleErrors
            .Where(e => !e.Contains("ResizeObserver", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(blockingConsoleErrors);

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, $"paspoort-r3-product-{tag}.png"),
            FullPage = true,
        });
        if (tag is "1280" or "390")
        {
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, $"paspoort-r3-product-{tag}-viewport.png"),
                FullPage = false,
            });
        }
    }
}
