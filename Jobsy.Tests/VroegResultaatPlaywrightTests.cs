using System.Text.RegularExpressions;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class VroegResultaatPlaywrightTests
{
    [Fact]
    public async Task Skip_tests_later_opens_banenkaart_and_capture_when_requested()
    {
        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_VROEG") ?? "").Trim() == "1";
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        var afterDir = "/cursor/stores/self/vroeg-resultaat-impl/after";
        if (capture)
        {
            Directory.CreateDirectory(afterDir);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureSkipBanenkaartAsync(browser, baseUrl, afterDir, capture, mobile2x: true, width: 390, height: 844, scale: 2, fileName: "skip-banenkaart-mobile-390@2x.png");
        await CaptureSkipBanenkaartAsync(browser, baseUrl, afterDir, capture, mobile2x: false, width: 1440, height: 900, scale: 1, fileName: "skip-banenkaart-desktop-1440.png");
    }

    private static async Task CaptureSkipBanenkaartAsync(
        IBrowser browser,
        string baseUrl,
        string afterDir,
        bool capture,
        bool mobile2x,
        int width,
        int height,
        int scale,
        string fileName)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = scale,
            IsMobile = mobile2x || width < 500,
            HasTouch = width < 500,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterFreshCandidateAsync(page, baseUrl);
        await EnsureWizardStartedAsync(page);
        await FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-nearby-panel-mobile]").WaitForAsync(new() { Timeout = 60_000 });
        Assert.Equal(0, await page.Locator("[data-testid=early-reward-nearby]").CountAsync());
        await NavigateToTestConsentWithoutStartingAsync(page);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1500);

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "skip-consent-before-click.png") });
        }

        await page.GetByTestId("onboarding-tests-later").First.ClickAsync(new() { Force = true });
        await page.WaitForURLAsync(u => u.ToString().Contains("/banenkaart", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });
        await page.Locator(".jobsy-discovery").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await page.Locator("#jobsy-map-boot").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(2500);
        Assert.Contains("/banenkaart", page.Url, StringComparison.OrdinalIgnoreCase);
        await AssertCandidateShellOnBanenkaartAsync(page, mobile2x || width < 500);

        var captureBlockers = capture || (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BLOCKERS") ?? "").Trim() == "1";
        if (captureBlockers)
        {
            Directory.CreateDirectory(afterDir);
            await page.ScreenshotAsync(new() { Path = Path.Combine(afterDir, fileName), FullPage = false });
        }
    }

    private static async Task AssertCandidateShellOnBanenkaartAsync(IPage page, bool expectMobileBottomNav)
    {
        await page.Locator(".app-header").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await page.Locator(".app-header .brand__logo, .app-header .lobsy-logo, .app-header img.brand__logo").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        if (expectMobileBottomNav)
        {
            await page.Locator("nav.bottom-nav").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }

        await page.Locator(".jobsy-discovery").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 30_000 });
        var openForWork = page.Locator("[data-testid=open-for-work-prompt]");
        if (await openForWork.CountAsync() > 0)
        {
            await openForWork.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        }
    }

    [Fact]
    public async Task Early_reward_screens_layout_and_capture_when_requested()
    {
        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_VROEG") ?? "").Trim() == "1";
        var captureZero = capture || (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_VROEG_ZERO") ?? "").Trim() == "1";
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        var afterDir = "/cursor/stores/self/vroeg-resultaat-impl/after";
        var compareDir = "/cursor/stores/self/vroeg-resultaat-impl/compare";
        if (capture)
        {
            Directory.CreateDirectory(afterDir);
            Directory.CreateDirectory(compareDir);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await using var mobile = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            DeviceScaleFactor = 1,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile);
        var page = await mobile.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterFreshCandidateAsync(page, baseUrl);
        await WaitForWizardShellAsync(page);

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "welcome-mobile-390-after.png") });
            File.WriteAllText(
                Path.Combine(afterDir, "welcome-intro-before.txt"),
                "In ± 6 minuten maken we samen je startprofiel. Daarna zie je in je Kompas wie je bent en welk werk bij je past.");
        }

        await EnsureWizardStartedAsync(page);
        await FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-nearby-panel-mobile]").WaitForAsync(new() { Timeout = 60_000 });
        Assert.Equal(0, await page.Locator("[data-testid=early-reward-nearby]").CountAsync());

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "A-nearby-mobile-390.png") });
        }

        await SkipOptionalStepsToConsentAsync(page);
        await FinishCompetencyMiniAsync(page);
        await AssertNoEarlyInsightInterstitialAsync(page);
        await page.Locator(".test-likert").WaitForAsync(new() { Timeout = 90_000 });
        await AssertEarlyScreenChromeAsync(page);

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "B-after-competency-mini-mobile-390.png") });
        }

        if (capture)
        {
            await using var mobileB2x = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 390, Height = 844 },
                DeviceScaleFactor = 2,
                IsMobile = true,
                HasTouch = true,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(mobileB2x);
            var bPage2x = await mobileB2x.NewPageAsync();
            bPage2x.SetDefaultTimeout(120_000);
            await RegisterFreshCandidateAsync(bPage2x, baseUrl);
            await EnsureWizardStartedAsync(bPage2x);
            await FillStepsThroughTransportAsync(bPage2x, maxTravelMinutes: 30);
            await ClickNextAsync(bPage2x);
            await SkipOptionalStepsToConsentAsync(bPage2x);
            await FinishCompetencyMiniAsync(bPage2x);
            await AssertNoEarlyInsightInterstitialAsync(bPage2x);
            await bPage2x.Locator(".test-likert").WaitForAsync(new() { Timeout = 90_000 });
            await bPage2x.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "B-after-competency-mini-mobile-390@2x.png") });
        }

        await mobile.CloseAsync();

        if (captureZero)
        {
            await CaptureZeroNearbyFallbackAsync(browser, baseUrl, afterDir);
        }

        if (capture)
        {
            await CaptureExtraViewportShotsAsync(browser, baseUrl, afterDir, compareDir);
        }
    }

    private static async Task CaptureZeroNearbyFallbackAsync(
        IBrowser browser,
        string baseUrl,
        string afterDir)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await RegisterFreshCandidateAsync(page, baseUrl);
        await EnsureWizardStartedAsync(page);
        await FillStepsThroughTransportAsync(page, maxTravelMinutes: 5, postcode: "8899EH");
        await ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-nearby-panel-mobile]").WaitForAsync(new() { Timeout = 60_000 });
        await page.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "A-nearby-zero-mobile-390@2x.png") });
    }

    private static async Task CaptureExtraViewportShotsAsync(
        IBrowser browser,
        string baseUrl,
        string afterDir,
        string compareDir)
    {
        await using var mobile2x = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile2x);
        var page2x = await mobile2x.NewPageAsync();
        page2x.SetDefaultTimeout(120_000);
        await RegisterFreshCandidateAsync(page2x, baseUrl);
        await EnsureWizardStartedAsync(page2x);
        await FillStepsThroughTransportAsync(page2x, maxTravelMinutes: 30);
        await ClickNextAsync(page2x);
        await page2x.Locator("[data-testid=onboarding-nearby-panel-mobile]").WaitForAsync(new() { Timeout = 60_000 });
        await page2x.Locator(".ob-nearby-panel__count, .ob-nearby-panel__mobile-text").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await page2x.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "A-nearby-mobile-390@2x.png") });
        await NavigateToTestConsentWithoutStartingAsync(page2x);
        await page2x.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).WaitForAsync(new() { Timeout = 60_000 });
        await page2x.WaitForTimeoutAsync(800);
        await page2x.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "skip-consent-mobile-390@2x.png") });

        await using var desktop = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(desktop);
        var dPage = await desktop.NewPageAsync();
        dPage.SetDefaultTimeout(120_000);
        await RegisterFreshCandidateAsync(dPage, baseUrl);
        await EnsureWizardStartedAsync(dPage);
        await FillStepsThroughTransportAsync(dPage, maxTravelMinutes: 30);
        await ClickNextAsync(dPage);
        await ClickNextAsync(dPage);
        await NavigateToTestConsentWithoutStartingAsync(dPage);
        await dPage.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).WaitForAsync(new() { Timeout = 60_000 });
        await dPage.WaitForTimeoutAsync(800);
        await dPage.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "skip-consent-desktop-1440.png") });
        await SkipOptionalStepsToConsentAsync(dPage);
        await FinishCompetencyMiniAsync(dPage);
        await AssertNoEarlyInsightInterstitialAsync(dPage);
        await dPage.Locator(".test-likert").WaitForAsync(new() { Timeout = 90_000 });
        await dPage.ScreenshotAsync(new() { Path = Path.Combine(afterDir, "B-after-competency-mini-desktop-1440.png") });

        CopyMockupCompares(compareDir, afterDir);
    }

    private static void CopyMockupCompares(string compareDir, string afterDir)
    {
        var mockRoot = ResolveMockupRoot();
        var mockA = Path.Combine(mockRoot, "overview-mobile-variants.png");
        var mockAMobile = Path.Combine(mockRoot, "A", "mobile-390x844.png");
        var mockBMobile = Path.Combine(mockRoot, "B", "mobile-390x844.png");
        var mockB = Path.Combine(mockRoot, "B", "desktop-1440x900.png");

        foreach (var (src, name) in new (string, string)[]
                 {
                     (mockA, "mockup-overview-mobile.png"),
                     (mockAMobile, "mockup-A-mobile-390.png"),
                     (mockBMobile, "mockup-B-mobile-390.png"),
                     (mockB, "mockup-B-desktop-1440.png"),
                 })
        {
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(compareDir, name), overwrite: true);
            }
        }

        TrySideBySide(
            Path.Combine(afterDir, "A-nearby-mobile-390.png"),
            mockAMobile,
            Path.Combine(compareDir, "compare-A-nearby-vs-mockup.png"));
        TrySideBySide(
            Path.Combine(afterDir, "B-insight-mobile-390.png"),
            mockBMobile,
            Path.Combine(compareDir, "compare-B-insight-vs-mockup.png"));
        TrySideBySide(
            Path.Combine(afterDir, "B-insight-desktop-1440.png"),
            mockB,
            Path.Combine(compareDir, "compare-B-desktop-vs-mockup.png"));
    }

    private static string ResolveMockupRoot()
    {
        const string checkedOut = "/workspace/docs/mockups/vroeg-resultaat";
        if (Directory.Exists(checkedOut))
        {
            return checkedOut;
        }

        var temp = Path.Combine(Path.GetTempPath(), "jobsy-vroeg-mockups");
        Directory.CreateDirectory(temp);
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var git = Directory.Exists(Path.Combine(repoRoot, ".git")) ? repoRoot : "/workspace";
        foreach (var rel in new[]
                 {
                     "docs/mockups/vroeg-resultaat/overview-mobile-variants.png",
                     "docs/mockups/vroeg-resultaat/A/mobile-390x844.png",
                     "docs/mockups/vroeg-resultaat/B/mobile-390x844.png",
                     "docs/mockups/vroeg-resultaat/B/desktop-1440x900.png",
                 })
        {
            var dest = Path.Combine(temp, rel.Replace("docs/mockups/vroeg-resultaat/", "", StringComparison.Ordinal));
            if (File.Exists(dest))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    WorkingDirectory = git,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                psi.ArgumentList.Add("show");
                psi.ArgumentList.Add($"origin/docs/vroeg-resultaat:{rel}");
                using var p = System.Diagnostics.Process.Start(psi);
                if (p is null)
                {
                    continue;
                }

                p.WaitForExit(30_000);
                if (p.ExitCode != 0)
                {
                    continue;
                }

                var data = p.StandardOutput.BaseStream;
                using var ms = new MemoryStream();
                data.CopyTo(ms);
                if (ms.Length > 0)
                {
                    File.WriteAllBytes(dest, ms.ToArray());
                }
            }
            catch
            {
                // Best-effort mockup extraction for compare/ artifacts.
            }
        }

        return temp;
    }

    private static void TrySideBySide(string leftPath, string rightPath, string outPath)
    {
        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                ArgumentList =
                {
                    "-c",
                    """
import sys
from PIL import Image
left, right, out = sys.argv[1:4]
a = Image.open(left).convert('RGB')
b = Image.open(right).convert('RGB')
h = max(a.height, b.height)

def pad(im):
    if im.height == h:
        return im
    canvas = Image.new('RGB', (im.width, h), (245, 245, 245))
    canvas.paste(im, (0, 0))
    return canvas

a, b = pad(a), pad(b)
combo = Image.new('RGB', (a.width + b.width + 8, h), (32, 32, 32))
combo.paste(a, (0, 0))
combo.paste(b, (a.width + 8, 0))
combo.save(out)
""",
                    leftPath,
                    rightPath,
                    outPath
                },
                RedirectStandardError = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(15_000);
        }
        catch
        {
            // Optional visual compare; layout assertions remain the gate.
        }
    }

    private static async Task AssertEarlyScreenChromeAsync(IPage page)
    {
        var audit = await page.EvaluateAsync<ChromeAudit>(
            """
            () => {
              const mascot = document.querySelector('.ob-wizard__mascot-anchor img');
              const footer = document.querySelector('[data-sticky-footer="true"]');
              const main = document.querySelector('.app-main') || document.scrollingElement;
              const scrollTop = main ? main.scrollTop : window.scrollY;
              if (!mascot || !footer) {
                return { ok: false, reason: 'missing mascot or footer', scrollTop };
              }
              const m = mascot.getBoundingClientRect();
              const content = document.querySelector('.ob-vroeg') || document.querySelector('.ob-wizard__content');
              const c = content ? content.getBoundingClientRect() : { bottom: 0 };
              const overlap = m.top < c.bottom - 8 && m.left < window.innerWidth - 40;
              const footerVisible = footer.getBoundingClientRect().bottom <= window.innerHeight + 2
                && footer.getBoundingClientRect().height > 20;
              return {
                ok: !overlap && footerVisible && scrollTop <= 4,
                reason: overlap ? 'mascot-overlap' : !footerVisible ? 'footer-hidden' : scrollTop > 4 ? 'scroll-not-top' : '',
                scrollTop
              };
            }
            """);

        Assert.True(audit.Ok, $"Early screen chrome: {audit.Reason} scrollTop={audit.ScrollTop}");
    }

    private sealed class ChromeAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
        public double ScrollTop { get; set; }
    }

    internal static async Task RegisterFreshCandidateAsync(IPage page, string baseUrl)
    {
        var email = $"vroeg.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Vroeg");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await WaitForOnboardingCircuitReadyAsync(page);
    }

    internal static async Task EnsureWizardStartedAsync(IPage page)
    {
        await WaitForWizardShellAsync(page);
        await WaitForOnboardingCircuitReadyAsync(page);

        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
            var start = page.Locator("button.ob-wizard__cta.btn-primary").First;
            if (await start.CountAsync() == 0)
            {
                start = page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" });
            }

            await start.ClickAsync(new() { Force = true, Timeout = 30_000 });
            await page.WaitForTimeoutAsync(600);
        }

        await page.Locator("#onboarding-field-firstname input, .ob-fields input").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
    }

    private static async Task WaitForOnboardingCircuitReadyAsync(IPage page)
    {
        var hasBlazor = await page.EvaluateAsync<bool>(
            "() => !!document.querySelector('script[src*=\"blazor.web.js\"]')");
        if (!hasBlazor)
        {
            return;
        }

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task WaitForWizardShellAsync(IPage page)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const loading = document.querySelector('.ob-wizard__loading');
                  if (loading) return false;
                  return document.querySelector('.ob-wizard__welcome')
                    || document.querySelector('.ob-fields')
                    || document.querySelector('.ob-wizard__finish');
                }
                """,
                null,
                new() { Timeout = 90_000 });

            if (await page.Locator(".ob-fields input").CountAsync() > 0
                || await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
            {
                return;
            }

            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(800);
        }
    }

    internal static async Task FillStepsThroughTransportAsync(
        IPage page,
        int maxTravelMinutes,
        string postcode = "2671GB")
    {
        await page.Locator(".ob-fields input").First.FillAsync("Vroeg");
        await page.Locator(".ob-fields input").Nth(1).FillAsync("Test");
        await ResolveOnboardingHomeAddressAsync(page, postcode);
        await FillOnboardingBirthDateIfPresentAsync(page);

        await ClickNextAsync(page);
        await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        await ClickNextAsync(page);
        await page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First.ClickAsync();
        var travel = page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button")
            .Filter(new() { HasText = maxTravelMinutes.ToString() });
        if (await travel.CountAsync() == 0)
        {
            travel = page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button").First;
        }

        await travel.First.ClickAsync();
    }

    internal static async Task ResolveOnboardingHomeAddressAsync(IPage page, string postcode = "2671GB")
    {
        var address = page.Locator("#onboarding-home-address, .ob-fields input[inputmode=text], .ob-fields input[autocomplete=postal-code]").First;
        await address.ClickAsync();
        await address.FillAsync("");
        await address.PressSequentiallyAsync(postcode, new() { Delay = 40 });
        await page.WaitForTimeoutAsync(1200);

        var suggestion = page.Locator("button.address-suggestions__item, .address-suggestions__item").First;
        var resolved = false;
        for (var attempt = 0; attempt < 45 && !resolved; attempt++)
        {
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync(new() { Timeout = 15_000 });
                resolved = true;
                break;
            }

            var hintOk = await page.EvaluateAsync<bool>(
                "() => { const hint = document.querySelector('.ob-fields .ob-wizard__hint'); return !!(hint?.textContent?.includes('✓')); }");
            if (hintOk)
            {
                resolved = true;
                break;
            }

            if (await page.Locator("[data-testid=onboarding-nearby-panel-desktop], [data-testid=onboarding-nearby-panel-mobile]").CountAsync() > 0)
            {
                resolved = true;
                break;
            }

            if (attempt == 5)
            {
                var spaced = postcode.Replace(" ", "", StringComparison.Ordinal);
                if (spaced.Length == 6)
                {
                    await address.PressSequentiallyAsync($"{spaced[..4]} {spaced[4..]}", new() { Delay = 40 });
                }
            }

            await page.WaitForTimeoutAsync(1000);
        }

        Assert.True(resolved, "Expected home address to resolve via suggestion or postcode lookup.");
        await page.WaitForTimeoutAsync(1200);
    }

    internal static async Task FillOnboardingBirthDateIfPresentAsync(IPage page)
    {
        var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birth.CountAsync() > 0)
        {
            await birth.FillAsync("2000-06-15");
            await birth.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }
    }

    private static async Task NavigateToTestConsentWithoutStartingAsync(IPage page)
    {
        for (var step = 0; step < 6; step++)
        {
            if (await page.Locator(".ob-wizard__consent input[type=checkbox]").CountAsync() > 0)
            {
                return;
            }

            if (await page.Locator(".test-likert").CountAsync() > 0)
            {
                return;
            }

            var skip = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Overslaan", RegexOptions.IgnoreCase) });
            if (await skip.CountAsync() > 0)
            {
                await skip.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(450);
                continue;
            }

            var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Weet ik nog niet", RegexOptions.IgnoreCase) });
            if (await unknown.CountAsync() > 0)
            {
                await unknown.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(450);
                continue;
            }

            if (await page.Locator("[data-testid=onboarding-next]").CountAsync() > 0)
            {
                await ClickNextAsync(page);
                continue;
            }

            break;
        }
    }

    private static async Task SkipOptionalStepsToConsentAsync(IPage page)
    {
        for (var step = 0; step < 4; step++)
        {
            if (await page.Locator(".test-likert").CountAsync() > 0)
            {
                return;
            }

            var skip = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Overslaan", RegexOptions.IgnoreCase) });
            if (await skip.CountAsync() > 0)
            {
                await skip.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(450);
                continue;
            }

            var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Weet ik nog niet", RegexOptions.IgnoreCase) });
            if (await unknown.CountAsync() > 0)
            {
                await unknown.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(450);
                continue;
            }

            if (await page.Locator(".ob-wizard__consent input[type=checkbox]").CountAsync() > 0)
            {
                await page.Locator(".ob-wizard__consent input[type=checkbox]").CheckAsync(new() { Force = true });
                await ClickNextAsync(page);
                await page.WaitForSelectorAsync(".test-likert", new() { Timeout = 60_000 });
                return;
            }

            break;
        }
    }

    private static async Task AssertNoEarlyInsightInterstitialAsync(IPage page)
    {
        Assert.Equal(0, await page.Locator("[data-testid=early-reward-insight]").CountAsync());
    }

    private static async Task FinishCompetencyMiniAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".test-likert", new() { Timeout = 60_000 });
        for (var i = 0; i < 12; i++)
        {
            if (await page.Locator(".test-likert").CountAsync() == 0)
            {
                break;
            }

            await page.Locator(".test-likert__opt").Nth(3).ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(400);
            var miniNext = page.Locator("button.test-flow__next:not([disabled])").First;
            if (await miniNext.CountAsync() > 0)
            {
                await miniNext.ClickAsync(new() { Force = true });
            }

            await page.WaitForTimeoutAsync(500);
        }

        await AssertNoEarlyInsightInterstitialAsync(page);
    }

    internal static async Task ClickNextAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.Locator("[data-testid=onboarding-next]").ClickAsync(new() { Force = true, Timeout = 20_000 });
        await page.WaitForTimeoutAsync(500);
    }
}

internal static class SmokeReachability
{
    public static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            using var response = await http.GetAsync(baseUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
