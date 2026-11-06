using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart is server-rendered (<see href="BanenkaartSsr.razor"/> with <c>NoBlazorRuntime</c>); smoke
/// tests must not require a Blazor circuit or <c>/_blazor</c> WebSocket on that route.
/// </summary>
internal static class PlaywrightBanenkaartWaitHelper
{
    /// <summary>SSR shell + map/list script surface.</summary>
    public static async Task WaitForBanenkaartInteractiveAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (!root) return false;
              return !!(window.jobMap && typeof window.jobMap.isAlive === 'function' && window.jobMap.isAlive())
                || document.querySelector('[data-testid="kb-list-rows"]') != null
                || document.querySelector('.kb-list-rows .kb-list-row') != null;
            }
            """,
            null,
            new() { Timeout = 60_000 });
    }

    public static async Task DismissStartPromptIfPresentAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() == 0)
        {
            return;
        }

        try
        {
            await later.First.ClickAsync(new() { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(400);
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
        }
    }
}
