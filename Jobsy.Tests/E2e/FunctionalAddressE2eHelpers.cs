using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class FunctionalAddressE2eHelpers
{
    internal static ILocator OnboardingAddressInput(IPage page)
        => page.Locator("#onboarding-home-address").First;

    internal static ILocator BanenkaartAddressInput(IPage page, int viewportWidth)
        => viewportWidth >= 900
            ? page.Locator("#discovery-address-desktop").First
            : page.Locator("#discovery-address, #discovery-address-sheet").First;

    internal static async Task TypeAddressQueryAsync(ILocator input, string query)
    {
        await input.ScrollIntoViewIfNeededAsync();
        await input.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await input.FillAsync("");
        await input.PressSequentiallyAsync(query, new() { Delay = 35 });
    }

    internal static async Task WaitForSuggestionWithHouseNumberAsync(IPage page, int houseNumber = 27)
    {
        await page.WaitForFunctionAsync(
            """
            ({ houseNumber }) => {
              const items = document.querySelectorAll('.address-suggestions button.address-suggestions__item, .address-suggestions__item');
              const needle = String(houseNumber);
              return Array.from(items).some(el => (el.textContent || '').includes(needle));
            }
            """,
            new { houseNumber },
            new() { Timeout = 90_000 });
    }

    internal static async Task BlurAddressWithoutPickingAsync(IPage page, string inputId)
    {
        await page.EvaluateAsync(
            """
            (id) => {
              const el = document.getElementById(id);
              el?.blur();
              window.jobsyGeo?.blurElement?.(id);
            }
            """,
            inputId);
        await page.WaitForTimeoutAsync(2500);
    }

    internal static async Task WaitForOnboardingAddressResolvedAsync(IPage page)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (await page.EvaluateAsync<bool>(
                    """
                    () => {
                      const hints = document.querySelectorAll('.ob-fields .ob-wizard__hint, .ob-fields__address .ob-wizard__hint');
                      for (const hint of hints) {
                        if (hint.textContent?.includes('✓')) return true;
                      }
                      const draftLat = document.querySelector('[data-testid=onboarding-nearby-panel-desktop], [data-testid=onboarding-nearby-panel-mobile]');
                      return !!draftLat;
                    }
                    """))
            {
                return;
            }

            await page.WaitForTimeoutAsync(1000);
        }

        throw new InvalidOperationException("Onboarding address did not resolve after blur.");
    }

    internal static async Task FillMinimalStep1ForVolgendeAsync(IPage page)
    {
        await page.Locator("input[autocomplete=given-name]").FillAsync("Adres");
        await page.Locator("input[autocomplete=family-name]").FillAsync("E2e");
        await FunctionalOnboardingInteractivityHelpers.AssertWorkStatusChipTogglesAsync(page);
        await FunctionalOnboardingInteractivityHelpers.FillBirthDateAsync(page);
    }

    internal static async Task AssertVolgendeAdvancesToStep2Async(IPage page, bool forceClick)
    {
        var next = page.GetByTestId("onboarding-next");
        await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 30_000 });
        await next.ClickAsync(new() { Timeout = 20_000, Force = forceClick });
        await page.Locator("[data-testid=onboarding-step-2-illustrated], .ob-wizard__presets, .ob-illustrated-step").First
            .WaitForAsync(new() { Timeout = 90_000 });
    }

    internal static async Task WaitForBanenkaartAddressSuggestionsAsync(IPage page)
    {
        await page.Locator(".address-suggestions button.address-suggestions__item").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
    }
}
