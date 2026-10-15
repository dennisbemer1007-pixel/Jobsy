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
        await CaptureCoastStep2ScreenshotsAsync(browser, baseUrl, artifacts);
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

    private static async Task CaptureCoastStep2ScreenshotsAsync(IBrowser browser, string baseUrl, string artifacts)
    {
        await CandidateJourneyE2e.PrepareDiscoveryCoastStepForE2eAsync(KbE2e.DefaultCompleteEmail);

        foreach (var (width, height, name, mobile) in new[]
                 {
                     (1280, 800, "ontdekkingsreis-live-1280x800-stap2-top.png", false),
                     (1280, 800, "ontdekkingsreis-live-1280x800-stap2-midscroll.png", false),
                     (1440, 900, "ontdekkingsreis-live-1440x900-stap2.png", false),
                     (390, 844, "ontdekkingsreis-live-390x844-stap2.png", true),
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

            await LoginAndOpenDiscoveryAsync(page, baseUrl, stap: 2);
            await WaitForCoastStep2Async(page, mobile);

            Assert.Equal(0, await page.Locator("[data-lobsy-coach]").CountAsync());

            if (name.Contains("midscroll", StringComparison.Ordinal))
            {
                await page.EvaluateAsync(
                    @"() => {
                        const main = document.querySelector('.app-main') || document.documentElement;
                        const max = Math.max(main.scrollHeight, document.documentElement.scrollHeight) - window.innerHeight;
                        window.scrollTo(0, Math.max(0, Math.floor(max / 2)));
                    }");
                await page.WaitForTimeoutAsync(350);
            }

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifacts, name),
                FullPage = false,
            });

            if (!mobile)
            {
                var railTitle = page.Locator("#journey-rail-title");
                var railSaved = page.Locator(".journey-rail__saved");
                await Assertions.Expect(railTitle).ToBeVisibleAsync();
                var titleBox = await railTitle.BoundingBoxAsync();
                Assert.NotNull(titleBox);
                Assert.True(titleBox.Y < 120, $"Rail title should start near top of viewport (y={titleBox.Y}).");

                if (await railSaved.CountAsync() > 0)
                {
                    var savedBox = await railSaved.BoundingBoxAsync();
                    Assert.NotNull(savedBox);
                    Assert.True(savedBox.Y + savedBox.Height <= height + 2,
                        "Rail footer should not be clipped at scroll position.");
                }
            }
        }
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

    private static async Task LoginAndOpenDiscoveryAsync(IPage page, string baseUrl, int stap)
    {
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        if (await page.Locator("input[name=\"email\"]").CountAsync() > 0)
        {
            await page.FillAsync("input[name=\"email\"]", KbE2e.DefaultCompleteEmail);
            await page.FillAsync("input[name=\"password\"]", KbE2e.DefaultPassword);
            await page.ClickAsync("button.login-submit");
            await page.WaitForURLAsync(u => !u.ToString().Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
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
