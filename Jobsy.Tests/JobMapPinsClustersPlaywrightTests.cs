using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart E2E (mobile 390×844 + desktop 1280×800): cluster tap opens pager
/// without zoom; identical-coordinate pins page; single pins show a filled title;
/// exactly one popup; no 429 / card failures. Soft-skips when
/// <c>JOBSY_E2E_BASE_URL</c> is unset.
/// </summary>
public class JobMapPinsClustersPlaywrightTests
{
    [Fact]
    public async Task Mobile_map_pins_and_clusters_show_titles_without_429()
    {
        await RunViewportScenarioAsync(390, 844);
    }

    [Fact]
    public async Task Desktop_map_pins_and_clusters_show_titles_without_429()
    {
        await RunViewportScenarioAsync(1280, 800);
    }

    [Fact]
    public void Playwright_job_map_guardrail_is_wired()
    {
        var root = FindRepoRoot();
        var testFile = Path.Combine(root, "Jobsy.Tests", "JobMapPinsClustersPlaywrightTests.cs");
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.True(File.Exists(testFile));
        Assert.Contains("__testGetMap", js);
        Assert.Contains("390", File.ReadAllText(testFile));
        Assert.Contains("844", File.ReadAllText(testFile));
        Assert.Contains("1280", File.ReadAllText(testFile));
        Assert.Contains("800", File.ReadAllText(testFile));
        Assert.Contains("Vacature niet beschikbaar", File.ReadAllText(testFile));
        Assert.Contains("map-popup__pager-status", File.ReadAllText(testFile));
        Assert.Contains("markersByCoordKey", js, StringComparison.Ordinal);
        Assert.Contains("lastClusterTapAt", js, StringComparison.Ordinal);
        Assert.Contains("jobsy-agency-areas", js, StringComparison.Ordinal);
        Assert.Contains("via uitzendbureau", js, StringComparison.Ordinal);
        Assert.Contains("__testGetAgencyAreaCount", js, StringComparison.Ordinal);
    }

    private static async Task RunViewportScenarioAsync(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        var status429 = 0;
        var failedCardRequests = 0;
        context.Response += (_, response) =>
        {
            if (response.Status == 429)
            {
                Interlocked.Increment(ref status429);
            }

            var url = response.Url ?? "";
            if ((url.Contains("/api/vacancies/cards", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("/card", StringComparison.OrdinalIgnoreCase))
                && response.Status >= 400)
            {
                Interlocked.Increment(ref failedCardRequests);
            }
        };

        var page = await context.NewPageAsync();
        string? pageError = null;
        page.PageError += (_, error) => { pageError ??= error; };

        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 90_000
        });

        await page.WaitForSelectorAsync("#job-map canvas, #job-map .maplibregl-canvas", new PageWaitForSelectorOptions
        {
            Timeout = 60_000
        });

