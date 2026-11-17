using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Detects a logged-in candidate shell on banenkaart without relying on the language toggle
/// (guest map SSR also exposes discovery + keyword search).
/// </summary>
internal static class FunctionalE2eCandidateSession
{
    internal const string AuthenticatedBanenkaartScript = """
        () => {
          const discovery = document.querySelector('.jobsy-discovery');
          if (!discovery) {
            return false;
          }

          const headerNav = document.querySelector(
            '[data-testid=app-shell-header] [data-testid=candidate-header-nav], ' +
            '.app-header [data-testid=candidate-header-nav]');
          const bottomNav = document.querySelector('[data-testid=candidate-bottom-nav-ssr]');
          const accountToggle = document.querySelector('.app-header .account-menu__toggle');
          const stripAuth = discovery.getAttribute('data-kb-strip-external') === '1';
          const shellHeader = document.querySelector('[data-testid=app-shell-header]');

          const authenticated =
            (!!headerNav && headerNav.checkVisibility()) ||
            (!!bottomNav && bottomNav.checkVisibility()) ||
            (!!accountToggle && accountToggle.checkVisibility()) ||
            stripAuth;

          if (!authenticated) {
            return false;
          }

          if (shellHeader && !shellHeader.checkVisibility()) {
            return false;
          }

          const search = document.querySelector(
            '[data-testid=kb-keyword-search-desktop], [data-testid=kb-keyword-search]');
          return !!search && search.checkVisibility();
        }
        """;

    internal static async Task WaitForAuthenticatedBanenkaartAsync(IPage page, int timeoutMs = 90_000)
    {
        await KbDiscoveryE2eHelpers.DismissStartPromptIfPresentAsync(page);
        try
        {
            await page.WaitForFunctionAsync(
                AuthenticatedBanenkaartScript,
                null,
                new() { Timeout = timeoutMs });
        }
        catch (TimeoutException ex)
        {
            var shot = Path.Combine(
                Path.GetTempPath(),
                $"jobsy-functional-e2e-auth-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.png");
            try
            {
                await page.ScreenshotAsync(new() { Path = shot, FullPage = true });
            }
            catch
            {
                shot = "(screenshot failed)";
            }

            var hasShell = await page.Locator("[data-testid=app-shell-header]").CountAsync();
            var hasNav = await page.Locator("[data-testid=candidate-header-nav]").CountAsync();
            var hasAccount = await page.Locator(".app-header .account-menu__toggle").CountAsync();
            var strip = await page.Locator(".jobsy-discovery[data-kb-strip-external='1']").CountAsync();

            throw new InvalidOperationException(
                $"Functional E2E: expected authenticated candidate on banenkaart (url={page.Url}). " +
                $"dom: shell={hasShell}, nav={hasNav}, account={hasAccount}, stripAuth={strip}. " +
                $"screenshot={shot}",
                ex);
        }

        await page.WaitForTimeoutAsync(300);
    }

    internal static async Task<bool> IsAuthenticatedBanenkaartAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(AuthenticatedBanenkaartScript);
        }
        catch
        {
            return false;
        }
    }
}