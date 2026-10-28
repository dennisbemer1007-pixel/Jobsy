using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class AccIsochroneProbePlaywrightTests
{
    [Fact]
    public async Task Acc_probe_isochrone_network_and_mode()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ACC_ISO_PROBE") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "https://acceptatie.lobsy.nl";
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });

        var cases = new[]
        {
            ("guest-auto-amsterdam-1440", 1440, 900, false,
                "/banenkaart?originLat=52.3676&originLng=4.9041&originLabel=Amsterdam&maxMinutes=60&transport=Auto&weergave=kaart"),
            ("guest-fiets-denhaag-390", 390, 844, false,
                "/banenkaart?originLat=52.0705&originLng=4.3007&originLabel=Den%20Haag&maxMinutes=20&transport=Fiets&weergave=kaart"),
            ("guest-ov-amsterdam-1440", 1440, 900, false,
                "/banenkaart?originLat=52.3676&originLng=4.9041&originLabel=Amsterdam&maxMinutes=30&transport=OV&weergave=kaart"),
        };

        foreach (var (tag, w, h, login, path) in cases)
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                IgnoreHTTPSErrors = true
            });
            var page = await ctx.NewPageAsync();
            var isoCalls = new List<int>();
            page.Response += (_, res) =>
            {
                if (res.Url.Contains("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
                {
                    isoCalls.Add(res.Status);
                }
            };

            if (login)
            {
                _ = await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, KbE2e.DefaultPassword);
            }

            await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await page.WaitForTimeoutAsync(12_000);

            var isoMode = await page.EvaluateAsync<string>(
                """
                () => {
                  const pane = document.querySelector('.map-pane[data-iso-mode]');
                  if (pane) return pane.getAttribute('data-iso-mode') || 'missing';
                  const map = document.querySelector('#job-map[data-iso-mode], .map-pane [data-iso-mode]');
                  return map ? map.getAttribute('data-iso-mode') : 'missing';
                }
                """);
            var ringType = await page.EvaluateAsync<string>(
                """
                () => {
                  try {
                    const m = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                    if (!m || !m.getSource('jobsy-travel-rings')) return 'no-source';
                    const data = m.getSource('jobsy-travel-rings')._data || m.getSource('jobsy-travel-rings').serialize();
                    const g = data && data.features && data.features[0] && data.features[0].geometry;
                    if (!g) return 'no-geom';
                    const c = g.coordinates && g.coordinates[0] && g.coordinates[0][0];
                    if (!c || !c.length) return g.type || 'unknown';
                    return g.type + ':' + c.length;
                  } catch (e) { return 'err'; }
                }
                """);
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/acc-probe-{tag}.png",
                FullPage = false
            });
            await File.AppendAllTextAsync(
                "/opt/cursor/artifacts/acc-isochrone-probe.jsonl",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    tag,
                    isoCalls,
                    isoMode,
                    ringType
                }) + "\n");

            if (tag.Contains("ov", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("none", isoMode);
            }
            else if (tag.Contains("auto", StringComparison.OrdinalIgnoreCase) || tag.Contains("fiets", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains(200, isoCalls);
                Assert.Equal("real", isoMode);
            }
        }
    }
}
