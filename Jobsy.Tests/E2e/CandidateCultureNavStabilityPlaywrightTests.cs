using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Signed-in UI language must not flip when navigating without using /taal/.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class CandidateCultureNavStabilityPlaywrightTests
{
    private const string Email = "onboarding.e2e@jobsy.local";
    private const string Password = "Jobsy123!";

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Dutch_cookie_stays_nl_across_candidate_nav(int width, int height)
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "https://acceptatie.lobsy.nl";
        if (!FunctionalE2eGate.IsRequired && !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        if (FunctionalE2eGate.IsRequired)
        {
            baseUrl = await FunctionalE2eGate.RequireReachableBaseUrlAsync();
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var uri = new Uri(baseUrl);
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "en-US",
            ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept-Language"] = "en-US,en;q=0.9" },
            IgnoreHTTPSErrors = true,
        });
        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "Jobsy.Culture",
                Value = "nl",
                Domain = uri.Host,
                Path = "/",
            }
        ]);

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        if (!await KbE2e.TryLoginAsync(page, baseUrl, Email, Password))
        {
            return;
        }

        var mobile = width < 500;
        foreach (var path in new[] { "/banenkaart", "/candidate/paspoort", "/candidate/ontdekkingsreis", "/carriere" })
        {
            var link = mobile
                ? page.Locator($"nav.bottom-nav a[href='{path}']").First
                : page.Locator($"a[href='{path}']").First;
            if (await link.CountAsync() == 0)
            {
                await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            }
            else
            {
                await link.ClickAsync();
            }

            await page.WaitForTimeoutAsync(800);
            var lang = await page.EvaluateAsync<string>("() => document.documentElement.lang || ''");
            Assert.Equal("nl", lang);
            var cookie = await page.EvaluateAsync<string>(
                "() => (document.cookie.match(/Jobsy\\.Culture=([^;]+)/) || [])[1] || ''");
            Assert.Equal("nl", Uri.UnescapeDataString(cookie));
        }
    }
}
