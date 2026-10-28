using System.Diagnostics;
using System.Text.Json;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acceptatie isochroon UAT after #1324: auto/fiets show Valhalla polygon (no circle); OV has no line, filtered pins only.
/// Run: JOBSY_ACC_ISO_UAT=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl dotnet test --filter Acc_isochrone_uat
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AccIsochroneUatPlaywrightTests
{
    private const string ArtifactDir = "/opt/cursor/artifacts";
    private const string AgentStoreDir = "/cursor/stores/self/artifacts";

    [Fact]
    public async Task Acc_isochrone_uat_auto_fiets_ov_1440_390()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ACC_ISO_UAT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "https://acceptatie.lobsy.nl";
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            Assert.Fail("Acceptatie unreachable at " + baseUrl);
        }

        Directory.CreateDirectory(ArtifactDir);
        Directory.CreateDirectory(AgentStoreDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var accHead = await ResolveAcceptatieCommitAsync(baseUrl);
        var prefix = $"acc-{accHead}-isochroon";
        var rows = new List<object>();
        var failures = new List<string>();
        var clickByTag = new Dictionary<string, AccPageClickabilityAudit.ViewportResult>();

        var cases = new (string transport, int width, int height, int maxMinutes)[]
        {
            ("Auto", 1440, 900, 60),
            ("Auto", 390, 844, 60),
            ("Fiets", 1440, 900, 30),
            ("Fiets", 390, 844, 30),
            ("OV", 1440, 900, 30),
            ("OV", 390, 844, 30),
        };

        foreach (var (transport, width, height, maxMinutes) in cases)
        {
            var tag = $"{transport.ToLowerInvariant()}-{width}";
            try
            {
                var (row, caseFails) = await RunCaseAsync(
                    browser, baseUrl, prefix, tag, transport, width, height, maxMinutes, clickByTag);
                rows.Add(row);
                foreach (var f in caseFails)
                {
                    failures.Add(f);
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{tag}: {ex.Message}");
                rows.Add(new { tag, error = ex.Message });
            }
        }

        var clickReports = new List<object>();
        foreach (var transport in new[] { "auto", "fiets", "ov" })
        {
            if (!clickByTag.TryGetValue($"{transport}-1440", out var wide)
                || !clickByTag.TryGetValue($"{transport}-390", out var narrow))
            {
                failures.Add($"click-audit-{transport}: missing viewport run");
                continue;
            }

            var merged = AccPageClickabilityAudit.MergeViewports(wide, narrow);
            var reportBase = $"{prefix}-click-{transport}";
            AccPageClickabilityAudit.WriteReport(
                ArtifactDir,
                reportBase,
                $"Banenkaart isochroon ({transport})",
                merged,
                wide.SkippedDangerous.Concat(narrow.SkippedDangerous).ToList(),
                accHead);
            TryCopyToAgentStore(
                Path.Combine(ArtifactDir, reportBase + ".md"),
                Path.Combine(AgentStoreDir, reportBase + ".md"));
            TryCopyToAgentStore(
                Path.Combine(ArtifactDir, reportBase + ".json"),
                Path.Combine(AgentStoreDir, reportBase + ".json"));
            clickReports.Add(new
            {
                transport,
                element_count = merged.Count,
                report = reportBase,
                elements = merged.Select(r => new
                {
                    r.Element,
                    r.Kind,
                    r.Result1440,
                    r.Result390,
                    r.Skipped
                }).ToList()
            });
            try
            {
                AccPageClickabilityAudit.AssertAllOk(merged, $"Banenkaart isochroon {transport}");
            }
            catch (Exception ex)
            {
                failures.Add($"click-audit-{transport}: {ex.Message}");
            }
        }

        var metrics = new Dictionary<string, object>
        {
            ["acc_head"] = accHead,
            ["artifact_prefix"] = prefix,
            ["cases"] = rows,
            ["click_audit"] = clickReports,
            ["failures"] = failures
        };
        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(ArtifactDir, "acc-isochrone-uat-metrics.json"), json);
        TryCopyToAgentStore(
            Path.Combine(ArtifactDir, "acc-isochrone-uat-metrics.json"),
            Path.Combine(AgentStoreDir, "acc-isochrone-uat-metrics.json"));

        if (failures.Count > 0)
        {
            Assert.Fail("Isochrone UAT failures: " + string.Join("; ", failures));
        }
    }

    private static async Task<(object Row, List<string> Failures)> RunCaseAsync(
        IBrowser browser,
        string baseUrl,
        string prefix,
        string tag,
        string transport,
        int width,
        int height,
        int maxMinutes,
        Dictionary<string, AccPageClickabilityAudit.ViewportResult> clickByTag)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        const string seedEmail = "onboarding.e2e@jobsy.local";
        var seedPassword = (Environment.GetEnvironmentVariable("JOBSY_E2E_PASSWORD")
                            ?? Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD")
                            ?? KbE2e.DefaultPassword).Trim();
        if (!await KbE2e.TryLoginAsync(page, baseUrl, seedEmail, seedPassword))
        {
            Assert.Fail("Seed candidate login failed on acceptatie.");
        }

        await page.EvaluateAsync(
            """
            () => {
              window.__jobsyAccFilterAppliedMs = null;
              window.__jobsyAccNavStart = performance.now();
              document.addEventListener('jobsy-bk-filters-applied', () => {
                if (window.__jobsyAccFilterAppliedMs == null) {
                  window.__jobsyAccFilterAppliedMs = Math.round(performance.now() - window.__jobsyAccNavStart);
                }
              }, { once: true });
            }
            """);

        var isoStatuses = new List<int>();
        page.Response += (_, res) =>
        {
            if (res.Url.Contains("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
            {
                isoStatuses.Add(res.Status);
            }
        };

        var path = "/banenkaart?originLat=52.3676&originLng=4.9041&originLabel=Amsterdam"
                   + $"&maxMinutes={maxMinutes}&transport={Uri.EscapeDataString(transport)}&weergave=kaart";
        var sw = Stopwatch.StartNew();
        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var domMs = sw.ElapsedMilliseconds;

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              const canvas = document.querySelector('#job-map canvas.maplibregl-canvas');
              const mapReady = root && root.dataset.banenkaartMapReady === '1';
              const painted = canvas && canvas.width > 0 && canvas.height > 0;
              return mapReady || painted;
            }
            """,
            null,
            new() { Timeout = 90_000 });
        var mapReadyMs = sw.ElapsedMilliseconds;

        var expectReal = !string.Equals(transport, "OV", StringComparison.OrdinalIgnoreCase);
        await page.WaitForFunctionAsync(
            expectReal
                ? """
                  () => {
                    const el = document.querySelector('.map-pane[data-iso-mode], #job-map[data-iso-mode]');
                    return el && el.getAttribute('data-iso-mode') === 'real';
                  }
                  """
                : """
                  () => {
                    const el = document.querySelector('.map-pane[data-iso-mode], #job-map[data-iso-mode]');
                    return el && el.getAttribute('data-iso-mode') === 'none';
                  }
                  """,
            null,
            new() { Timeout = 120_000 });
        var isoReadyMs = sw.ElapsedMilliseconds;

        await page.WaitForTimeoutAsync(400);

        var isoMode = await page.EvaluateAsync<string>(
            """
            () => {
              const el = document.querySelector('.map-pane[data-iso-mode], #job-map[data-iso-mode]');
              return el ? (el.getAttribute('data-iso-mode') || '') : 'missing';
            }
            """);
        var ringFeatureCount = await page.EvaluateAsync<int>(
            """
            () => window.jobMap && typeof window.jobMap.__testGetTravelRingFeatureCount === 'function'
              ? window.jobMap.__testGetTravelRingFeatureCount() : -1
            """);
        var renderedPinFeatures = await page.EvaluateAsync<int>(
            """
            () => {
              try {
                const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                if (!map) return 0;
                const layers = ['jobsy-pins-clusters', 'jobsy-pins-unclustered', 'jobsy-pins-clusters-hit'];
                return (map.queryRenderedFeatures({ layers: layers.filter(id => map.getLayer(id)) }) || []).length;
              } catch (e) { return 0; }
            }
            """);
        var listCount = await page.Locator(".kb-list-row, .banenkaart-ssr-list__row, [data-testid='kb-list-row']").CountAsync();

        var filterReadyMs = await page.EvaluateAsync<int?>(
            """
            () => {
              const v = window.__jobsyAccFilterAppliedMs;
              return v == null ? null : Number(v);
            }
            """);
        if (filterReadyMs is null)
        {
            filterReadyMs = (int)Math.Min(mapReadyMs, int.MaxValue);
        }

        if (string.Equals(transport, "OV", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      try {
                        const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                        if (!map) return false;
                        const layers = ['jobsy-pins-clusters', 'jobsy-pins-unclustered', 'jobsy-pins-clusters-hit']
                          .filter(id => map.getLayer(id));
                        return (map.queryRenderedFeatures({ layers }) || []).length > 0;
                      } catch (e) { return false; }
                    }
                    """,
                    null,
                    new() { Timeout = 20_000 });
            }
            catch (TimeoutException)
            {
            }

            renderedPinFeatures = await page.EvaluateAsync<int>(
                """
                () => {
                  try {
                    const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                    if (!map) return 0;
                    const layers = ['jobsy-pins-clusters', 'jobsy-pins-unclustered', 'jobsy-pins-clusters-hit'];
                    return (map.queryRenderedFeatures({ layers: layers.filter(id => map.getLayer(id)) }) || []).length;
                  } catch (e) { return 0; }
                }
                """);
        }

        var screenshot = $"{prefix}-{tag}.png";
        var shotPath = Path.Combine(ArtifactDir, screenshot);
        var mapShot = page.Locator("#job-map").First;
        if (await mapShot.CountAsync() > 0)
        {
            await mapShot.ScreenshotAsync(new() { Path = shotPath });
        }
        else
        {
            await page.ScreenshotAsync(new() { Path = shotPath, FullPage = false });
        }

        TryCopyToAgentStore(shotPath, Path.Combine(AgentStoreDir, screenshot));

        clickByTag[tag] = await AccPageClickabilityAudit.AuditPageAsync(page, tag);

        var emptyStrip = await AccLayoutEmptyStripAudit.MeasureBanenkaartAsync(page);
        var caseFails = new List<string>();

        if (emptyStrip.largest_empty_strip_px > AccLayoutEmptyStripAudit.MaxAllowedEmptyStripPx)
        {
            caseFails.Add(
                $"{tag}: Layout empty strip too large ({emptyStrip.largest_empty_strip_px}px > "
                + $"{AccLayoutEmptyStripAudit.MaxAllowedEmptyStripPx}px)");
        }

        var isOv = string.Equals(transport, "OV", StringComparison.OrdinalIgnoreCase);
        if (isOv)
        {
            if (isoMode != "none")
            {
                caseFails.Add($"{tag}: OV iso_mode expected none, got {isoMode}");
            }

            if (isoStatuses.Contains(200))
            {
                caseFails.Add($"{tag}: OV must not fetch isochrones (saw HTTP 200)");
            }

            if (ringFeatureCount > 0)
            {
                caseFails.Add($"{tag}: OV must not draw travel rings, count={ringFeatureCount}");
            }

            if (renderedPinFeatures <= 0)
            {
                caseFails.Add($"{tag}: OV map must show filtered vacancy pins, rendered={renderedPinFeatures}");
            }
        }
        else
        {
            if (!isoStatuses.Contains(200))
            {
                caseFails.Add($"{tag}: Expected isochrone HTTP 200, got [{string.Join(",", isoStatuses)}]");
            }

            if (isoMode != "real")
            {
                caseFails.Add($"{tag}: Expected iso_mode real, got {isoMode}");
            }

            if (ringFeatureCount <= 0)
            {
                caseFails.Add($"{tag}: Expected travel ring features, count={ringFeatureCount}");
            }
        }

        if (mapReadyMs > 3000)
        {
            caseFails.Add($"{tag}: map_ready_ms {mapReadyMs} > 3000");
        }

        if (isoReadyMs > 3000)
        {
            caseFails.Add($"{tag}: iso_mode_ready_ms {isoReadyMs} > 3000");
        }

        if (filterReadyMs > 500)
        {
            caseFails.Add($"{tag}: filter_apply_ms {filterReadyMs} > 500");
        }

        var row = new
        {
            tag,
            transport,
            width,
            maxMinutes,
            dom_content_loaded_ms = domMs,
            map_ready_ms = mapReadyMs,
            map_ready_target_ms = 1000,
            iso_mode_ready_ms = isoReadyMs,
            filter_apply_ms = filterReadyMs,
            iso_mode = isoMode,
            isochrone_http_statuses = isoStatuses,
            travel_ring_feature_count = ringFeatureCount,
            rendered_pin_features = renderedPinFeatures,
            list_row_count = listCount,
            largest_empty_strip_px = emptyStrip.largest_empty_strip_px,
            empty_holder_px = emptyStrip.empty_holder_px,
            band_gap_px = emptyStrip.band_gap_px,
            screenshot,
            case_failures = caseFails
        };

        return (row, caseFails);
    }

    private static async Task<string> ResolveAcceptatieCommitAsync(string baseUrl)
    {
        var env = (Environment.GetEnvironmentVariable("JOBSY_ACC_HEAD") ?? "").Trim();
        if (env.Length is >= 7 and <= 40)
        {
            return env.Length > 12 ? env[..12] : env;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var html = await http.GetStringAsync(baseUrl.TrimEnd('/') + "/login");
        const string marker = "name=\"lobsy-commit\" content=\"";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return "unknown";
        }

        start += marker.Length;
        var end = html.IndexOf('"', start);
        if (end <= start)
        {
            return "unknown";
        }

        var full = html[start..end].Trim();
        return full.Length > 12 ? full[..12] : full;
    }

    private static void TryCopyToAgentStore(string source, string dest)
    {
        try
        {
            if (File.Exists(source))
            {
                File.Copy(source, dest, overwrite: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
