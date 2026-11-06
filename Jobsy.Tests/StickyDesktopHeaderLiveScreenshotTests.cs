using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Live local-stack screenshots for sticky desktop app header (PR evidence).</summary>
[Collection("PlaywrightSmoke")]
public class StickyDesktopHeaderLiveScreenshotTests
{
    private const int ScrollPx = 600;

    [Fact]
    public async Task Capture_live_sticky_header_screenshots_when_e2e_available()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        var artifacts = Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")?.Trim();
        if (string.IsNullOrWhiteSpace(artifacts))
        {
            artifacts = "/cursor/stores/self/live";
        }

        Directory.CreateDirectory(artifacts);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);

        await CapturePaspoortDesktopAsync(browser, baseUrl, artifacts);
        await CaptureVacaturesDesktopAsync(browser, baseUrl, artifacts);
        await CaptureProfileDesktopAsync(browser, baseUrl, artifacts);
        await CaptureAccountDropdownDesktopAsync(browser, baseUrl, artifacts);
        await CaptureBanenkaartDesktopAsync(browser, baseUrl, artifacts);
        await CaptureJourneyDesktopAsync(browser, baseUrl, artifacts);
        await CapturePaspoortMobileAsync(browser, baseUrl, artifacts);
    }

    [Fact]
    public async Task Capture_journey_and_mobile_paspoort_when_e2e_available()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        var artifacts = Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")?.Trim();
        if (string.IsNullOrWhiteSpace(artifacts))
        {
            artifacts = "/cursor/stores/self/live";
        }

        Directory.CreateDirectory(artifacts);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureJourneyDesktopAsync(browser, baseUrl, artifacts);
        await CapturePaspoortMobileAsync(browser, baseUrl, artifacts);
    }

    private static async Task CapturePaspoortDesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=overview", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForRealHeaderAsync(page);
        await page.Locator(".passport-tabs").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".passport-overview, .passport-page").First.WaitForAsync(new() { Timeout = 60_000 });

        await ScrollMainAsync(page, ScrollPx);
        await AssertHeaderStickyAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-desktop-paspoort-1440x900-scrolled.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureVacaturesDesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/vacancies", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForRealHeaderAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.Locator(".vacancy-list, .job-card, .apps-page, .panel-page").First.WaitForAsync(new() { Timeout = 60_000 });
        await ScrollMainAsync(page, ScrollPx);
        await page.Locator(".job-card").Last.ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("() => window.scrollBy(0, -100)");
        await page.WaitForTimeoutAsync(250);

        await AssertHeaderStickyAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-desktop-vacatures-1440x900-scrolled.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureProfileDesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/profile", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForRealHeaderAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.Locator(".profile-page, h1").First.WaitForAsync(new() { Timeout = 60_000 });

        await ScrollMainAsync(page, ScrollPx);
        await AssertHeaderStickyAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-desktop-profile-1440x900-scrolled.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureAccountDropdownDesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForRealHeaderAsync(page);
        await ScrollMainAsync(page, ScrollPx);
        await AssertHeaderStickyAsync(page);

        var toggle = page.Locator(".account-menu__toggle").First;
        await toggle.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await toggle.ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-header-menu') === 'account'",
            null,
            new() { Timeout = 10_000 });
        await page.Locator(".account-menu__panel").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await page.WaitForTimeoutAsync(250);

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-desktop-account-menu-1440x900-scrolled.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureBanenkaartDesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}{E2eRoutes.Banenkaart}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForRealHeaderAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 120_000 });
        await page.WaitForSelectorAsync(
            "[data-testid=kb-view-toggle], .kb-list-rows, .jobsy-discovery, .map-stage, #job-map",
            new() { Timeout = 120_000, State = WaitForSelectorState.Visible });

        await ScrollMainAsync(page, Math.Min(ScrollPx, 200));
        await AssertHeaderStickyAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-desktop-banenkaart-1440x900.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureJourneyDesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await CandidateJourneyE2e.PrepareDiscoveryCoastStepForE2eAsync(KbE2e.DefaultCompleteEmail);

        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis?stap=2", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.Locator(".journey-rail").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".journey-rail__phase--open").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".journey-step h1").WaitForAsync(new() { Timeout = 60_000 });

        var railTitle = page.Locator("#journey-rail-title, .journey-rail h2").First;
        await railTitle.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var railBoxBefore = await railTitle.BoundingBoxAsync();
        Assert.NotNull(railBoxBefore);

        await page.EvaluateAsync(
            """
            () => {
                const step = document.querySelector('.journey-step');
                const fields = document.querySelectorAll('.journey-step input, .journey-step textarea, .journey-step .ob-wizard__content');
                const last = fields[fields.length - 1];
                last?.scrollIntoView({ block: 'end' });
                if (step && step.scrollHeight > step.clientHeight + 8) {
                    step.scrollTop = Math.min(step.scrollHeight, 520);
                }
            }
            """);
        await page.WaitForTimeoutAsync(350);

        var railBox = await railTitle.BoundingBoxAsync();
        Assert.NotNull(railBox);
        Assert.True(railBox.Y is >= 8 and <= 160, $"Rail should stay visible while step scrolls (y={railBox.Y}).");

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-desktop-journey-stap2-1440x900-scrolled.png"),
            FullPage = false,
        });
    }

    private static async Task CapturePaspoortMobileAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await using var context = await browser.NewContextAsync(KbE2e.MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=overview", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".passport-tabs").WaitForAsync(new() { Timeout = 60_000 });
        await ScrollMainAsync(page, ScrollPx);
        await page.WaitForTimeoutAsync(300);

        var tabsBox = await page.Locator(".passport-tabs").BoundingBoxAsync();
        Assert.NotNull(tabsBox);
        Assert.True(tabsBox.Y >= 400 || tabsBox.Y <= 120,
            $"Mobile passport tabs expected fixed bottom or top band (y={tabsBox.Y}).");

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "live-mobile-paspoort-390x844-scrolled.png"),
            FullPage = false,
        });
    }

    private static async Task LoginAsync(IPage page, string baseUrl)
    {
        if (!await KbE2e.TryLoginAsync(page, baseUrl))
        {
            await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.FillAsync("input[name=\"email\"]", KbE2e.DefaultCompleteEmail);
            await page.FillAsync("input[name=\"password\"]", KbE2e.DefaultPassword);
            await page.ClickAsync("button.login-submit");
            await page.WaitForURLAsync(u => !u.ToString().Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
        }

        await KbE2e.WaitForStyledAppAsync(page, "app.min.css");
    }

    private static async Task WaitForRealHeaderAsync(IPage page)
    {
        await page.Locator(".app-header").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".app-header .brand__logo, .app-header .lobsy-logo, .app-header img.brand__logo").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await page.Locator(".account-menu__toggle, .notification-bell__toggle").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    }

    private static async Task ScrollMainAsync(IPage page, int pixels)
    {
        for (var i = 0; i < 6; i++)
        {
            await page.Mouse.WheelAsync(0, 650);
            await page.WaitForTimeoutAsync(120);
        }

        var scrolled = await page.EvaluateAsync<double>(
            """
            pixels => {
                window.scrollTo(0, Math.max(window.scrollY, pixels));
                const scrollables = [
                    document.querySelector('.app-main'),
                    document.querySelector('.passport-layout__main'),
                    document.querySelector('.passport-page'),
                    document.querySelector('.profile-page'),
                    document.querySelector('.vacancy-list'),
                    document.querySelector('.apps-page'),
                ].filter(Boolean);
                for (const el of scrollables) {
                    if (el.scrollHeight > el.clientHeight + 8) {
                        el.scrollTop = Math.max(el.scrollTop, pixels);
                    }
                }
                const deep = document.querySelector(
                    '.job-card:nth-child(3), .application-card:nth-child(2), .passport-data, .profile-page section, .passport-bubble');
                deep?.scrollIntoView({ block: 'start' });
                return Math.max(window.scrollY, document.documentElement.scrollTop, document.body.scrollTop);
            }
            """,
            pixels);

        if (scrolled < 200)
        {
            await page.Keyboard.PressAsync("End");
            await page.WaitForTimeoutAsync(200);
            scrolled = await page.EvaluateAsync<double>("() => window.scrollY");
        }

        Assert.True(scrolled >= 200 || pixels <= 250,
            $"Expected scroll under sticky header, document scrollY={scrolled}.");
        await page.WaitForTimeoutAsync(350);
    }

    private static async Task AssertHeaderStickyAsync(IPage page)
    {
        var header = page.Locator(".app-header").First;
        var box = await header.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.Y is >= -2 and <= 4, $"Header should stick to top (y={box.Y}).");
        var position = await header.EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("sticky", position);
    }
}
