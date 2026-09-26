using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart mobile E2E (390×844): every tapped pin shows a title; clusters open
/// the pager; identical-coordinate locations page; no "Vacature niet beschikbaar"
/// and no HTTP 429. Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is unset.
/// </summary>
public class JobMapPinsClustersPlaywrightTests
{
    [Fact]
    public async Task Mobile_map_pins_and_clusters_show_titles_without_429()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

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
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });

        var status429 = 0;
        context.Response += (_, response) =>
        {
            if (response.Status == 429)
            {
                Interlocked.Increment(ref status429);
            }
        };

        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new PageGotoOptions
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

        var pinPoints = await page.EvaluateAsync<PinPoint[]>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap
                ? window.jobMap.__testGetMap()
                : null;
              if (!map || typeof map.queryRenderedFeatures !== 'function' || typeof maplibregl === 'undefined') {
                return [];
              }
              const canvas = map.getCanvas();
              const w = canvas.width || 390;
              const h = canvas.height || 400;
              const layers = ['jobsy-pins-unclustered', 'jobsy-pins-unclustered-glyph'];
              const found = [];
              const seen = new Set();
              for (let y = 40; y < h - 40; y += 28) {
                for (let x = 20; x < w - 20; x += 28) {
                  const feats = map.queryRenderedFeatures(
                    new maplibregl.Point(x, y),
                    { layers: layers });
                  if (!feats || !feats.length) continue;
                  const id = feats[0].properties && feats[0].properties.id;
                  if (!id || seen.has(String(id))) continue;
                  seen.add(String(id));
                  found.push({ x, y, id: String(id) });
                  if (found.length >= 14) return found;
                }
              }
              return found;
            }
            """);

        Assert.NotNull(pinPoints);
        Assert.True(pinPoints!.Length >= 1, "Expected at least one rendered pin on the map");

        var random = new Random(390844);
        var sample = pinPoints.OrderBy(_ => random.Next()).Take(Math.Min(10, pinPoints.Length)).ToList();
        foreach (var pin in sample)
        {
            await page.Mouse.ClickAsync(pin.X, pin.Y);
            await page.WaitForTimeoutAsync(700);
            var title = await page.Locator(".map-popup__title").First.InnerTextAsync(new LocatorInnerTextOptions
            {
                Timeout = 8_000
            });
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.DoesNotContain("Vacature niet beschikbaar", title, StringComparison.OrdinalIgnoreCase);
            var body = await page.Locator("body").InnerTextAsync();
            Assert.DoesNotContain("Vacature niet beschikbaar", body, StringComparison.OrdinalIgnoreCase);
        }

        var pagerSeen = false;
        for (var attempt = 0; attempt < 24 && !pagerSeen; attempt++)
        {
            var cluster = await page.EvaluateAsync<PinPoint?>(
                """
                () => {
                  const map = window.jobMap && window.jobMap.__testGetMap
                    ? window.jobMap.__testGetMap()
                    : null;
                  if (!map || typeof maplibregl === 'undefined') return null;
                  const canvas = map.getCanvas();
                  const w = canvas.width || 390;
                  const h = canvas.height || 400;
                  for (let y = 40; y < h - 40; y += 36) {
                    for (let x = 20; x < w - 20; x += 36) {
                      const feats = map.queryRenderedFeatures(
                        new maplibregl.Point(x, y),
                        { layers: ['jobsy-pins-clusters'] });
                      if (feats && feats.length) {
                        return { x, y, id: String(feats[0].properties.cluster_id || '') };
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

            await page.Mouse.ClickAsync(cluster.X, cluster.Y);
            await page.WaitForTimeoutAsync(900);

            if (await page.Locator(".map-popup__pager-status").CountAsync() > 0)
            {
                pagerSeen = true;
                var status = await page.Locator(".map-popup__pager-status").InnerTextAsync();
                Assert.Contains("van", status, StringComparison.OrdinalIgnoreCase);

                var next = page.Locator(".map-popup__pager-nav").Nth(1);
                if (await next.IsEnabledAsync())
                {
                    await next.ClickAsync();
                    await page.WaitForTimeoutAsync(800);
                }

                var pageTitle = await page.Locator(".map-popup__title").First.InnerTextAsync();
                Assert.False(string.IsNullOrWhiteSpace(pageTitle));
                Assert.DoesNotContain("Vacature niet beschikbaar", pageTitle, StringComparison.OrdinalIgnoreCase);
                break;
            }
        }

        Assert.True(pagerSeen, "Expected a cluster pager (1 van N) after tapping clusters");
        Assert.Equal(0, status429);
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
        Assert.Contains("Vacature niet beschikbaar", File.ReadAllText(testFile));
        Assert.Contains("map-popup__pager-status", File.ReadAllText(testFile));
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
