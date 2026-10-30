using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acceptatie repro + verification for banenkaart functional checklist (PR #922).
/// Screenshots under /opt/cursor/artifacts/banenkaart-acc-nine/.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartAccNinePointPlaywrightTests
{
    private const string DefaultAccUrl = "https://acceptatie.lobsy.nl";
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";
    private static readonly string ArtifactDir = "/opt/cursor/artifacts/banenkaart-acc-nine";

    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Acc_banenkaart_nine_point_checklist(int width, int height, bool touch)
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? DefaultAccUrl;
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(ArtifactDir);
        var tag = $"{width}x{height}";

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var cspViolations = new List<string>();
        var consoleErrors = new List<string>();
        var font404 = 0;
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }

            if (msg.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("script-src", StringComparison.OrdinalIgnoreCase))
            {
                cspViolations.Add(msg.Text);
            }
        };
        page.Response += (_, resp) =>
        {
            if (resp.Status == 404 && resp.Url.Contains("/fonts/", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref font404);
            }
        };

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
            && !await KbE2e.TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed on acceptatie.");
        }

        var allStatus = (await page.APIRequest.GetAsync(baseUrl + "/api/banenkaart-client/all")).Status;
        var pinsStatus = (await page.APIRequest.GetAsync(
            baseUrl + "/api/vacancies/pins?transport=Fiets&maxMinutes=30&minLat=52&minLng=4&maxLat=52.5&maxLng=5&zoom=12.5")).Status;

        await page.GotoAsync(baseUrl + "/banenkaart?maxMinutes=60&transport=Auto&originLat=52.3725&originLng=4.8921&originLabel=Amsterdam",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitForMapReadyAsync(page);

        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, $"01-banenkaart-{tag}.png"), FullPage = false });

        var bottomNav = await page.Locator("[data-testid=candidate-bottom-nav-ssr]").CountAsync();
        Assert.True(bottomNav > 0, "Candidate bottom nav should be visible.");

        var headerNavLinks = await page.Locator("[data-testid=candidate-header-nav] .candidate-header-nav__link").CountAsync();
        Assert.Equal(0, headerNavLinks);

        var variantC = await page.Locator(".kb-banenkaart-header-strip").CountAsync();
        var legacyStrip = await page.Locator("[data-testid=kb-nearby-banner].kb-nearby-banner--strip").CountAsync();
        Assert.True(variantC > 0 || legacyStrip > 0, "Header illustration strip should be present above filters.");
        if (variantC == 0 && legacyStrip > 0)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, $"00-strip-legacy-{tag}.png"), FullPage = false });
        }

        Assert.Equal(0, font404);
        Assert.True(cspViolations.Count == 0, "CSP: " + string.Join("; ", cspViolations));
        Assert.True(allStatus is 200 or 304, "/all status " + allStatus);
        Assert.True(pinsStatus == 200, "pins?zoom=12.5 status " + pinsStatus);

        await TapFirstPinOrClusterAsync(page);
        await page.WaitForTimeoutAsync(1200);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, $"02-popup-{tag}.png"), FullPage = false });

        var heart = page.Locator(".map-cluster-card__slide.is-active .map-popup__heart, .map-popup__heart[data-save-job]").First;
        if (await heart.CountAsync() > 0)
        {
            var urlBefore = page.Url;
            var pressedBefore = await heart.GetAttributeAsync("aria-pressed") ?? "false";
            await heart.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(800);
            Assert.Equal(urlBefore, page.Url);
            var pressedAfter = await heart.GetAttributeAsync("aria-pressed") ?? "false";
            Assert.NotEqual(pressedBefore, pressedAfter);
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await WaitForMapReadyAsync(page);
            await TapFirstPinOrClusterAsync(page);
            await page.WaitForTimeoutAsync(1000);
            var heartAfterReload = page.Locator(".map-popup__heart.is-saved, .map-popup__heart[aria-pressed=\"true\"]").First;
            Assert.True(await heartAfterReload.CountAsync() > 0, "Saved heart state should survive reload.");
        }

        var innerScroll = await page.EvaluateAsync<bool>(
            """
            () => {
              const body = document.querySelector('.map-cluster-card__body');
              if (!body) return false;
              return body.scrollHeight > body.clientHeight + 4;
            }
            """);
        if (touch)
        {
            Assert.False(innerScroll, "Cluster popup should not have inner scroll at 390.");
        }

        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(badConsole.Count == 0, "Console: " + string.Join("; ", badConsole));

        foreach (var path in new[] { "/candidate/paspoort", "/candidate/ontdekkingsreis", "/candidate/applications", "/carriere", "/profiel" })
        {
            await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.WaitForTimeoutAsync(400);
            var navCount = await page.Locator("[data-testid=candidate-bottom-nav-ssr], nav.bottom-nav").CountAsync();
            Assert.True(navCount > 0, "Bottom nav missing on " + path);
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var res = await http.GetAsync(baseUrl.TrimEnd('/') + "/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForMapReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && root.dataset.banenkaartMapReady === '1') return true;
              const canvas = document.querySelector('#job-map canvas, #job-map .maplibregl-canvas');
              return !!(canvas && canvas.getBoundingClientRect().height > 200);
            }
            """,
            null,
            new() { Timeout = 120_000 });
    }

    private static async Task TapFirstPinOrClusterAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            async () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map) return;
              if (window.jobMap.__testTapLargestCluster && window.jobMap.__testTapLargestCluster()) return;
              const canvas = map.getCanvas();
              const w = canvas.clientWidth || 390;
              const h = canvas.clientHeight || 400;
              for (let y = 80; y < h - 60; y += 24) {
                for (let x = 24; x < w - 24; x += 24) {
                  const feats = map.queryRenderedFeatures(new maplibregl.Point(x, y),
                    { layers: ['jobsy-pins-unclustered', 'jobsy-pins-clusters-hit'].filter(id => map.getLayer(id)) });
                  if (feats && feats.length) {
                    const rect = canvas.getBoundingClientRect();
                    canvas.dispatchEvent(new MouseEvent('click', {
                      bubbles: true, clientX: rect.left + x, clientY: rect.top + y, view: window
                    }));
                    return;
                  }
                }
              }
            }
            """);
    }
}
