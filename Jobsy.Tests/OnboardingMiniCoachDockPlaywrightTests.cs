using System.Text.RegularExpressions;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Onboarding mini steps 7–10: coach lobster must not cover Volgende; label must not truncate.
/// Run with <c>JOBSY_E2E_BASE_URL=http://127.0.0.1:5201</c> (local stack + e2e API).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingMiniCoachDockPlaywrightTests
{
    [Theory]
    [InlineData(390, 844)]
    [InlineData(360, 740)]
    public async Task Mini_steps_7_through_10_volgende_clear_of_mascot(int width, int height)
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

        await RegisterAndReachMiniStep7Async(page, baseUrl);

        for (var miniStep = 7; miniStep <= 10; miniStep++)
        {
            await page.EvaluateAsync("() => { window.scrollTo(0, 0); document.querySelector('.app-main')?.scrollTo(0, 0); }");
            await page.WaitForSelectorAsync(".test-flow__counter", new() { Timeout = 30_000 });
            await page.WaitForSelectorAsync("button.test-flow__next", new() { Timeout = 15_000 });

            var audit = await page.EvaluateAsync<MiniFooterAudit>(
                """
                (miniStep) => {
                  const next = document.querySelector('button.test-flow__next');
                  if (!next) return { ok: false, reason: 'no-next' };
                  const nextBox = next.getBoundingClientRect();
                  const label = (next.innerText || '').trim();
                  const truncated = label.length > 0 && label !== 'Volgende' && label !== 'Volgende test';
                  const style = getComputedStyle(next);
                  const clipped = next.scrollWidth > next.clientWidth + 1
                    || style.textOverflow === 'ellipsis' && next.offsetWidth < next.scrollWidth;
                  const mascotSelectors = [
                    '.lobsy-coach-dock',
                    '#lobsy-coach-btn',
                    '.lobsy-coach-dock__btn img',
                    '.lobsy-assistant-tab',
                    '.lobsy-assistant-tab__btn',
                    '.pub-mascot',
                    '.lobsy-assistant-tab__mascot'
                  ];
                  const overlaps = [];
                  for (const sel of mascotSelectors) {
                    document.querySelectorAll(sel).forEach(el => {
                      const st = getComputedStyle(el);
                      if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') return;
                      const r = el.getBoundingClientRect();
                      if (r.width < 4 || r.height < 4) return;
                      const hit = nextBox.left < r.right && nextBox.right > r.left
                        && nextBox.top < r.bottom && nextBox.bottom > r.top;
                      if (hit) overlaps.push(sel);
                    });
                  }
                  const centerHit = document.elementFromPoint(
                    Math.min(nextBox.right - 2, window.innerWidth - 1),
                    nextBox.top + nextBox.height / 2);
                  const centerOnNext = centerHit === next || next.contains(centerHit);
                  const footer = document.querySelector('.test-flow__footer[data-sticky-footer]');
                  const footerBottom = footer ? footer.getBoundingClientRect().bottom : 0;
                  const vh = window.innerHeight;
                  const footerFlush = footer ? Math.abs(footerBottom - vh) <= 1.5 : false;
                  const currentChip = document.querySelector('.ob-wizard__test-tab.is-current');
                  const currentBg = currentChip ? getComputedStyle(currentChip).backgroundColor : '';
                  const currentHasClass = !!currentChip;
                  const currentStyle = currentChip ? getComputedStyle(currentChip) : null;
                  const brandish = currentStyle
                    && (currentStyle.color === 'rgb(255, 255, 255)' || currentStyle.color === '#ffffff')
                    && currentStyle.backgroundColor !== 'rgba(0, 0, 0, 0)'
                    && !currentStyle.backgroundColor.includes('221, 213, 204');
                  const doneChip = document.querySelector('.ob-wizard__test-tab.is-done');
                  const doneStyle = doneChip ? getComputedStyle(doneChip) : null;
                  const doneBg = doneStyle ? doneStyle.backgroundColor : '';
                  const doneGreen = doneStyle
                    && (doneStyle.color === 'rgb(21, 128, 61)' || doneStyle.color.includes('128, 61)'));
                  const doneOk = miniStep <= 7 || (doneChip && doneGreen);
                  const ok = overlaps.length === 0 && !truncated && !clipped && centerOnNext
                    && footerFlush && currentHasClass && brandish && doneOk;
                  let reason = 'ok';
                  if (overlaps.length) reason = 'mascot-overlap';
                  else if (truncated) reason = 'truncated-label';
                  else if (clipped) reason = 'clipped-label';
                  else if (!centerOnNext) reason = 'elementFromPoint-not-next';
                  else if (!footerFlush) reason = 'footer-not-flush bottom=' + footerBottom + ' vh=' + vh;
                  else if (!currentHasClass || !brandish) reason = 'active-chip-color currentBg=' + currentBg;
                  else if (!doneOk) reason = 'done-chip-color doneBg=' + doneBg;
                  return {
                    ok,
                    reason,
                    label,
                    overlaps,
                    centerTag: centerHit ? centerHit.tagName + (centerHit.className ? '.' + centerHit.className : '') : null,
                    truncated,
                    clipped,
                    footerBottom,
                    vh,
                    currentBg,
                    doneBg
                  };
                }
                """,
                miniStep);

            Assert.True(audit.Ok, $"Step {miniStep} @ {width}x{height}: {audit.Reason} label={audit.Label} overlaps=[{string.Join(',', audit.Overlaps ?? [])}] footer={audit.FooterBottom}/{audit.Vh} currentBg={audit.CurrentBg}");

            if (miniStep == 10)
            {
                break;
            }

            await CompleteCurrentMiniAsync(page);
            await page.WaitForFunctionAsync(
                """
                prev => {
                  const n = parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10);
                  return n > prev || !!document.querySelector('.ob-wizard__finish');
                }
                """,
                miniStep,
                new() { Timeout = 90_000 });
        }
    }

    private static async Task CompleteCurrentMiniAsync(IPage page)
    {
        for (var q = 0; q < 5; q++)
        {
            await page.Locator(".test-likert input[value=\"4\"]").First.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(450);
            var next = page.Locator("button.test-flow__next:not([disabled])").First;
            if (await next.CountAsync() > 0)
            {
                await next.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(550);
            }
            else
            {
                await page.WaitForTimeoutAsync(900);
            }
        }
    }

    private sealed class MiniFooterAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
        public string? Label { get; set; }
        public string[]? Overlaps { get; set; }
        public string? CenterTag { get; set; }
        public bool Truncated { get; set; }
        public bool Clipped { get; set; }
        public double FooterBottom { get; set; }
        public double Vh { get; set; }
        public string? CurrentBg { get; set; }
        public string? DoneBg { get; set; }
    }

    private static async Task RegisterAndReachMiniStep7Async(IPage page, string baseUrl)
    {
        var email = $"coach.mini.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Sanne");
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code));
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(800);
        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" }).ClickAsync();
            await page.WaitForTimeoutAsync(400);
        }

        await page.Locator(".ob-fields input").Nth(1).FillAsync("Test");
        await page.Locator(".ob-fields input[autocomplete=postal-code], .ob-fields input[inputmode=text]").First.FillAsync("2671GB");
        await page.WaitForTimeoutAsync(400);
        var suggestion = page.Locator(".ob-wizard__suggestions button").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.ClickAsync();
        }

        await page.Locator("[data-testid=onboarding-birth-date] input").FillAsync("2000-01-01");
        await ClickOnboardingNextAsync(page);
        await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        await ClickOnboardingNextAsync(page);
        await page.Locator("#onboarding-field-transport .ob-wizard__transport button").First.ClickAsync();
        await page.Locator(".ob-wizard__transport button").Filter(new() { HasTextRegex = new Regex("^30$") }).First.ClickAsync();
        var skip = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Overslaan", RegexOptions.IgnoreCase) }).First;
        if (await skip.CountAsync() > 0)
        {
            await skip.ClickAsync(new() { Timeout = 8_000 });
        }
        else
        {
            await ClickOnboardingNextAsync(page);
        }

        await page.WaitForTimeoutAsync(300);
        await ClickOnboardingNextAsync(page);
        await page.Locator("button").Filter(new() { HasTextRegex = new Regex("^MBO$", RegexOptions.IgnoreCase) }).First.ClickAsync(new() { Timeout = 8_000 });
        await ClickOnboardingNextAsync(page);
        var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Weet ik nog niet", RegexOptions.IgnoreCase) }).First;
        if (await unknown.CountAsync() > 0)
        {
            await unknown.ClickAsync(new() { Timeout = 8_000 });
        }
        else
        {
            await ClickOnboardingNextAsync(page);
        }

        await ClickOnboardingNextAsync(page);
        foreach (var cb in await page.Locator(".ob-wizard__consent input:not(:checked)").AllAsync())
        {
            await cb.CheckAsync(new() { Force = true });
        }

        await ClickOnboardingNextAsync(page);
        await page.WaitForSelectorAsync(".test-likert", new() { Timeout = 30_000 });
    }

    private static async Task ClickOnboardingNextAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var next = page.Locator("[data-testid=onboarding-next]");
        await next.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await page.WaitForTimeoutAsync(350);
    }
}
