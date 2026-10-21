using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartJourneyCaptureTests : IAsyncLifetime
{
    private OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        _ = _web.EnsureServerAddress();
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

    [Theory]
    [InlineData(1280, 900, false, "1280x720")]
    [InlineData(390, 844, true, "390x844")]
    public async Task Start_journey_renders_with_layout_contract(int width, int height, bool mobile, string tag)
    {
        var outDir = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(outDir);
        var baseUrl = _web!.EnsureServerAddress().TrimEnd('/');

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync($"{baseUrl}/__test/sign-in?return={Uri.EscapeDataString("/candidate/start")}");
        await page.Locator("[data-ob-rustig-screen=start-journey]").WaitForAsync(new() { Timeout = 90_000 });
        await page.Locator("[data-testid=onboarding-start-journey-route]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const imgs = document.querySelectorAll('.ob-start-journey-route__vignette img');
              return imgs.length >= 5 && [...imgs].every(i => i.complete && i.naturalWidth > 0);
            }
            """,
            null,
            new() { Timeout = 60_000 });

        if (mobile)
        {
            Assert.Equal("ok", await EvaluateMobileLayoutAsync(page));
        }
        else
        {
            var heroMobPanorama = await page.EvaluateAsync<bool>("""
                () => {
                  const band = document.querySelector('.ob-start-journey-hero');
                  if (!band) return false;
                  const bg = getComputedStyle(band).backgroundImage;
                  return bg && bg.includes('start-mobile');
                }
                """);
            Assert.True(heroMobPanorama, "Desktop start hero must use mobile panorama (Welkom gate crop).");
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"onboarding-start-journey-{tag}.png"),
            FullPage = false,
        });

        await page.GetByTestId("onboarding-begin").ClickAsync(new() { Force = mobile });
        await page.Locator("[data-ob-rustig-screen=step-01]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        Assert.Equal("welcome", _web!.OnboardingSource);
    }

    private static async Task<string> EvaluateMobileLayoutAsync(IPage page)
        => await page.EvaluateAsync<string>("""
            () => {
              const pad = 16;
              const vw = window.innerWidth;
              const gapRouteCta = 24;

              const rectsOverlap = (a, b, slack = 1) =>
                a.left < b.right - slack &&
                b.left < a.right - slack &&
                a.top < b.bottom - slack &&
                b.top < a.bottom - slack;

              const textEls = [
                ...document.querySelectorAll('.ob-start-journey-route__title'),
                ...document.querySelectorAll('.ob-start-journey-route__sub'),
                ...document.querySelectorAll('.ob-start-journey-route__here'),
              ];
              for (const el of textEls) {
                const r = el.getBoundingClientRect();
                if (r.width < 1 || r.height < 1) continue;
                if (r.left < pad - 0.5) return `text-left:${Math.round(r.left)}`;
                if (r.right > vw - pad + 0.5) return `text-right:${Math.round(r.right)}`;
              }

              const titles = [...document.querySelectorAll('.ob-start-journey-route__title')];
              for (const t of titles) {
                const style = getComputedStyle(t);
                const lh = parseFloat(style.lineHeight);
                const linePx = Number.isFinite(lh) && lh > 0 ? lh : parseFloat(style.fontSize) * 1.25;
                if (t.scrollHeight > linePx * 1.4) {
                  return `title-multiline:${t.textContent?.trim()}`;
                }
              }

              const route = document.querySelector('[data-testid=onboarding-start-journey-route]');
              const cta = document.querySelector('[data-testid=onboarding-begin]');
              if (route && cta) {
                const rr = route.getBoundingClientRect();
                const cr = cta.getBoundingClientRect();
                const gap = cr.top - rr.bottom;
                if (gap < gapRouteCta - 1) return `route-cta-gap:${Math.round(gap)}`;
                if (rectsOverlap(rr, cr, 0)) return 'route-overlaps-cta';
                if (cr.top < 0 || cr.bottom > window.innerHeight || cr.height < 20) {
                  return 'cta-not-above-fold';
                }
              }

              const boxes = [
                ...document.querySelectorAll('.ob-start-journey-route__label'),
                ...document.querySelectorAll('.ob-start-journey-route__vignette'),
                ...document.querySelectorAll('.ob-start-journey-route__here'),
              ];
              if (cta) boxes.push(cta);

              for (let i = 0; i < boxes.length; i++) {
                for (let j = i + 1; j < boxes.length; j++) {
                  const a = boxes[i].getBoundingClientRect();
                  const b = boxes[j].getBoundingClientRect();
                  if (a.width < 1 || a.height < 1 || b.width < 1 || b.height < 1) continue;
                  if (rectsOverlap(a, b, 0)) {
                    return `overlap:${i}-${j}`;
                  }
                }
              }

              return 'ok';
            }
            """);
}
