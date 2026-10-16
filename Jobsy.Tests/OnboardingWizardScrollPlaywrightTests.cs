using System.Text.RegularExpressions;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Tests;

/// <summary>
/// Profile wizard steps 1–6 must start at scroll top on mobile (scroll container is <c>.ob-wizard__content</c> when present).
/// Mini-test steps 7–10 use <c>test-flow</c> chrome; see <see cref="OnboardingMiniCoachDockPlaywrightTests"/>.
/// Run with <c>JOBSY_E2E_BASE_URL=http://127.0.0.1:5201</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingWizardScrollPlaywrightTests
{
    /// <summary>JOBSY_CAPTURE_ONBOARDING_SCROLL=before|after → saves steps 1–10 (390×844 viewport top) to store and repo <c>onboarding-scroll/</c>.</summary>
    [Fact]
    public async Task Capture_onboarding_scroll_screenshots_when_requested()
    {
        var mode = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_SCROLL") ?? "").Trim().ToLowerInvariant();
        if (mode is not ("before" or "after"))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        var repoScrollDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "onboarding-scroll", mode));
        var outDirs = new[]
        {
            $"/cursor/stores/self/onboarding-scroll/{mode}",
            repoScrollDir,
        };
        foreach (var dir in outDirs)
        {
            Directory.CreateDirectory(dir);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndBeginWizardAsync(page, baseUrl);

        const int lastCaptureStep = 10;
        for (var step = 1; step <= lastCaptureStep; step++)
        {
            await WaitWizardStepAsync(page, step);
            await ScrollWizardToTopAsync(page);
            await page.WaitForTimeoutAsync(300);

            foreach (var outDir in outDirs)
            {
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(outDir, $"step-{step:00}.png"),
                    FullPage = false,
                });
            }

            if (step == 1)
            {
                await page.Locator("#onboarding-home-address, .ob-fields input[inputmode=text]").First.FillAsync("2671");
                await page.WaitForTimeoutAsync(600);
                foreach (var outDir in outDirs)
                {
                    await page.ScreenshotAsync(new()
                    {
                        Path = Path.Combine(outDir, "step-01-suggestions.png"),
                        FullPage = false,
                    });
                }

                await ScrollWizardToTopAsync(page);
            }

            if (mode == "after" && step is 1 or 3)
            {
                await ScrollWizardToBottomAsync(page);
                await page.WaitForTimeoutAsync(300);
                foreach (var outDir in outDirs)
                {
                    await page.ScreenshotAsync(new()
                    {
                        Path = Path.Combine(outDir, $"step-{step:00}-scrolled-bottom.png"),
                        FullPage = false,
                    });
                }

                await ScrollWizardToTopAsync(page);
            }

            if (step >= lastCaptureStep)
            {
                break;
            }

            await ScrollMainDownAsync(page);
            await AdvanceWizardStepAsync(page, step);
        }

        if (mode == "after")
        {
            BuildMockupCompareSheet(repoScrollDir);
        }
    }

    private static void BuildMockupCompareSheet(string afterDir)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(afterDir, "..", ".."));
        var compareDir = Path.Combine(repoRoot, "onboarding-scroll", "compare");
        Directory.CreateDirectory(compareDir);
        var mockupDir = Path.Combine(repoRoot, "docs", "mockups", "onboarding-ontwerp", "A");
        foreach (var step in new[] { 1, 3, 5, 7, 8 })
        {
            var mockup = Path.Combine(mockupDir, $"step-{step}-mobile-390x844.png");
            var impl = Path.Combine(afterDir, $"step-{step:00}.png");
            var dest = Path.Combine(compareDir, $"step-{step:00}-mockup-vs-impl.png");
            if (!File.Exists(mockup) || !File.Exists(impl))
            {
                continue;
            }

            using var left = Image.Load<Rgba32>(mockup);
            using var right = Image.Load<Rgba32>(impl);
            var height = Math.Max(left.Height, right.Height);
            var width = left.Width + right.Width + 8;
            using var canvas = new Image<Rgba32>(width, height);
            canvas.Mutate(ctx =>
            {
                ctx.BackgroundColor(SixLabors.ImageSharp.Color.FromRgb(245, 242, 238));
                ctx.DrawImage(left, new Point(0, 0), 1f);
                ctx.DrawImage(right, new Point(left.Width + 8, 0), 1f);
            });
            canvas.SaveAsPng(dest);
            var storeCompare = "/cursor/stores/self/onboarding-scroll/compare";
            Directory.CreateDirectory(storeCompare);
            canvas.SaveAsPng(Path.Combine(storeCompare, $"step-{step:00}-mockup-vs-impl.png"));
        }
    }

    private static async Task WaitWizardStepAsync(IPage page, int step)
    {
        await page.WaitForFunctionAsync(
            """
            expected => parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10) === expected
            """,
            step,
            new() { Timeout = 30_000 });
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(360, 740)]
    public async Task Each_step_has_primary_action_pinned_to_viewport_bottom(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndBeginWizardAsync(page, baseUrl);

        const int lastStep = 10;
        for (var step = 1; step <= lastStep; step++)
        {
            await WaitWizardStepAsync(page, step);
            await ScrollWizardToTopAsync(page);
            await AssertMobileActionBarAsync(page, step, width, height);

            if (step >= lastStep)
            {
                break;
            }

            await ScrollMainDownAsync(page);
            await AdvanceWizardStepAsync(page, step);
        }
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(360, 740)]
    public async Task Each_step_starts_with_heading_visible_at_scroll_top(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndBeginWizardAsync(page, baseUrl);

        const int lastStep = 6;
        for (var step = 1; step <= lastStep; step++)
        {
            await WaitWizardStepAsync(page, step);
            await AssertStepScrollTopAsync(page, step);

            if (step >= lastStep)
            {
                break;
            }

            await ScrollMainDownAsync(page);
            await AdvanceWizardStepAsync(page, step);
        }
    }

    private static async Task AssertStepScrollTopAsync(IPage page, int step)
    {
        var audit = await page.EvaluateAsync<ScrollAudit>(
            """
            () => {
              const root = document.querySelector('.ob-wizard__content') || document.querySelector('.app-main');
              const mainScroll = root ? root.scrollTop : 0;
              const winScroll = window.scrollY || 0;
              const h1 = document.querySelector('.ob-wizard__content h1, .ob-fields h1');
              if (!h1) {
                return { ok: false, reason: 'no-h1', mainScroll, winScroll, headingTop: null };
              }
              const rect = h1.getBoundingClientRect();
              const rootRect = root ? root.getBoundingClientRect() : { top: 0, bottom: window.innerHeight };
              const inView = rect.top >= rootRect.top - 2
                && rect.top < rootRect.bottom - 40
                && rect.height > 0;
              const ok = mainScroll <= 1 && winScroll <= 1 && inView;
              let reason = 'ok';
              if (mainScroll > 1) reason = 'scroll-' + mainScroll;
              else if (winScroll > 1) reason = 'window-scroll-' + winScroll;
              else if (!inView) reason = 'heading-offscreen top=' + rect.top;
              return { ok, reason, mainScroll, winScroll, headingTop: rect.top };
            }
            """);

        Assert.True(audit.Ok, $"Step {step}: {audit.Reason} scroll={audit.MainScroll} win={audit.WinScroll} h1Top={audit.HeadingTop}");
    }

    private static async Task AssertMobileActionBarAsync(IPage page, int step, int width, int height)
    {
        var audit = await page.EvaluateAsync<ActionBarAudit>(
            """
            () => {
              const next = document.querySelector('[data-testid=onboarding-next]')
                || document.querySelector('button.test-flow__next');
              if (!next) {
                return { ok: false, reason: 'no-next', bottomGap: null, width: null, overlap: null };
              }
              const rect = next.getBoundingClientRect();
              const vh = window.innerHeight;
              const bottomGap = vh - rect.bottom;
              const footer = next.closest('.ob-wizard__footer, .test-flow__footer');
              const footerRect = footer ? footer.getBoundingClientRect() : null;
              const footerFull = footerRect && footerRect.width >= window.innerWidth - 40;
              const minWidth = footerFull
                ? Math.max(200, footerRect.width - 110)
                : window.innerWidth - 40;
              const wideEnough = rect.width >= minWidth - 1;
              const bottomOk = bottomGap >= -2 && bottomGap <= 24;
              const cx = Math.min(rect.right - 4, window.innerWidth - 2);
              const cy = rect.top + rect.height / 2;
              const centerHit = document.elementFromPoint(cx, cy);
              const centerOnNext = centerHit === next || (centerHit && next.contains(centerHit));
              const blockers = document.querySelectorAll(
                '.ob-wizard__content input:not([type=hidden]), .ob-wizard__content select, .ob-wizard__content textarea, .ob-wizard__content .lobsy-choice-tile, .ob-wizard__content .test-likert__opt');
              const overlaps = [];
              const footerTop = footer ? footer.getBoundingClientRect().top : vh;
              for (const el of blockers) {
                const st = getComputedStyle(el);
                if (st.display === 'none' || st.visibility === 'hidden') continue;
                const r = el.getBoundingClientRect();
                if (r.width < 4 || r.height < 4) continue;
                if (r.top >= footerTop - 2) continue;
                const hit = rect.left < r.right && rect.right > r.left && rect.top < r.bottom && rect.bottom > r.top;
                if (hit) overlaps.push(el.tagName + (el.className ? '.' + String(el.className).split(' ')[0] : ''));
              }
              const footerBg = footer ? getComputedStyle(footer).backgroundColor : '';
              const footerPosition = footer ? getComputedStyle(footer).position : '';
              const shellClass = document.querySelector('.app-shell')?.className || '';
              const backVisible = !!document.querySelector('[data-testid=onboarding-back], button.test-flow__back');
              let reason = 'ok';
              if (!bottomOk) reason = 'bottom-gap-' + bottomGap.toFixed(1);
              else if (!wideEnough) reason = 'narrow-' + rect.width.toFixed(0) + '<' + minWidth;
              else if (!centerOnNext) reason = 'center-not-next-' + (centerHit ? centerHit.tagName : 'null');
              else if (overlaps.length) reason = 'overlap-' + overlaps.slice(0, 3).join(',');
              else if (!backVisible && !(parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10) === 1)) {
                reason = 'no-back';
              }
              const ok = reason === 'ok';
              const styles = [...document.querySelectorAll('link[rel=stylesheet]')]
                .map(l => l.href)
                .filter(h => h.includes('onboarding'))
                .join('|');
              return {
                ok,
                reason,
                bottomGap,
                width: rect.width,
                overlap: overlaps.join(','),
                footerBg,
                footerPosition,
                shellClass,
                styles,
                footerTag: footer ? footer.tagName + (footer.getAttribute('data-sticky-footer') ?? '') : 'none'
              };
            }
            """);

        Assert.True(audit.Ok, $"Step {step} @ {width}x{height}: {audit.Reason} gap={audit.BottomGap} w={audit.Width} overlap={audit.Overlap} footerBg={audit.FooterBg} pos={audit.FooterPosition} tag={audit.FooterTag} styles={audit.Styles}");
    }

    private static Task ScrollWizardToTopAsync(IPage page)
        => page.EvaluateAsync(
            """
            () => {
              const content = document.querySelector('.ob-wizard__content');
              if (content) content.scrollTop = 0;
              const main = document.querySelector('.app-main');
              if (main) main.scrollTop = 0;
              window.scrollTo(0, 0);
            }
            """);

    private static Task ScrollWizardToBottomAsync(IPage page)
        => page.EvaluateAsync(
            """
            () => {
              const content = document.querySelector('.ob-wizard__content');
              if (content) content.scrollTop = content.scrollHeight;
              const main = document.querySelector('.app-main');
              if (main) main.scrollTop = main.scrollHeight;
            }
            """);

    private static async Task ScrollMainDownAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('.ob-wizard__content') || document.querySelector('.app-main');
              const amount = Math.min(480, (root?.scrollHeight || 800) - (root?.clientHeight || 400));
              if (root) root.scrollTop = amount;
              window.scrollTo(0, amount);
            }
            """);
        await page.WaitForTimeoutAsync(200);
    }

    private static async Task AdvanceWizardStepAsync(IPage page, int step)
    {
        if (step == 1)
        {
            var first = page.Locator(".ob-fields label:has-text('Voornaam') input, .ob-fields label:has-text('First') input").First;
            if (await first.CountAsync() == 0)
            {
                first = page.Locator(".ob-fields input").First;
            }

            await first.FillAsync("Scroll");
            await page.Locator(".ob-fields input").Nth(1).FillAsync("Test");
            await page.Locator("#onboarding-home-address, .ob-fields input[inputmode=text], .ob-fields input[autocomplete=postal-code]").First.FillAsync("2671GB");
            await page.WaitForTimeoutAsync(500);
            var suggestion = page.Locator(".ob-wizard__suggestions button").First;
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync(new() { Timeout = 5_000 });
            }

            var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
            if (await birth.CountAsync() > 0)
            {
                await birth.FillAsync("2000-06-15");
                await birth.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
            }

            await page.WaitForTimeoutAsync(300);
        }
        else if (step == 2)
        {
            await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        }
        else if (step == 3)
        {
            await page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First.ClickAsync();
            await page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button").Filter(new() { HasText = "30" }).First.ClickAsync();
        }
        else if (step == 4)
        {
            var skipWork = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Overslaan", RegexOptions.IgnoreCase) }).First;
            if (await skipWork.CountAsync() > 0)
            {
                await skipWork.ClickAsync(new() { Timeout = 8_000 });
                return;
            }
        }
        else if (step == 5)
        {
            await page.Locator("button").Filter(new() { HasTextRegex = new Regex("^MBO$", RegexOptions.IgnoreCase) }).First.ClickAsync(new() { Timeout = 8_000 });
            await ClickOnboardingNextAsync(page);
            return;
        }
        else if (step == 6)
        {
            var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Weet ik nog niet", RegexOptions.IgnoreCase) }).First;
            if (await unknown.CountAsync() > 0)
            {
                await unknown.ClickAsync(new() { Timeout = 8_000 });
                return;
            }
        }
        else if (step == 7)
        {
            await FinishCurrentMiniTestAsync(page);
            return;
        }
        else if (step is >= 8 and <= 9)
        {
            await FinishCurrentMiniTestAsync(page);
            return;
        }
        else if (step == 10)
        {
            await FinishCurrentMiniTestAsync(page);
            return;
        }

        await ClickOnboardingNextAsync(page);
    }

    private static async Task FinishCurrentMiniTestAsync(IPage page)
    {
        var startStep = await page.EvaluateAsync<int>(
            "() => parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10)");

        if (await page.Locator(".ob-wizard__consent input[type=checkbox]").CountAsync() > 0)
        {
            await page.Locator(".ob-wizard__consent input[type=checkbox]").CheckAsync(new() { Force = true });
            await ClickOnboardingNextAsync(page);
            await page.WaitForSelectorAsync(".test-likert", new() { Timeout = 60_000 });
        }

        for (var q = 0; q < 8; q++)
        {
            var stepNow = await page.EvaluateAsync<int>(
                "() => parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10)");
            if (stepNow > startStep || await page.Locator(".ob-wizard__finish").CountAsync() > 0)
            {
                return;
            }

            if (await page.Locator(".test-likert").CountAsync() == 0)
            {
                await page.WaitForTimeoutAsync(700);
                continue;
            }

            await page.Locator(".test-likert__opt").Nth(3).ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(500);
            var miniNext = page.Locator("button.test-flow__next:not([disabled])").First;
            if (await miniNext.CountAsync() > 0)
            {
                await miniNext.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(700);
            }
            else
            {
                await page.WaitForTimeoutAsync(1400);
            }
        }

        await page.WaitForFunctionAsync(
            """
            start => {
              const n = parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10);
              return n > start || !!document.querySelector('.ob-wizard__finish');
            }
            """,
            startStep,
            new() { Timeout = 120_000 });
    }

    private static async Task RegisterAndBeginWizardAsync(IPage page, string baseUrl)
    {
        var email = $"scroll.ob.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await page.Locator("input[type=email]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Scroll");
        var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
        await submit.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await submit.First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code));
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(600);
        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" }).ClickAsync();
            await page.WaitForTimeoutAsync(400);
        }
    }

    private static async Task ClickOnboardingNextAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var next = page.Locator("[data-testid=onboarding-next]");
        await next.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await page.WaitForTimeoutAsync(450);
    }

    private sealed class ScrollAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
        public double MainScroll { get; set; }
        public double WinScroll { get; set; }
        public double? HeadingTop { get; set; }
    }

    private sealed class ActionBarAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
        public double? BottomGap { get; set; }
        public double? Width { get; set; }
        public string? Overlap { get; set; }
        public string? FooterBg { get; set; }
        public string? FooterPosition { get; set; }
        public string? ShellClass { get; set; }
        public string? Styles { get; set; }
        public string? FooterTag { get; set; }
    }
}
