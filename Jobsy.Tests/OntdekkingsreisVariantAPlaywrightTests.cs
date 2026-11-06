using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Live-app screenshots of the discovery journey (variant A + calm rail).</summary>
[Collection("PlaywrightSmoke")]
public class OntdekkingsreisVariantAPlaywrightTests
{
    [Fact]
    public async Task Question_screen_screenshots_when_e2e_available()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var artifacts = Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")?.Trim();
        if (string.IsNullOrWhiteSpace(artifacts))
        {
            artifacts = "/opt/cursor/artifacts/screenshots";
        }

        Directory.CreateDirectory(artifacts);

        await CaptureStep8QuestionScreenshotsAsync(browser, baseUrl, artifacts);
        await CaptureCoastStep2DesktopAsync(browser, baseUrl, artifacts);
        await CaptureMobileEarlyStepsAsync(browser, baseUrl, artifacts);
    }

    private static async Task CaptureStep8QuestionScreenshotsAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts)
    {
        await CandidateJourneyE2e.PrepareDiscoveryQuestionScreenForE2eAsync(KbE2e.DefaultCompleteEmail, step: 8);

        foreach (var (width, height, name, mobile) in new[]
                 {
                     (1280, 800, "ontdekkingsreis-live-1280x800-stap8.png", false),
                     (1440, 900, "ontdekkingsreis-live-1440x900-stap8.png", false),
                     (390, 844, "ontdekkingsreis-live-390x844-stap8.png", true),
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await LoginAndOpenDiscoveryAsync(page, baseUrl, stap: 8);
            await WaitForQuestionScreenAsync(page);

            var likert = page.Locator(".journey-step .test-likert");
            await likert.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifacts, name),
                FullPage = false,
            });
        }
    }

    private static async Task CaptureCoastStep2DesktopAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await CandidateJourneyE2e.PrepareDiscoveryCoastStepForE2eAsync(KbE2e.DefaultCompleteEmail);

        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAndOpenDiscoveryAsync(page, baseUrl, stap: 2);
        await WaitForCoastStep2Async(page, mobile: false);

        Assert.Equal(0, await page.Locator("[data-lobsy-coach]").CountAsync());

        var topPath = Path.Combine(artifacts, "ontdekkingsreis-live-1280x800-stap2-top.png");
        await page.ScreenshotAsync(new() { Path = topPath, FullPage = false });

        var scrolled = await ScrollDiscoveryPageAsync(page, 600);
        Assert.True(scrolled >= 400, $"Expected page scroll ≥400px, got {scrolled}.");
        await page.WaitForTimeoutAsync(300);

        var midPath = Path.Combine(artifacts, "ontdekkingsreis-live-1280x800-stap2-midscroll.png");
        await page.ScreenshotAsync(new() { Path = midPath, FullPage = false });

        var topBytes = new FileInfo(topPath).Length;
        var midBytes = new FileInfo(midPath).Length;
        Assert.NotEqual(topBytes, midBytes);

        var railTitle = page.Locator("#journey-rail-title");
        await Assertions.Expect(railTitle).ToBeVisibleAsync();
        var titleBox = await railTitle.BoundingBoxAsync();
        Assert.NotNull(titleBox);
        Assert.True(titleBox.Y is >= 0 and <= 120, $"Rail should stay sticky near top (y={titleBox.Y}).");

        await using var wide = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(wide);
        var widePage = await wide.NewPageAsync();
        widePage.SetDefaultTimeout(90_000);
        await LoginAndOpenDiscoveryAsync(widePage, baseUrl, stap: 2);
        await WaitForCoastStep2Async(widePage, mobile: false);
        await widePage.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, "ontdekkingsreis-live-1440x900-stap2.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureMobileEarlyStepsAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await CaptureMobileStepScreenshotsAsync(browser, baseUrl, artifacts, step: 1, 390, 844,
        [
            ("ontdekkingsreis-live-390x844-stap1.png", false),
        ]);

        await CaptureMobileStepScreenshotsAsync(browser, baseUrl, artifacts, step: 2, 390, 844,
        [
            ("ontdekkingsreis-live-390x844-stap2-top.png", false),
            ("ontdekkingsreis-live-390x844-stap2-bottom.png", true),
        ]);

        await CaptureMobileStepScreenshotsAsync(browser, baseUrl, artifacts, step: 6, 390, 844,
        [
            ("ontdekkingsreis-live-390x844-stap6-top.png", false),
            ("ontdekkingsreis-live-390x844-stap6-bottom.png", true),
        ],
        extraBottomShot: ("ontdekkingsreis-live-360x780-stap6-bottom.png", 360, 780));
    }

    private static async Task CaptureMobileStepScreenshotsAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        int step,
        int width,
        int height,
        IReadOnlyList<(string FileName, bool ScrollBottom)> shots,
        (string FileName, int Width, int Height)? extraBottomShot = null)
    {
        await CandidateJourneyE2e.PrepareDiscoveryStepForE2eAsync(KbE2e.DefaultCompleteEmail, step);

        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await LoginAndOpenDiscoveryAsync(page, baseUrl, stap: step);
        await WaitForDiscoveryMobileStepAsync(page, step);

        Assert.Equal(0, await page.Locator("[data-lobsy-coach]").CountAsync());
        var mobileCoach = page.Locator(".journey-coach--mobile .journey-coach__tap");
        await Assertions.Expect(mobileCoach.First).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".journey-coach--mobile.is-bubble-open")).ToHaveCountAsync(0);

        var inlineCoach = page.Locator(".journey-coach-inline--card");
        await Assertions.Expect(inlineCoach.First).ToBeVisibleAsync();

        foreach (var (fileName, scrollBottom) in shots)
        {
            if (scrollBottom)
            {
                var scrolled = await ScrollJourneyStageToBottomAsync(page);
                Assert.True(scrolled > 40, $"Expected journey-stage scroll >40px, got {scrolled}.");
                await page.WaitForTimeoutAsync(300);
            }
            else
            {
                await page.EvaluateAsync(@"() => { const s = document.querySelector('.journey-stage'); if (s) s.scrollTop = 0; }");
                await page.WaitForTimeoutAsync(200);
            }

            if (step is 2 or 6)
            {
                await AssertMobileCoachDoesNotCoverChoicesAsync(page);
            }

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifacts, fileName),
                FullPage = false,
            });
        }

        if (extraBottomShot is { } alt)
        {
            await page.SetViewportSizeAsync(alt.Width, alt.Height);
            await page.WaitForTimeoutAsync(300);
            var scrolled = await ScrollJourneyStageToBottomAsync(page);
            Assert.True(scrolled > 40, $"Expected journey-stage scroll >40px at {alt.Width}x{alt.Height}, got {scrolled}.");
            if (step is 2 or 6)
            {
                await AssertMobileCoachDoesNotCoverChoicesAsync(page);
            }

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifacts, alt.FileName),
                FullPage = false,
            });
        }
    }

    private static async Task<double> ScrollJourneyStageToBottomAsync(IPage page)
    {
        return await page.EvaluateAsync<double>(
            @"() => {
                const stage = document.querySelector('.journey-stage');
                if (!stage) {
                    return 0;
                }
                stage.scrollTop = stage.scrollHeight;
                return stage.scrollTop;
            }");
    }

    private static async Task AssertMobileCoachDoesNotCoverChoicesAsync(IPage page)
    {
        var overlapDetail = await page.EvaluateAsync<string?>(
            @"() => {
                const coachTap = document.querySelector('.journey-coach--mobile .journey-coach__tap');
                if (!coachTap) {
                    return null;
                }
                const coachBox = coachTap.getBoundingClientRect();
                const bubble = document.querySelector('.journey-coach--mobile .journey-coach__bubble');
                const targets = document.querySelectorAll(
                    '.journey-step .journey-chip, .journey-step button.btn, .journey-step input, .journey-step textarea, .journey-step select, .journey-step .ob-wizard__preset');
                function overlaps(a, b) {
                    return !(a.right <= b.left || a.left >= b.right || a.bottom <= b.top || a.top >= b.bottom);
                }
                for (const el of targets) {
                    if (el.closest('.journey-coach-inline--card')) {
                        continue;
                    }
                    if (!el.offsetParent && el.tagName !== 'BODY') {
                        continue;
                    }
                    const style = window.getComputedStyle(el);
                    if (style.visibility === 'hidden' || style.display === 'none') {
                        continue;
                    }
                    const box = el.getBoundingClientRect();
                    if (box.width < 2 || box.height < 2) {
                        continue;
                    }
                    if (overlaps(coachBox, box)) {
                        return 'coach×' + (el.getAttribute('data-testid') || el.className || el.tagName);
                    }
                    if (bubble) {
                        const bubbleBox = bubble.getBoundingClientRect();
                        if (overlaps(bubbleBox, box)) {
                            return 'bubble×' + (el.getAttribute('data-testid') || el.className || el.tagName);
                        }
                    }
                }
                return null;
            }");

        Assert.True(string.IsNullOrEmpty(overlapDetail),
            $"Mobile coach must not overlap journey choices or inputs (overlap: {overlapDetail}).");
    }

    private static async Task<double> ScrollDiscoveryPageAsync(IPage page, int pixels)
    {
        return await page.EvaluateAsync<double>(
            @"pixels => {
                const root = document.documentElement;
                const body = document.body;
                const main = document.querySelector('.app-main');
                const stage = document.querySelector('.journey-stage');
                window.scrollTo(0, pixels);
                if (main && main.scrollHeight > main.clientHeight + 1) {
                    main.scrollTop = pixels;
                }
                if (stage && stage.scrollHeight > stage.clientHeight + 1) {
                    stage.scrollTop = pixels;
                }
                root.scrollTop = pixels;
                body.scrollTop = pixels;
                return Math.max(window.scrollY, root.scrollTop, body.scrollTop, main?.scrollTop ?? 0, stage?.scrollTop ?? 0);
            }",
            pixels);
    }

    private static async Task WaitForCoastStep2Async(IPage page, bool mobile)
    {
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        if (mobile)
        {
            await page.Locator(".journey-mprog").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            await page.Locator(".journey-rail__phase--open").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }

        await page.Locator(".journey-step h1").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(400);
    }

    private static async Task WaitForDiscoveryMobileStepAsync(IPage page, int step)
    {
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.Locator(".journey-mprog").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".journey-step h1").WaitForAsync(new() { Timeout = 60_000 });
        if (step is >= 7 and <= 10)
        {
            await page.Locator(".journey-coach-inline").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            await page.Locator(".journey-coach--mobile .journey-coach__tap").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await page.Locator(".journey-coach-inline--card").WaitForAsync(new() { Timeout = 60_000 });
        }

        await page.WaitForTimeoutAsync(400);
    }

    private static async Task LoginAndOpenDiscoveryAsync(IPage page, string baseUrl, int stap)
    {
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        if (await page.Locator("input[name=\"email\"]").CountAsync() > 0)
        {
            await page.FillAsync("input[name=\"email\"]", KbE2e.DefaultCompleteEmail);
            await page.FillAsync("input[name=\"password\"]", KbE2e.DefaultPassword);
            await page.ClickAsync("button.login-submit");
            try
            {
                await page.WaitForURLAsync(u => !u.ToString().Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 });
            }
            catch (TimeoutException)
            {
                await page.GotoAsync($"{baseUrl}/candidate/start", new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 90_000,
                });
            }
        }

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis?stap={stap}", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000,
        });
    }

    private static async Task WaitForQuestionScreenAsync(IPage page)
    {
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForSelectorAsync(".journey-step .test-flow--compact",
            new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(400);
    }
}
