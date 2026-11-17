using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class KbDiscoveryE2eHelpers
{
    internal static async Task PrepareBanenkaartAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl.TrimEnd('/')}/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await page.WaitForURLAsync("**/banenkaart**", new() { Timeout = 90_000 });
        await page.WaitForSelectorAsync(
            ".jobsy-discovery",
            new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });

        await FunctionalE2eCandidateSession.WaitForAuthenticatedBanenkaartAsync(page);
    }

    internal static async Task DismissStartPromptIfPresentAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later, button.kb-start-prompt__later").First;
        if (await later.CountAsync() > 0 && await later.IsVisibleAsync())
        {
            await later.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(350);
        }
    }

    internal static ILocator VisibleKeywordSearch(IPage page, int viewportWidth)
    {
        _ = viewportWidth;
        return page.Locator("[data-testid=kb-keyword-search-desktop], [data-testid=kb-keyword-search]").First;
    }

    internal static async Task<ILocator> WaitForVisibleKeywordSearchAsync(IPage page, int viewportWidth)
    {
        var search = VisibleKeywordSearch(page, viewportWidth);
        await search.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        return search;
    }
}
