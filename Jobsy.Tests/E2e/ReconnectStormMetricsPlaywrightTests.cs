using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Measures cookie header size and post-online request bursts across repeated offline cycles.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ReconnectStormMetricsPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
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

    [Fact]
    public async Task Three_offline_cycles_cookie_header_stable_and_online_burst_bounded()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var cycleMetrics = new List<object>();

        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        for (var cycle = 1; cycle <= 3; cycle++)
        {
            var burstStart = 0L;
            var burstRequests = new List<string>();
            var maxCookieBytes = 0;

            void OnRequest(object? _, IRequest req)
            {
                if (burstStart <= 0)
                {
                    return;
                }

                var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - burstStart;
                if (elapsed < 0 || elapsed > 4000)
                {
                    return;
                }

                var url = req.Url;
                if (url.Contains("negotiate", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("session-security", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("session-activity", StringComparison.OrdinalIgnoreCase))
                {
                    burstRequests.Add(req.Method + " " + url);
                }

                var cookie = req.Headers.TryGetValue("cookie", out var c) ? c : null;
                if (!string.IsNullOrEmpty(cookie))
                {
                    maxCookieBytes = Math.Max(maxCookieBytes, cookie.Length);
                }
            }

            page.Request += OnRequest;

            await context.SetOfflineAsync(true);
            await page.WaitForTimeoutAsync(30_000);
            burstStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await context.SetOfflineAsync(false);
            await page.WaitForFunctionAsync("() => navigator.onLine === true", null, new() { Timeout = 10_000 });

            await page.WaitForTimeoutAsync(5000);
            page.Request -= OnRequest;

            var cookieProbe = await page.EvaluateAsync<string>(
                """
                async () => {
                  const r = await fetch('/carriere', { method: 'HEAD', credentials: 'same-origin', cache: 'no-store' });
                  return String(r.status);
                }
                """);

            cycleMetrics.Add(new
            {
                cycle,
                maxCookieBytesDuringBurst = maxCookieBytes,
                burstRequestCount = burstRequests.Count,
                negotiateCalls = burstRequests.Count(u => u.Contains("negotiate", StringComparison.OrdinalIgnoreCase)),
                sessionCalls = burstRequests.Count(u =>
                    u.Contains("session-", StringComparison.OrdinalIgnoreCase)),
                carriereHeadStatus = cookieProbe
            });
        }

        var json = JsonSerializer.Serialize(cycleMetrics);
        foreach (var cycle in cycleMetrics)
        {
            var doc = JsonSerializer.SerializeToElement(cycle);
            var cookieBytes = doc.GetProperty("maxCookieBytesDuringBurst").GetInt32();
            var negotiate = doc.GetProperty("negotiateCalls").GetInt32();
            var burst = doc.GetProperty("burstRequestCount").GetInt32();
            Assert.True(cookieBytes < 12000, $"Cycle metrics: {json}. Cookie header too large ({cookieBytes} bytes).");
            Assert.True(negotiate <= 8, $"Cycle metrics: {json}. Too many parallel negotiate calls ({negotiate}).");
            Assert.True(burst <= 24, $"Cycle metrics: {json}. Session/reconnect burst too large ({burst}).");
        }
    }
}
