using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Acc-like Playwright guardrails: strict CSP, zero browser ApiBaseUrl calls, no Blazor circuit.
/// </summary>
public static class PlaywrightAccLikeRegression
{
    public const string AccLikeApiBaseUrl = "https://lobsy-acc-api.test/";

    public const string AccLikeApiHost = "lobsy-acc-api.test";

    public sealed class BrowserApiHostCounter
    {
        private int _count;

        public int Count => _count;

        public void Attach(IPage page)
        {
            page.Request += (_, req) =>
            {
                if (req.Url.Contains(AccLikeApiHost, StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref _count);
                }
            };
        }

        public void AssertZero(string scenario)
            => Xunit.Assert.True(
                _count == 0,
                $"Browser must not call ApiBaseUrl host ({AccLikeApiHost}) during {scenario}; saw {_count} request(s).");
    }

    public static BrowserApiHostCounter CreateApiHostGuard()
    {
        Environment.SetEnvironmentVariable("JOBSY_CSP_ALLOW_UNSAFE_EVAL", "0");
        return new BrowserApiHostCounter();
    }

    public static async Task BlockBlazorCircuitAsync(IPage page)
        => await page.RouteAsync("**/_blazor/**", route => route.AbortAsync());

    /// <summary>Strict acc-like guard: CSP off + API host counter + no Blazor circuit from first navigation.</summary>
    public static async Task<BrowserApiHostCounter> AttachAccLikePageAsync(IPage page, BrowserApiHostCounter? apiHostGuard = null)
    {
        apiHostGuard ??= CreateApiHostGuard();
        apiHostGuard.Attach(page);
        await BlockBlazorCircuitAsync(page);
        return apiHostGuard;
    }

    /// <summary>Block circuit after SSR/prerender shell is visible; actions under test must not use Blazor.</summary>
    public static async Task BlockBlazorAfterReadyAsync(IPage page)
        => await BlockBlazorCircuitAsync(page);
}
