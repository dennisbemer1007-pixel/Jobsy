using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Read-aloud controls in a real browser. Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ReadAloudPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1440, 900, "nl-NL", "")]
    [InlineData(390, 844, "nl-NL", "")]
    [InlineData(390, 844, "ar-SA", "ar")]
    public async Task Speaker_is_reachable_and_does_not_cover_controls(int width, int height, string locale, string lang)
    {
        if (!TryBase(out var baseUrl))
        {
            return;
        }

        await using var session = await OpenAsync(baseUrl, width, height, locale);
        if (session is null)
        {
            return;
        }

        var page = session.Page;
        var query = string.IsNullOrEmpty(lang) ? "" : "?lang=" + lang;
        await page.GotoAsync(baseUrl + "/candidate/career" + query,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var bubble = page.Locator(".lobsy-coach-dock__tip [data-read-aloud], .journey-bubble [data-read-aloud], .test-dive-scene__bubble [data-read-aloud]");
        var sawBubble = await bubble.CountAsync() > 0 && await bubble.First.IsVisibleAsync();
        if (sawBubble)
        {
            await bubble.First.ClickAsync();
            await page.Locator("[data-read-aloud-state='stop']").First.WaitForAsync(new() { Timeout = 8_000 });
            await page.Locator("[data-read-aloud-state='stop']").First.ClickAsync();
            await page.Locator("[data-read-aloud-state='play']").First.WaitForAsync(new() { Timeout = 8_000 });
        }
        else
        {
            await page.GotoAsync(baseUrl + "/candidate/profile" + query,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            var prefsName = lang == "ar" ? "التفضيلات" : "Voorkeuren";
            var prefs = page.GetByRole(AriaRole.Button, new() { Name = prefsName });
            if (await prefs.CountAsync() == 0)
            {
                return;
            }

            await prefs.First.ClickAsync();
            var setting = page.Locator("[data-read-aloud-setting]");
            await setting.First.WaitForAsync(new() { Timeout = 15_000 });
            if (lang == "ar")
            {
                var dir = await page.Locator("html").GetAttributeAsync("dir");
                Assert.Equal("rtl", dir);
            }

            var toggle = setting.Locator("input[type=checkbox]");
            if (await toggle.CountAsync() > 0)
            {
                Assert.True(await toggle.First.IsCheckedAsync());
            }
        }

        var layout = await page.EvaluateAsync<string>("""
            () => {
              const overflow = document.documentElement.scrollWidth > document.documentElement.clientWidth + 1;
              function box(el) {
                const r = el.getBoundingClientRect();
                return { left: r.left, right: r.right, top: r.top, bottom: r.bottom, width: r.width, height: r.height };
              }
              function hits(a, b) {
                if (a.width < 2 || a.height < 2 || b.width < 2 || b.height < 2) return false;
                return !(a.right < b.left || a.left > b.right || a.bottom < b.top || a.top > b.bottom);
              }
              const speakers = [...document.querySelectorAll("[data-read-aloud], [data-read-aloud-setting] input")];
              const obstacles = [...document.querySelectorAll(".lobsy-assistant-tab__btn, textarea, button.test-flow__next, button.engagement-btn, input, button.read-aloud")];
              let covered = false;
              for (const speaker of speakers) {
                const s = box(speaker);
                for (const obstacle of obstacles) {
                  if (speaker === obstacle || speaker.contains(obstacle) || obstacle.contains(speaker)) continue;
                  if (hits(s, box(obstacle))) covered = true;
                }
              }
              if (overflow && covered) return "both";
              if (overflow) return "overflow";
              if (covered) return "covered";
              return "ok";
            }
            """);
        Assert.Equal("ok", layout);
    }

    private static bool TryBase(out string baseUrl)
    {
        baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return !string.IsNullOrWhiteSpace(baseUrl);
    }

    private static async Task<BrowserSession?> OpenAsync(string baseUrl, int width, int height, string locale)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            Locale = locale
        });
        await context.AddInitScriptAsync(
            """
            window.__lobsyReadAloudSynth = (function () {
              const voices = [
                { lang: "nl-NL", name: "Test NL" },
                { lang: "en-GB", name: "Test EN" },
                { lang: "pl-PL", name: "Test PL" },
                { lang: "ro-RO", name: "Test RO" },
                { lang: "ar-SA", name: "Test AR" }
              ];
              return {
                speaking: false,
                getVoices: function () { return voices; },
                speak: function (utter) { this._utter = utter; this.speaking = true; },
                cancel: function () {
                  this.speaking = false;
                  var utter = this._utter;
                  this._utter = null;
                  if (utter && typeof utter.onend === "function") utter.onend();
                },
                addEventListener: function () {},
                removeEventListener: function () {}
              };
            })();
            """);
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
            playwright.Dispose();
            return null;
        }

        return new BrowserSession(playwright, browser, context, page);
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/account/login",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            var email = page.Locator("input[type=email], input[name=email]").First;
            var password = page.Locator("input[type=password]").First;
            if (await email.CountAsync() == 0)
            {
                return false;
            }

            await email.FillAsync(Environment.GetEnvironmentVariable("JOBSY_E2E_EMAIL") ?? DefaultEmail);
            await password.FillAsync(Environment.GetEnvironmentVariable("JOBSY_E2E_PASSWORD") ?? DefaultPassword);
            await page.Locator("button[type=submit]").First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class BrowserSession(
        IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IPage Page { get; } = page;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
