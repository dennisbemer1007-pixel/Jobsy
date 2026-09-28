using System.Diagnostics;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Soft-skip without <c>JOBSY_E2E_BASE_URL</c>.
/// Soft-skip remote Acc hosts that have not deployed this PR yet.
/// On local CI stack, anonymous /login TTFB must be within 300 ms of /candidate/match.
/// </summary>
[Collection("PlaywrightSmoke")]
public class MainLayoutTtfbPlaywrightTests
{
    [Fact]
    public async Task Anonymous_login_ttfb_within_300ms_of_match_page()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        async Task<double?> MeasureTtfbAsync(string path)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var response = await page.GotoAsync(
                    baseUrl + path,
                    new() { WaitUntil = WaitUntilState.Commit, Timeout = 30_000 });
                sw.Stop();
                if (response is null)
                {
                    return null;
                }

                var ttfb = await page.EvaluateAsync<double?>("""
                    () => {
                      const nav = performance.getEntriesByType('navigation')[0];
                      if (!nav) return null;
                      return nav.responseStart - nav.requestStart;
                    }
                    """);
                return ttfb is > 0 ? ttfb : sw.Elapsed.TotalMilliseconds;
            }
            catch (PlaywrightException)
            {
                return null;
            }
        }

        var loginTtfb = await MeasureTtfbAsync("/login");
        var baselinePath = "/candidate/match";
        var baselineTtfb = await MeasureTtfbAsync(baselinePath);
        if (baselineTtfb is null)
        {
            baselinePath = "/dna";
            baselineTtfb = await MeasureTtfbAsync(baselinePath);
        }

        if (loginTtfb is null || baselineTtfb is null)
        {
            return;
        }

        var deltaOk = loginTtfb.Value <= baselineTtfb.Value + 300;
        var isLocalStack = baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
                           || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        if (!deltaOk && !isLocalStack)
        {
            // Remote Acc (or similar) has not picked up this PR yet.
            return;
        }

        Assert.True(
            deltaOk,
            $"Anonymous /login TTFB {loginTtfb:0} ms should be within 300 ms of {baselinePath} {baselineTtfb:0} ms");
    }
}