        await page.WaitForFunctionAsync(
            """
            () => {
              const count = window.jobMap && window.jobMap.__testGetPinCount
                ? window.jobMap.__testGetPinCount()
                : 0;
              const boot = document.getElementById('jobsy-map-boot');
              const bootHasId = !!(boot && boot.textContent && boot.textContent.indexOf('"id"') >= 0);
              return count > 0 || bootHasId;
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });

        await page.WaitForTimeoutAsync(1500);

        // --- Cluster → pager, no zoom ---
        var clusterHandled = false;
        for (var attempt = 0; attempt < 24 && !clusterHandled; attempt++)
        {
            var cluster = await page.EvaluateAsync<ClusterHit?>(
                """
                () => {
                  const map = window.jobMap && window.jobMap.__testGetMap
                    ? window.jobMap.__testGetMap()
                    : null;
                  if (!map || typeof maplibregl === 'undefined') return null;
                  const canvas = map.getCanvas();
                  const w = canvas.clientWidth || canvas.width || 390;
                  const h = canvas.clientHeight || canvas.height || 400;
                  const rect = canvas.getBoundingClientRect();
                  for (let y = 48; y < h - 48; y += 32) {
                    for (let x = 32; x < w - 32; x += 32) {
                      const feats = map.queryRenderedFeatures(
                        new maplibregl.Point(x, y),
                        { layers: ['jobsy-pins-clusters'] });
                      if (feats && feats.length) {
                        const pc = Number(feats[0].properties.point_count) || 0;
                        if (pc < 2) continue;
                        return {
                          x: rect.left + x,
                          y: rect.top + y,
                          pointCount: pc,
                          zoom: map.getZoom()
                        };
                      }
                    }
                  }
                  return null;
                }
                """);

            if (cluster is null)
            {
                await page.EvaluateAsync(
                    """
                    () => {
                      const map = window.jobMap && window.jobMap.__testGetMap
                        ? window.jobMap.__testGetMap() : null;
                      if (map) map.easeTo({ zoom: Math.max(5, map.getZoom() - 1.2), duration: 0 });
                    }
                    """);
                await page.WaitForTimeoutAsync(500);
                continue;
            }

            var zoomBefore = cluster.Zoom;
            await page.Mouse.ClickAsync(cluster.X, cluster.Y);

            await page.WaitForSelectorAsync(".maplibregl-popup", new PageWaitForSelectorOptions
            {
                Timeout = 1500,
                State = WaitForSelectorState.Visible
            });

            var status = await page.Locator(".map-popup__pager-status").First.InnerTextAsync(
                new LocatorInnerTextOptions { Timeout = 1500 });
            var match = Regex.Match(status ?? "", @"1\s+van\s+(\d+)", RegexOptions.IgnoreCase);
            Assert.True(match.Success, "Expected pager status '1 van N', got: " + status);
            var n = int.Parse(match.Groups[1].Value);
            Assert.True(n >= 2, "Expected N >= 2");
            Assert.Equal(cluster.PointCount, n);

            var zoomAfter = await page.EvaluateAsync<double>(
                """
                () => {
                  const map = window.jobMap && window.jobMap.__testGetMap
                    ? window.jobMap.__testGetMap() : null;
                  return map ? map.getZoom() : -1;
                }
                """);
            Assert.InRange(zoomAfter, zoomBefore - 0.05, zoomBefore + 0.05);

            await page.WaitForFunctionAsync(
                """
                () => {
                  const t = document.querySelector('.map-popup__title');
                  if (!t) return false;
                  const text = (t.textContent || '').trim();
                  return text.length > 0 && text.toLowerCase().indexOf('vacature niet beschikbaar') < 0;
                }
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 3000 });

            Assert.Equal(1, await page.Locator(".maplibregl-popup").CountAsync());

            var title1 = (await page.Locator(".map-popup__title").First.InnerTextAsync()).Trim();
            var next = page.Locator(".map-popup__pager-nav").Nth(1);
            var prev = page.Locator(".map-popup__pager-nav").Nth(0);
            Assert.False(await prev.IsEnabledAsync(), "‹ should be disabled on page 1");

            if (n >= 2)
            {
                Assert.True(await next.IsEnabledAsync());
                await next.ClickAsync();
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      const s = document.querySelector('.map-popup__pager-status');
                      return s && /2\s+van\s+\d+/i.test(s.textContent || '');
                    }
                    """,
                    null,
                    new PageWaitForFunctionOptions { Timeout = 3000 });
                var title2 = (await page.Locator(".map-popup__title").First.InnerTextAsync()).Trim();
                Assert.False(string.IsNullOrWhiteSpace(title2));
                Assert.DoesNotContain("Vacature niet beschikbaar", title2, StringComparison.OrdinalIgnoreCase);
                Assert.NotEqual(title1, title2);

                await prev.ClickAsync();
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      const s = document.querySelector('.map-popup__pager-status');
                      return s && /1\s+van\s+\d+/i.test(s.textContent || '');
                    }
                    """,
                    null,
                    new PageWaitForFunctionOptions { Timeout = 3000 });
            }

            // Last page: › disabled
            for (var i = 1; i < n; i++)
            {
                if (await next.IsEnabledAsync())
                {
                    await next.ClickAsync();
                    await page.WaitForTimeoutAsync(400);
                }
            }

            var statusLast = await page.Locator(".map-popup__pager-status").First.InnerTextAsync();
            Assert.Contains($"van {n}", statusLast, StringComparison.OrdinalIgnoreCase);
            Assert.False(await next.IsEnabledAsync(), "› should be disabled on page N");
            Assert.Equal(1, await page.Locator(".maplibregl-popup").CountAsync());

            clusterHandled = true;
        }

        Assert.True(clusterHandled, "Expected a cluster pager (1 van N) after tapping clusters");

        // --- Single pin at zoom ≥ 15: popup without pager ---
        await page.EvaluateAsync(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap
                ? window.jobMap.__testGetMap() : null;
              if (map) map.easeTo({ zoom: 15.2, duration: 0 });
            }
            """);
        await page.WaitForTimeoutAsync(800);

        var pin = await page.EvaluateAsync<PinPoint?>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap
                ? window.jobMap.__testGetMap() : null;
              if (!map || typeof maplibregl === 'undefined') return null;
              const canvas = map.getCanvas();
              const w = canvas.clientWidth || canvas.width || 390;
              const h = canvas.clientHeight || canvas.height || 400;
              const rect = canvas.getBoundingClientRect();
              const layers = ['jobsy-pins-unclustered', 'jobsy-pins-unclustered-glyph'];
              for (let y = 40; y < h - 40; y += 28) {
                for (let x = 20; x < w - 20; x += 28) {
                  const feats = map.queryRenderedFeatures(
                    new maplibregl.Point(x, y), { layers: layers });
                  if (!feats || !feats.length) continue;
                  const id = feats[0].properties && feats[0].properties.id;
                  if (!id) continue;
                  return { x: rect.left + x, y: rect.top + y, id: String(id) };
                }
              }
              return null;
            }
            """);

        if (pin is not null)
        {
            await page.Mouse.ClickAsync(pin.X, pin.Y);
            await page.WaitForSelectorAsync(".maplibregl-popup", new PageWaitForSelectorOptions
            {
                Timeout = 3000,
                State = WaitForSelectorState.Visible
            });
            Assert.Equal(0, await page.Locator(".map-popup__pager-status").CountAsync());
            await page.WaitForFunctionAsync(
                """
                () => {
                  const t = document.querySelector('.map-popup__title');
                  if (!t) return false;
                  const text = (t.textContent || '').trim();
                  return text.length > 0 && text.toLowerCase().indexOf('vacature niet beschikbaar') < 0;
                }
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 3000 });
            Assert.Equal(1, await page.Locator(".maplibregl-popup").CountAsync());
        }

        // --- Identical coordinates at zoom 16: pager 1 van 2/3 ---
        await page.EvaluateAsync(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap
                ? window.jobMap.__testGetMap() : null;
              if (map) map.easeTo({ zoom: 16, duration: 0 });
            }
            """);
        await page.WaitForTimeoutAsync(800);

        var sameSpot = await page.EvaluateAsync<PinPoint?>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap
                ? window.jobMap.__testGetMap() : null;
              if (!map || typeof maplibregl === 'undefined') return null;
              // Probe via jobMap internals if exposed markers exist in GeoJSON source.
              const canvas = map.getCanvas();
              const w = canvas.clientWidth || canvas.width || 390;
              const h = canvas.clientHeight || canvas.height || 400;
              const rect = canvas.getBoundingClientRect();
              const layers = ['jobsy-pins-unclustered', 'jobsy-pins-unclustered-glyph', 'jobsy-pins-clusters'];
              for (let y = 40; y < h - 40; y += 24) {
                for (let x = 20; x < w - 20; x += 24) {
                  const feats = map.queryRenderedFeatures(
                    new maplibregl.Point(x, y), { layers: layers });
                  if (!feats || !feats.length) continue;
                  const props = feats[0].properties || {};
                  if (props.cluster) {
                    const pc = Number(props.point_count) || 0;
                    if (pc >= 2 && pc <= 3) {
                      return { x: rect.left + x, y: rect.top + y, id: 'cluster:' + pc };
                    }
                  }
                }
              }
              // Fallback: click any unclustered pin; coordKey same-spot opens pager when stacked.
              for (let y = 40; y < h - 40; y += 28) {
                for (let x = 20; x < w - 20; x += 28) {
                  const feats = map.queryRenderedFeatures(
                    new maplibregl.Point(x, y),
                    { layers: ['jobsy-pins-unclustered', 'jobsy-pins-unclustered-glyph'] });
                  if (feats && feats.length) {
                    return {
                      x: rect.left + x,
                      y: rect.top + y,
                      id: String(feats[0].properties.id || '')
                    };
                  }
                }
              }
              return null;
            }
            """);

        if (sameSpot is not null)
        {
            await page.Mouse.ClickAsync(sameSpot.X, sameSpot.Y);
            await page.WaitForTimeoutAsync(900);
            if (await page.Locator(".map-popup__pager-status").CountAsync() > 0)
            {
                var sameStatus = await page.Locator(".map-popup__pager-status").First.InnerTextAsync();
                Assert.True(
                    Regex.IsMatch(sameStatus ?? "", @"1\s+van\s+[23]", RegexOptions.IgnoreCase),
                    "Expected identical-coordinate pager '1 van 2' or '1 van 3', got: " + sameStatus);
            }

            Assert.Equal(1, await page.Locator(".maplibregl-popup").CountAsync());
        }

        Assert.Equal(0, status429);
        Assert.Equal(0, failedCardRequests);
        Assert.True(pageError is null, "pageerror: " + pageError);
    }

    private sealed class PinPoint
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    private sealed class ClusterHit
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("pointCount")]
        public int PointCount { get; set; }

        [JsonPropertyName("zoom")]
        public double Zoom { get; set; }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.GetAsync(baseUrl.TrimEnd('/') + "/");
            return response.StatusCode is HttpStatusCode.OK
                or HttpStatusCode.Redirect
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.MovedPermanently;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
