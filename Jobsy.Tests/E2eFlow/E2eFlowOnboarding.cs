using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2eFlow;

/// <summary>Full onboarding + ontdekkingsreis UI (from CandidateFullJourneyPlaywrightTests).</summary>
internal static class E2eFlowOnboarding
{
    public static async Task<string?> SignUpFreshAsync(
        IPage page,
        string baseUrl,
        string email,
        string firstName)
    {
        await CandidateJourneyE2e.ClearCapturedEmailCodesAsync(baseUrl);
        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("#signup-title, .pub-signup__title, .au-title, input[name=email]", new() { Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        var hasBlazorRuntime = await page.EvaluateAsync<bool>(
            "() => !!document.querySelector('script[src*=\"blazor.web.js\"]')");
        if (hasBlazorRuntime)
        {
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 90_000 });
        }

        await page.Locator("input[name=email], input[type=email]").First.FillAsync(email);
        var first = page.Locator("input[name=firstName]").First;
        if (await first.CountAsync() > 0)
        {
            await first.FillAsync(firstName);
        }

        var submit = page.Locator(
            "form.pub-signup__form button[type=submit], form.au-form button[type=submit], form.login-form button[type=submit], button.au-submit, button.login-submit").First;
        await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await submit.ScrollIntoViewIfNeededAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const btn = document.querySelector(
                'form.pub-signup__form button[type=submit], form.login-form button[type=submit], button.au-submit, button.login-submit');
              return btn && !btn.disabled && btn.getAttribute('aria-disabled') !== 'true';
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var mobile = (page.ViewportSize?.Width ?? 1440) < 600;
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var waitCodePage = page.WaitForURLAsync(
                    url => url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 120_000 });
                if (mobile)
                {
                    await Task.WhenAll(waitCodePage, submit.TapAsync(new() { Timeout = 60_000 }));
                }
                else
                {
                    await Task.WhenAll(waitCodePage, submit.ClickAsync(new() { Timeout = 60_000 }));
                }

