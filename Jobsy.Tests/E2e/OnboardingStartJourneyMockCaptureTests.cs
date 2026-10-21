using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartJourneyMockCaptureTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
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

    [Theory]
    [InlineData(1280, 720, false, "1280x720")]
    [InlineData(390, 844, true, "390x844")]
    public async Task Viewport_shows_cta_above_fold(int width, int height, bool mobile, string tag)
    {
        var outDir = ArtifactsDir();
        Directory.CreateDirectory(outDir);
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenMockAsync(context, baseUrl);
        await page.Locator("[data-testid=onboarding-begin]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".ob-start-journey-route__vignette img").First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const imgs = document.querySelectorAll('.ob-start-journey-route__vignette img');
              return imgs.length >= 5 && [...imgs].every(i => i.complete && i.naturalWidth > 0);
            }
            """,
            null,
            new() { Timeout = 60_000 });

        if (!mobile)
        {
            var ctaVisible = await page.EvaluateAsync<bool>("""
                () => {
                  const btn = document.querySelector('[data-testid=onboarding-begin]');
                  if (!btn) return false;
                  const r = btn.getBoundingClientRect();
                  return r.top >= 0 && r.bottom <= window.innerHeight && r.height > 20;
                }
                """);

            Assert.True(ctaVisible, $"Aan de slag must be fully visible above the fold at {tag}.");
        }

        if (mobile)
        {
            var mobileLayout = await page.EvaluateAsync<string>("""
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
                    if (r.left < pad - 0.5) return `text-left:${Math.round(r.left)}:${el.textContent?.trim().slice(0, 24)}`;
                    if (r.right > vw - pad + 0.5) return `text-right:${Math.round(r.right)}:${el.textContent?.trim().slice(0, 24)}`;
                  }

                  const route = document.querySelector('[data-testid=onboarding-start-journey-route]');
                  const cta = document.querySelector('[data-testid=onboarding-begin]');
                  if (route && cta) {
                    const rr = route.getBoundingClientRect();
                    const cr = cta.getBoundingClientRect();
                    const gap = cr.top - rr.bottom;
                    if (gap < gapRouteCta - 1) return `route-cta-gap:${Math.round(gap)}`;
                    if (rectsOverlap(rr, cr, 0)) return 'route-overlaps-cta';
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
                        return `overlap:${boxes[i].className}:${boxes[j].className}`;
                      }
                    }
                  }

                  return 'ok';
                }
                """);
            Assert.Equal("ok", mobileLayout);
        }

        if (width >= 1024)
        {
            var heroMobPanorama = await page.EvaluateAsync<bool>("""
                () => {
                  const band = document.querySelector('.ob-start-journey-mock-hero');
                  if (!band) return false;
                  const bg = getComputedStyle(band).backgroundImage;
                  return bg && bg.includes('start-mobile');
                }
                """);
            Assert.True(heroMobPanorama, "Desktop mock hero must use mobile panorama (Welkom gate crop).");
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"onboarding-start-journey-mock-{tag}.png"),
            FullPage = false,
        });

        if (width >= 1024)
        {
            await page.Locator(".ob-start-journey-mock-hero").ScreenshotAsync(new()
            {
                Path = Path.Combine(outDir, "onboarding-start-journey-mock-hero-desk-crop.png"),
            });
        }
    }

    [Fact]
    public async Task Desktop_full_page_capture()
    {
        var outDir = ArtifactsDir();
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenMockAsync(context, baseUrl);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, "onboarding-start-journey-mock-1280-full.png"),
            FullPage = true,
        });
    }

    private static async Task<IPage> OpenMockAsync(IBrowserContext context, string baseUrl)
    {
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/preview/onboarding-start-journey", new()
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 90_000,
        });
        await page.Locator("[data-testid=onboarding-start-journey-route]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        return page;
    }

    private static string ArtifactsDir()
    {
        var dir = "/opt/cursor/artifacts/screenshots";
        if (!Directory.Exists("/opt/cursor"))
        {
            dir = Path.Combine(FindRepoRoot(), "artifacts");
        }

        return dir;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        return Directory.GetCurrentDirectory();
    }
}
