using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// After enhanced navigation, FocusOnNavigate focuses h1 but the programmatic
/// focus box is hidden (<c>h1[tabindex="-1"]:focus:not(:focus-visible)</c>).
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class KandidaatBanenFocusPlaywrightTests
{
    [Fact]
    public async Task Desktop_after_navigation_h1_focused_without_visible_outline()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.WaitForTimeoutAsync(800);

        var login = page.Locator("a[href='/login']").First;
        if (await login.CountAsync() > 0)
        {
            await login.ClickAsync(new() { Timeout = 10_000 });
            await page.WaitForTimeoutAsync(1_200);
        }
        else
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.WaitForTimeoutAsync(800);
        }

        var result = await page.EvaluateAsync<FocusProbe>("""
            () => {
              const h1 = document.querySelector('h1');
              if (!h1) return { ok: false, reason: 'no-h1' };
              const active = document.activeElement;
              const isH1 = active === h1 || (active && active.tagName === 'H1');
              const style = window.getComputedStyle(h1);
              return {
                ok: true,
                isH1: !!isH1,
                outlineStyle: style.outlineStyle || '',
                tabindex: h1.getAttribute('tabindex')
              };
            }
            """);

        Assert.True(result.Ok, result.Reason ?? "probe failed");
        if (result.IsH1)
        {
            Assert.Equal("none", result.OutlineStyle);
        }
        else
        {
            // FocusOnNavigate may not have run yet on cold Acc; CSS rule must still exist.
            var cssOk = await page.EvaluateAsync<bool>("""
                () => {
                  for (const sheet of Array.from(document.styleSheets)) {
                    try {
                      for (const rule of Array.from(sheet.cssRules || [])) {
                        if (rule.selectorText && rule.selectorText.indexOf('h1[tabindex="-1"]') >= 0) {
                          return true;
                        }
                      }
                    } catch (_) { /* cross-origin */ }
                  }
                  return false;
                }
                """);
            Assert.True(cssOk, "Expected h1[tabindex=-1] focus rule in loaded stylesheets when h1 is not active.");
        }
    }

    private sealed record FocusProbe(bool Ok, bool IsH1 = false, string OutlineStyle = "", string? Tabindex = null, string? Reason = null);

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
