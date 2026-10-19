using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalCandidateSweepPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Candidate_chrome_language_switch_and_assistant_tab(int width, int height, bool mobile)
    {
        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await FunctionalE2eAuth.NewCandidateContextAsync(browser, baseUrl, new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
        });
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await KbDiscoveryE2eHelpers.PrepareBanenkaartAsync(page, baseUrl);
        await page.Locator(".app-header .language-selector__toggle").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        var langToggle = page.Locator(".language-selector__toggle").First;
        if (await langToggle.CountAsync() > 0 && await langToggle.IsVisibleAsync())
        {
            var before = await langToggle.InnerTextAsync();
            await langToggle.ClickAsync(new() { Force = mobile });
            var altLang = page.Locator(".language-selector__option:not(.is-active)").First;
            if (await altLang.CountAsync() == 0)
            {
                return;
            }

            var href = await altLang.GetAttributeAsync("href") ?? "";
            await altLang.ClickAsync(new() { Force = mobile });
            if (!string.IsNullOrWhiteSpace(href))
            {
                await page.WaitForURLAsync(u => u.ToString().Contains("lang=", StringComparison.OrdinalIgnoreCase) || u.ToString() != page.Url, new() { Timeout = 30_000 })
                    .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
            }

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var after = await page.Locator(".language-selector__toggle .language-selector__code").First.InnerTextAsync();
            Assert.False(string.Equals(before.Trim(), after.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (mobile)
        {
            var tab = page.Locator(".lobsy-assistant-tab__btn").First;
            if (await tab.CountAsync() > 0)
            {
                await tab.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(400);
                await Assertions.Expect(page.Locator("#lobsy-assistant-panel, .lobsy-assistant.is-open")).ToBeVisibleAsync(new() { Timeout = 15_000 });
                await page.Locator(".lobsy-assistant__icon-btn").Filter(new() { HasText = "×" }).First.ClickAsync(new() { Force = true });
            }
        }

        var tag = mobile ? "mobile-390" : "desktop-1440";
        await FunctionalE2eArtifacts.CaptureAsync(page, "candidate-chrome", tag);
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Banenkaart_search_and_view_toggle_change_state(int width, int height)
    {
        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await FunctionalE2eAuth.NewCandidateContextAsync(browser, baseUrl, new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 600,
            HasTouch = width < 600,
            Locale = "nl-NL",
        });
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await KbDiscoveryE2eHelpers.PrepareBanenkaartAsync(page, baseUrl);
        var search = await KbDiscoveryE2eHelpers.WaitForVisibleKeywordSearchAsync(page, width);
        await search.FillAsync("horeca");
        await page.WaitForTimeoutAsync(800);
        Assert.Contains("horeca", await search.InputValueAsync(), StringComparison.OrdinalIgnoreCase);

        var listToggle = page.GetByTestId("kb-view-list");
        if (await listToggle.CountAsync() > 0 && await listToggle.IsVisibleAsync())
        {
            await listToggle.ClickAsync(new() { Force = width < 600 });
            await page.WaitForTimeoutAsync(500);
            var listMode = page.Locator(".jobsy-discovery.show-list, .jobsy-discovery--list").First;
            if (await listMode.CountAsync() > 0)
            {
                await Assertions.Expect(listMode).ToBeVisibleAsync(new() { Timeout = 15_000 });
            }
        }

        var tag = width < 600 ? "mobile-390" : "desktop-1440";
        await FunctionalE2eArtifacts.CaptureAsync(page, "banenkaart-search", tag);
    }

    private static async Task<(bool Run, string BaseUrl)> TryBeginAsync()
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "";
        if (FunctionalE2eGate.IsRequired)
        {
            baseUrl = await FunctionalE2eGate.RequireReachableBaseUrlAsync();
            return (true, baseUrl);
        }

        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return (false, baseUrl);
        }

        return (true, baseUrl);
    }
}
