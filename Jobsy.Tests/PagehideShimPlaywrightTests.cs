using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Idempotent unload→pagehide shim: two enhanced navigations must not stack the wrapper.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class PagehideShimPlaywrightTests
{
    [Fact]
    public async Task Two_enhanced_navigations_do_not_stack_pagehide_shim()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var pageErrors = new ConcurrentBag<string>();
        page.PageError += (_, err) => pageErrors.Add(err);

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1_000);

        var firstAdd = await page.EvaluateAsync<string>(
            "() => EventTarget.prototype.addEventListener.toString()");

        await page.EvaluateAsync("""
            () => {
              const fn = function () {};
              window.__jobsyShimProbe = fn;
              window.addEventListener('unload', fn);
              window.removeEventListener('unload', fn);
            }
            """);

        // Prefer in-app link navigation (enhanced) over GotoAsync.
        var vacancyLink = page.Locator("a[href*='/vacancies/']").First;
        if (await vacancyLink.CountAsync() > 0)
        {
            await vacancyLink.ClickAsync(new() { Timeout = 10_000 });
            await page.WaitForTimeoutAsync(1_500);
            await page.EvaluateAsync("""
                () => {
                  const fn = window.__jobsyShimProbe || function () {};
                  window.addEventListener('unload', fn);
                  window.removeEventListener('unload', fn);
                }
                """);

            var back = page.Locator("a[href='/'], a[href='/banenkaart'], a.brand, a.jobsy-logo").First;
            if (await back.CountAsync() > 0)
            {
                await back.ClickAsync(new() { Timeout = 10_000 });
                await page.WaitForTimeoutAsync(1_500);
            }
            else
            {
                await page.GoBackAsync();
                await page.WaitForTimeoutAsync(1_500);
            }
        }
        else
        {
            // Fallback: navigate to login and back via links when the map has no vacancy links yet.
            var login = page.Locator("a[href='/login']").First;
            if (await login.CountAsync() > 0)
            {
                await login.ClickAsync();
                await page.WaitForTimeoutAsync(1_500);
                var home = page.Locator("a[href='/']").First;
                if (await home.CountAsync() > 0)
                {
                    await home.ClickAsync();
                    await page.WaitForTimeoutAsync(1_500);
                }
            }
        }

        await page.EvaluateAsync("""
            () => {
              const fn = window.__jobsyShimProbe || function () {};
              window.addEventListener('unload', fn);
              window.removeEventListener('unload', fn);
            }
            """);

        var lastAdd = await page.EvaluateAsync<string>(
            "() => EventTarget.prototype.addEventListener.toString()");
        Assert.Equal(firstAdd, lastAdd);

        var stackErrors = pageErrors
            .Where(e => e.Contains("Maximum call stack size exceeded", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(stackErrors.Count == 0, "pagehide shim stacked: " + string.Join(" | ", stackErrors));
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
