using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.TestSupport;

internal static class OnboardingPlaywrightHelpers
{
    internal static async Task WaitForBlazorCircuitAsync(IPage page, Func<Task> navigation)
    {
        await page.RunAndWaitForWebSocketAsync(
            navigation,
            new()
            {
                Timeout = 90_000,
                Predicate = socket => (socket.Url ?? "").Contains("_blazor", StringComparison.OrdinalIgnoreCase)
            });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
    }

    internal static async Task EnsureStep1FieldsAsync(IPage page)
    {
        var loading = page.Locator(".ob-wizard__loading");
        if (await loading.CountAsync() > 0)
        {
            await loading.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
        }

        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            var begin = page.Locator(".ob-wizard__welcome .ob-wizard__cta");
            await begin.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await begin.ClickAsync(new() { Delay = 100 });
            if (await loading.CountAsync() > 0)
            {
                await loading.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
            }
        }

        await page.Locator(".ob-fields, #onboarding-field-firstname input").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
    }

    internal static Task WaitForSavedIndicatorAsync(IPage page)
        => page.WaitForFunctionAsync(
            """
            () => {
              const el = document.querySelector('.ob-wizard__status-row [aria-live="polite"]');
              return el && /bewaard/i.test(el.textContent || '');
            }
            """,
            null,
            new() { Timeout = 90_000 });
}
