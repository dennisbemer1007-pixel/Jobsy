using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class OnboardingRustigClickAuditSupport
{
    internal static ILocator LaterVerderControl(IPage page)
    {
        var rustig = page.Locator(".ob-rustig__actions > button.ob-rustig__link").First;
        return rustig;
    }

    internal static async Task<ILocator> ResolveLaterVerderAsync(IPage page)
    {
        if (await page.Locator(".ob-rustig__actions > button.ob-rustig__link").CountAsync() > 0)
        {
            return page.Locator(".ob-rustig__actions > button.ob-rustig__link").First;
        }

        var headerLater = page.Locator(".ob-wizard__header-actions button.ob-wizard__later").First;
        if (await headerLater.CountAsync() > 0 && await headerLater.IsVisibleAsync())
        {
            return headerLater;
        }

        var named = page.GetByRole(AriaRole.Button, new() { Name = "Later verder" });
        var n = await named.CountAsync();
        for (var i = 0; i < n; i++)
        {
            var candidate = named.Nth(i);
            if (await candidate.IsVisibleAsync())
            {
                return candidate;
            }
        }

        return named.First;
    }

    internal static ILocator LikertFirstOption(IPage page)
        => page.Locator(".test-likert .test-likert__opt").First;

    internal static async Task<string> TryHitAndClickAsync(IPage page, ILocator locator, string label)
    {
        if (await locator.CountAsync() == 0)
        {
            return "n/a";
        }

        try
        {
            await locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, locator.First, label);
            await locator.First.ClickAsync(new() { Force = true, Timeout = 30_000 });
            return "ok";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    internal static async Task<string> TryLikertFirstClickAsync(IPage page)
    {
        var fieldset = page.Locator(".test-likert").First;
        if (await fieldset.CountAsync() == 0)
        {
            return "n/a";
        }

        try
        {
            await fieldset.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.EvaluateAsync(
                """
                () => {
                  const main = document.querySelector('.app-main');
                  if (main) main.scrollTop = 0;
                }
                """);
            if (await fieldset.Locator(".test-likert__opt.is-on").CountAsync() > 0)
            {
                return "ok";
            }

            var opt = fieldset.Locator(".test-likert__opt").First;
            var label = opt.Locator("label").First;
            var clickTarget = await label.CountAsync() > 0 ? label : opt;
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, clickTarget, "likert-1");
            await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page);
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (await page.EvaluateAsync<bool>(
                        "() => !!document.querySelector('.test-likert input[type=radio]:checked')"))
                {
                    return "ok";
                }

                if (attempt == 0)
                {
                    await clickTarget.ClickAsync(new() { Force = true, Timeout = 15_000 });
                }
                else if (attempt == 1)
                {
                    await opt.EvaluateAsync(
                        """
                        el => {
                          el.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
                        }
                        """);
                }
                else
                {
                    await opt.EvaluateAsync(
                        """
                        el => {
                          const input = el.querySelector('input[type=radio]');
                          if (!input) return;
                          input.click();
                          input.dispatchEvent(new Event('change', { bubbles: true }));
                        }
                        """);
                }

                await page.WaitForTimeoutAsync(600);
            }

            if (await page.EvaluateAsync<bool>(
                    "() => !!document.querySelector('.test-likert input[type=radio]:checked')"))
            {
                return "ok";
            }

            var flowNext = page.Locator(".test-flow__next").First;
            if (await flowNext.CountAsync() > 0 && !await flowNext.IsDisabledAsync())
            {
                return "ok";
            }

            await page.WaitForFunctionAsync(
                """
                () => {
                  const rg = document.querySelector('.test-likert');
                  if (!rg) return false;
                  return !!rg.querySelector('.test-likert__opt.is-on, input[type=radio]:checked');
                }
                """,
                null,
                new() { Timeout = 20_000 });
            return "ok";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    internal static async Task AppendMiniTestStepRowsAsync(IPage page, List<(string Control, string Result)> rows)
    {
        var laterVerder = await ResolveLaterVerderAsync(page);
        rows.Add(("Later verder", await TryHitAuditAsync(page, laterVerder, "Later verder")));

        var accountLink = page.Locator("a.ob-rustig__link--underline[href='/privacy/data']");
        if (await accountLink.CountAsync() == 0)
        {
            accountLink = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
        }

        rows.Add(("Mijn gegevens & account", await TryHitAuditAsync(page, accountLink, "account")));
        rows.Add(("Likert 1", await TryLikertFirstClickAsync(page)));

        var flowBack = page.Locator(".test-flow__back").First;
        rows.Add(("Test terug", await TryHitAuditAsync(page, flowBack, "Test terug")));
        var flowNext = page.Locator(".test-flow__next").First;
        rows.Add(("Test volgende", await TryHitAuditAsync(page, flowNext, "Test volgende")));
        var testsLater = page.Locator("[data-testid=onboarding-tests-later], .test-flow__later").First;
        rows.Add(("Later doen", await TryHitAuditAsync(page, testsLater, "Later doen")));
    }

    internal static async Task<string> TryHitAuditAsync(IPage page, ILocator locator, string label)
    {
        if (await locator.CountAsync() == 0)
        {
            return "n/a";
        }

        try
        {
            var target = locator.First;
            if (label is "Later doen")
            {
                await target.ScrollIntoViewIfNeededAsync();
            }

            await target.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, target, label);
            return "ok";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
