using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class KbDiscoveryE2eHelpers
{
    internal static async Task PrepareBanenkaartAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl.TrimEnd('/')}/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await page.WaitForFunctionAsync(
            """
            () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'
              || !!document.querySelector('.jobsy-discovery')
            """,
            null,
            new() { Timeout = 90_000 });

        await DismissStartPromptIfPresentAsync(page);
        await page.WaitForFunctionAsync(
            """
            () => {
              const shell = document.querySelector('.app-shell');
              if (!shell) return false;
              if (shell.classList.contains('app-shell--candidate')
                  || shell.classList.contains('app-shell--banenkaart-shell')) {
                return true;
              }
              const toggle = document.querySelector('.app-header .language-selector__toggle');
              return !!toggle && toggle.checkVisibility();
            }
            """,
            null,
            new() { Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);
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
        if (viewportWidth >= 900)
        {
            return page.Locator("[data-testid=kb-keyword-search-desktop], [data-testid=kb-keyword-search]").First;
        }

        return page.Locator("[data-testid=kb-keyword-search]").First;
    }

    internal static async Task<ILocator> WaitForVisibleKeywordSearchAsync(IPage page, int viewportWidth)
    {
        var search = VisibleKeywordSearch(page, viewportWidth);
        await search.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        return search;
    }
}
