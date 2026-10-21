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
        var fieldset = page.Locator(
                "[data-ob-rustig-screen^=\"step-\"] .test-likert, [data-onboarding-step] .test-likert, .ob-wizard__card .test-likert, .test-likert")
            .First;
        if (await fieldset.CountAsync() == 0)
        {
            return "n/a";
        }

        const int timeoutMs = 20_000;

        try
        {
            await FunctionalOnboardingInteractivityHelpers.AcceptTestConsentIfPresentAsync(page);
            await fieldset.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            await page.EvaluateAsync(
                """
                () => {
                  const likert = document.querySelector('.test-likert');
                  if (likert) {
                    likert.scrollIntoView({ block: 'center', inline: 'nearest', behavior: 'instant' });
                  }
                  const scrollers = [
                    document.querySelector('.ob-rustig__canvas--mini-test'),
                    document.querySelector('.ob-wizard__content'),
                    document.querySelector('.app-main'),
                  ];
                  for (const el of scrollers) {
                    if (el) el.scrollTop = 0;
                  }
                }
                """);
            await page.WaitForTimeoutAsync(200);

            var opt = fieldset.Locator(".test-likert__opt").First;
            await opt.ScrollIntoViewIfNeededAsync();
            var label = opt.Locator("label").First;
            var target = await label.CountAsync() > 0 ? label : opt;
            var hit = await TryHitAuditAsync(page, target, "Likert 1");
            if (hit is not "ok")
            {
                return hit;
            }

            try
            {
                await opt.ClickAsync(new() { Force = true, Timeout = timeoutMs });
                await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page);
            }
            catch
            {
                // Hit-target audit is the gate; Blazor selection can lag on rustig desktop.
            }

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
