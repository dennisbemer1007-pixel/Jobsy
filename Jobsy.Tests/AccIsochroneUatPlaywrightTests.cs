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
                var row = await RunCaseAsync(browser, baseUrl, prefix, tag, transport, width, height, maxMinutes);
                rows.Add(row);
            }
            catch (Exception ex)
            {
                failures.Add($"{tag}: {ex.Message}");
                rows.Add(new { tag, error = ex.Message });
            }
        }

        var metrics = new Dictionary<string, object>
        {
            ["acc_head"] = accHead,
            ["artifact_prefix"] = prefix,
            ["cases"] = rows,
            ["failures"] = failures
        };
        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(ArtifactDir, "acc-isochrone-uat-metrics.json"), json);
        await File.WriteAllTextAsync(Path.Combine(AgentStoreDir, "acc-isochrone-uat-metrics.json"), json);

        if (failures.Count > 0)
        {
            Assert.Fail("Isochrone UAT failures: " + string.Join("; ", failures));
        }
    }

    private static async Task<object> RunCaseAsync(
        IBrowser browser,
        string baseUrl,
        string prefix,
        string tag,
        string transport,
        int width,
        int height,
        int maxMinutes)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await ctx.NewPageAsync();
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
        var domMs = sw.ElapsedMilliseconds;

        await page.WaitForFunctionAsync(
            """
            () => {
              const el = document.querySelector('.map-pane[data-iso-mode], #job-map[data-iso-mode]');
              if (!el) return false;
              const m = el.getAttribute('data-iso-mode');
              return m && m !== 'loading';
            }
            """,
            null,
            new() { Timeout = 90_000 });
        var isoReadyMs = sw.ElapsedMilliseconds;

        await page.WaitForTimeoutAsync(1500);

        var isoMode = await page.EvaluateAsync<string>(
            """
            () => {
              const el = document.querySelector('.map-pane[data-iso-mode], #job-map[data-iso-mode]');
              return el ? (el.getAttribute('data-iso-mode') || '') : 'missing';
            }
            """);
        var ringSummary = await page.EvaluateAsync<string>(
            """
            () => {
              try {
                const m = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                if (!m || !m.getSource('jobsy-travel-rings')) return 'no-source';
                const src = m.getSource('jobsy-travel-rings');
                const data = src._data || (src.serialize && src.serialize());
                const g = data && data.features && data.features[0] && data.features[0].geometry;
                if (!g || !g.coordinates) return 'empty';
                const ring = g.coordinates[0];
                return g.type + ':' + (ring ? ring.length : 0);
              } catch (e) { return 'err:' + e.message; }
            }
            """);
        var listCount = await page.Locator(".kb-list-row, .banenkaart-ssr-list__row, [data-testid='kb-list-row']").CountAsync();

        var screenshot = $"{prefix}-{tag}.png";
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, screenshot), FullPage = false });
        File.Copy(Path.Combine(ArtifactDir, screenshot), Path.Combine(AgentStoreDir, screenshot), overwrite: true);

        var isOv = string.Equals(transport, "OV", StringComparison.OrdinalIgnoreCase);
        if (isOv)
        {
            Assert.Equal("none", isoMode);
            Assert.DoesNotContain(200, isoStatuses);
            Assert.True(ringSummary is "no-source" or "empty", "OV must not draw travel rings: " + ringSummary);
        }
        else
        {
            Assert.Contains(200, isoStatuses);
            Assert.Equal("real", isoMode);
            Assert.StartsWith("Polygon:", ringSummary, StringComparison.Ordinal);
            var parts = ringSummary.Split(':');
            Assert.True(parts.Length > 1 && int.TryParse(parts[1], out var pts) && pts > 20,
                "Expected Valhalla polygon vertex count, got " + ringSummary);
        }

        return new
        {
            tag,
            transport,
            width,
            maxMinutes,
            dom_content_loaded_ms = domMs,
            iso_mode_ready_ms = isoReadyMs,
            iso_mode = isoMode,
            isochrone_http_statuses = isoStatuses,
            ring_summary = ringSummary,
            list_row_count = listCount,
            screenshot
        };
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
}
