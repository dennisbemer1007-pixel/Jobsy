using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Acc-like Playwright guardrails: strict CSP (same as acc — no JOBSY_CSP_ALLOW_UNSAFE_EVAL) and a
/// distinct ApiBaseUrl host that must never be contacted from the browser (BFF / same-origin only).
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
}