                break;
            }
            catch (TimeoutException) when (attempt == 0)
            {
                await page.WaitForTimeoutAsync(750);
            }
        }

        if (!page.Url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase))
        {
            throw new TimeoutException(
                $"Signup submit did not reach /account-maken/code; still at {page.Url}");
        }

        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException($"No signup code captured for {email}");
        }

        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 120_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        return code;
    }

    public static async Task<int> CompleteOnboardingWizardWithScreenshotsAsync(
        IPage page,
        string baseUrl,
        string viewportTag,
        Func<IPage, string, string, Task> shotAsync)
    {
        await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);
        var screens = 0;
        string? lastMiniStatement = null;
        var miniStagnant = 0;

        for (var step = 1; step <= 16; step++)
        {
            screens++;
            await page.EvaluateAsync("() => { const m = document.querySelector('.app-main, .ob-wizard__content'); if (m) m.scrollTop = 0; }");
            await page.WaitForTimeoutAsync(400);
            await shotAsync(page, viewportTag, $"01-onboarding-{step:D2}");

            if (await page.Locator(".ob-wizard__finish").CountAsync() > 0)
            {
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                break;
            }

            if (await page.Locator(".ob-wizard__install").CountAsync() > 0)
            {
                var later = page.Locator("button.ob-wizard__ghost").First;
                if (await later.CountAsync() > 0)
                {
                    await later.ClickAsync();
                    continue;
                }
            }

            if (await page.Locator(".test-flow, fieldset.q-likert, .q-likert__scale").CountAsync() > 0)
            {
                var statement = page.Locator(".test-flow__statement").First;
                var statementText = await statement.CountAsync() > 0 ? await statement.InnerTextAsync() : "";
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 8, 4);
                await CandidateJourneyE2e.TryAdvanceMiniTestWizardAsync(page);
                if (!string.IsNullOrWhiteSpace(statementText)
                    && string.Equals(statementText, lastMiniStatement, StringComparison.Ordinal))
                {
                    miniStagnant++;
                    if (miniStagnant >= 3)
                    {
                        break;
                    }
                }
                else
                {
                    miniStagnant = 0;
                    lastMiniStatement = statementText;
                }

                continue;
            }

            await FillVisibleWizardFieldsAsync(page);

            if (await page.Locator(".ob-wizard__presets button").CountAsync() > 0)
            {
                await shotAsync(page, viewportTag, "01-onboarding-header-scrolled");
            }

            var skipDream = page.Locator(".ob-wizard__ghost").Filter(new() { HasText = "Weet ik nog niet" }).First;
            if (await skipDream.CountAsync() > 0)
            {
                await skipDream.ClickAsync(new() { Timeout = 5_000 });
                await page.WaitForTimeoutAsync(600);
                continue;
            }

            await CandidateJourneyE2e.ClickPrimaryAsync(page);
            await page.WaitForTimeoutAsync(600);
        }

        if (await page.Locator(".ob-wizard__finish").CountAsync() > 0)
        {
            await CandidateJourneyE2e.ClickPrimaryAsync(page);
        }

        await shotAsync(page, viewportTag, "01-onboarding-finish");
        return screens;
    }

    public static async Task CompleteDiscoveryMaxDepthAsync(
        IPage page,
        string baseUrl,
        string viewportTag,
        Func<IPage, string, string, Task> shotAsync)
    {
        await page.GotoAsync(baseUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page, "ontdekkingsreis.css");
        var shedShot = 0;
        var stagnantRounds = 0;

        for (var round = 0; round < 280; round++)
        {
            if (await page.Locator(".journey-end, .journey-klaar, [data-screen=klaar]").CountAsync() > 0)
            {
                break;
            }

            if (await page.Locator(".journey-shed").CountAsync() > 0)
            {
                await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
                await CandidateJourneyE2e.SelectMaxShedDepthAsync(page);
                shedShot++;
                await shotAsync(page, viewportTag, $"02-discovery-shed-{shedShot:D2}");
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                await page.WaitForTimeoutAsync(600);
                stagnantRounds = 0;
                continue;
            }

            if (await page.Locator(".test-flow, fieldset.q-likert, .q-likert__scale").CountAsync() > 0)
            {
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 30, 4);
                if (await CandidateJourneyE2e.TryAdvanceDiscoveryStepAsync(page))
                {
                    stagnantRounds = 0;
                }
                else
                {
                    stagnantRounds++;
                }

                continue;
            }

            if (await page.Locator(".ob-fields input").CountAsync() > 0)
            {
                await FillVisibleWizardFieldsAsync(page);
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                continue;
            }

            var consentBox = page.Locator(".ob-wizard__consent input[type=checkbox]").First;
            if (await consentBox.CountAsync() > 0 && !await consentBox.IsCheckedAsync())
            {
                await consentBox.CheckAsync(new() { Timeout = 5_000 });
            }

            var startTests = page.Locator("button.btn-primary").Filter(new() { HasText = "Start de eerste test" }).First;
            if (await startTests.CountAsync() > 0 && await startTests.IsEnabledAsync())
            {
                await startTests.ClickAsync(new() { Timeout = 15_000 });
                continue;
            }

            if (await CandidateJourneyE2e.TryAdvanceDiscoveryStepAsync(page))
            {
                stagnantRounds = 0;
            }
            else
            {
                stagnantRounds++;
                if (stagnantRounds >= 25)
                {
                    break;
                }
            }

            await page.WaitForTimeoutAsync(300);
        }

        await shotAsync(page, viewportTag, "02-discovery-complete");
    }

    private static async Task FillVisibleWizardFieldsAsync(IPage page)
    {
        var fields = page.Locator(".ob-fields input:visible, .ob-wizard__content .ob-fields input:visible");
        if (await fields.CountAsync() >= 3)
        {
            var lastName = fields.Nth(1);
            if (string.IsNullOrWhiteSpace(await lastName.InputValueAsync()))
            {
                await lastName.FillAsync("Jansen");
            }

            var postcode = fields.Nth(2);
            if (string.IsNullOrWhiteSpace(await postcode.InputValueAsync()))
            {
                await postcode.FillAsync("2671GB");
            }

            await page.WaitForTimeoutAsync(400);
            var suggestion = page.Locator(".ob-wizard__suggestions button").First;
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync(new() { Timeout = 5_000 });
            }

            var city = page.Locator(".ob-fields label:has-text('Plaats') input, .ob-fields label:has-text('Woonplaats') input").First;
            if (await city.CountAsync() > 0 && string.IsNullOrWhiteSpace(await city.InputValueAsync()))
            {
                await city.FillAsync("Naaldwijk");
            }
        }

        var birthInput = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birthInput.CountAsync() > 0 && string.IsNullOrWhiteSpace(await birthInput.InputValueAsync()))
        {
            var birth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-22);
            await birthInput.FillAsync(birth.ToString("yyyy-MM-dd"));
            await birthInput.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }

        var availStart = page.Locator("[data-testid=availability-start-date] input[type=date]").First;
        if (await availStart.CountAsync() > 0 && string.IsNullOrWhiteSpace(await availStart.InputValueAsync()))
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14);
            await availStart.FillAsync(start.ToString("yyyy-MM-dd"));
            await availStart.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }

        var preset = page.Locator(".ob-wizard__presets button:not(.is-selected)").First;
        if (await preset.CountAsync() > 0)
        {
            await preset.ClickAsync(new() { Timeout = 5_000 });
        }

        var transport = page.Locator(".ob-wizard__transport button:not(.is-selected)").First;
        if (await transport.CountAsync() > 0)
        {
            await transport.ClickAsync(new() { Timeout = 5_000 });
        }

        foreach (var selector in new[]
                 {
                     ".ob-wizard__education-grid button:not(.is-selected)",
                     ".ob-wizard__mbo-levels button:not(.is-selected)",
                     ".ob-wizard__dream-grid button:not(.is-selected)"
                 })
        {
            var pick = page.Locator(selector).First;
            if (await pick.CountAsync() > 0)
            {
                await pick.ClickAsync(new() { Timeout = 5_000 });
            }
        }

        var direction = page.Locator("#onboarding-education-direction").First;
        if (await direction.CountAsync() > 0 && string.IsNullOrWhiteSpace(await direction.InputValueAsync()))
        {
            await direction.FillAsync("Techniek");
        }

        var dreamSearch = page.Locator(".ob-wizard__dream-search input").First;
        if (await dreamSearch.CountAsync() > 0 && string.IsNullOrWhiteSpace(await dreamSearch.InputValueAsync()))
        {
            await dreamSearch.FillAsync("Orderpicker");
            await page.WaitForTimeoutAsync(600);
            var dreamPick = page.Locator(".ob-wizard__dream-search button").First;
            if (await dreamPick.CountAsync() > 0)
            {
                await dreamPick.ClickAsync(new() { Timeout = 5_000 });
            }
        }

        var addJob = page.Locator("button.ob-wizard__add-job").First;
        if (await addJob.CountAsync() > 0)
        {
            await addJob.ClickAsync(new() { Timeout = 3_000 });
            await page.Locator(".ob-wizard__job input").First.FillAsync("Orderpicker");
        }

        var perDirect = page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Per direct" }).First;
        if (await perDirect.CountAsync() > 0 && await page.Locator(".ob-wizard__start-option.is-selected").CountAsync() == 0)
        {
            await perDirect.ClickAsync(new() { Timeout = 3_000 });
        }

        foreach (var input in await page.Locator("input[type=text]:visible, input[type=tel]:visible").AllAsync())
        {
            var name = await input.GetAttributeAsync("name") ?? "";
            if (name.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(await input.InputValueAsync()))
            {
                continue;
            }

            if (name.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                await input.FillAsync("0612345678");
            }
        }

        var uncheckedBoxes = page.Locator(
            ".ob-wizard__consent input[type=checkbox]:not(:checked), .ob-wizard__check input[type=checkbox]:not(:checked)");
        var boxCount = await uncheckedBoxes.CountAsync();
        for (var i = 0; i < boxCount; i++)
        {
            try
            {
                await uncheckedBoxes.Nth(i).CheckAsync(new() { Timeout = 2_000, Force = true });
            }
            catch (Exception)
            {
                // optional or off-screen consent
            }
        }
    }
}
