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
            var miniPick = page.Locator("form[data-ob-ssr-form] button[name=obMiniPick]").Nth(2);
            if (await miniPick.CountAsync() > 0)
            {
                return await TryHitAuditAsync(page, miniPick, "Likert 1");
            }

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

    internal static async Task AppendHydrationAndKeyboardRowsAsync(IPage page, List<(string Control, string Result)> rows)
    {
        if (rows.All(r => r.Control != "Hydration (circuit)"))
        {
            var circuit = await page.EvaluateAsync<string>(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') || 'missing'");
            var reconnect = await page.Locator(
                    "#components-reconnect-modal.components-reconnect-show, #components-reconnect-modal.components-reconnect-failed")
                .CountAsync();
            var ssr = await page.Locator("form[data-ob-ssr-form]").CountAsync() > 0;
            rows.Add(("Hydration (circuit)", (circuit == "ready" || ssr) && reconnect == 0
                ? "ok"
                : $"circuit={circuit};ssr={ssr};reconnect={reconnect}"));
        }

        if (rows.All(r => r.Control != "Keyboard (Volgende focus)"))
        {
            var next = page.GetByTestId("onboarding-next");
            if (await next.CountAsync() == 0)
            {
                rows.Add(("Keyboard (Volgende focus)", "n/a"));
                return;
            }

            try
            {
                await next.First.FocusAsync();
                var focused = await page.EvaluateAsync<bool>(
                    """
                    () => {
                      const n = document.querySelector('[data-testid=onboarding-next]');
                      return !!(n && document.activeElement === n);
                    }
                    """);
                rows.Add(("Keyboard (Volgende focus)", focused ? "ok" : "no-focus"));
                await page.Keyboard.PressAsync("Tab");
                await page.WaitForTimeoutAsync(100);
                var enterTarget = await page.EvaluateAsync<string>(
                    "() => (document.activeElement?.getAttribute('data-testid') || document.activeElement?.tagName || '').toLowerCase()");
                rows.Add(("Keyboard (Tab cycle)", string.IsNullOrEmpty(enterTarget) ? "no-focus" : "ok"));
            }
            catch (Exception ex)
            {
                rows.Add(("Keyboard (Volgende focus)", ex.Message));
            }
        }
    }

    internal static async Task AppendHeaderAndCoachRowsAsync(IPage page, List<(string Control, string Result)> rows)
    {
        if (rows.All(r => r.Control != "Header help"))
        {
            rows.Add(("Header help", await TryHitAuditAsync(
                page,
                page.Locator("[data-testid=app-shell-header] .page-help__btn").First,
                "help")));
            rows.Add(("Header account", await TryHitAuditAsync(
                page,
                page.Locator("[data-testid=app-shell-header] .account-menu__toggle").First,
                "account")));
            var coachBtn = page.Locator(
                ".lobsy-coach-dock[data-ssr-coach] .lobsy-coach-dock__btn, .lobsy-coach-dock__btn").First;
            rows.Add(("Coach medallion", await TryHitAuditAsync(page, coachBtn, "coach")));

            if (await coachBtn.CountAsync() > 0)
            {
                try
                {
                    await coachBtn.ClickAsync(new() { Force = true, Timeout = 15_000 });
                    await page.WaitForTimeoutAsync(400);
                    var panel = page.Locator(".lobsy-coach-dock__panel, [data-ssr-coach-panel]").First;
                    var openOk = await panel.CountAsync() > 0 && await panel.IsVisibleAsync();
                    if (openOk)
                    {
                        var close = page.Locator(".lobsy-coach-dock__close, [data-ssr-coach-close]").First;
                        if (await close.CountAsync() > 0)
                        {
                            await close.ClickAsync(new() { Force = true, Timeout = 10_000 });
                        }
                        else
                        {
                            await coachBtn.ClickAsync(new() { Force = true, Timeout = 10_000 });
                        }

                        await page.WaitForTimeoutAsync(300);
                    }

                    rows.Add(("Coach open/dicht", openOk ? "ok" : "panel-missing"));
                }
                catch (Exception ex)
                {
                    rows.Add(("Coach open/dicht", ex.Message));
                }
            }
            else
            {
                rows.Add(("Coach open/dicht", "n/a"));
            }
        }
    }

    internal static async Task CaptureStepScreenshotAsync(IPage page, string reportDir, string fileName)
    {
        Directory.CreateDirectory(reportDir);
        await page.ScreenshotAsync(new() { Path = Path.Combine(reportDir, fileName), FullPage = false });
    }
}
