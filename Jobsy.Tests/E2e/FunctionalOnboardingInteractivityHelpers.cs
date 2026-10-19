using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class FunctionalOnboardingInteractivityHelpers
{
    internal static async Task AssertHitTargetAsync(IPage page, ILocator target, string label)
    {
        var box = await target.BoundingBoxAsync();
        Assert.NotNull(box);
        var cx = box!.X + box.Width / 2;
        var cy = box.Y + box.Height / 2;
        var audit = await page.EvaluateAsync<HitAudit>(
            """
            ({ x, y, selector }) => {
              const el = document.querySelector(selector);
              if (!el) return { ok: false, reason: 'missing-target' };
              const hit = document.elementFromPoint(x, y);
              if (!hit) return { ok: false, reason: 'no-hit' };
              const ok = hit === el || el.contains(hit);
              return {
                ok,
                reason: ok ? 'ok' : `blocked-by-${hit.tagName}${hit.className ? '.' + String(hit.className).split(' ')[0] : ''}`
              };
            }
            """,
            new { x = cx, y = cy, selector = await TargetSelectorAsync(target) });

        Assert.True(audit.Ok, $"{label}: {audit.Reason}");
    }

    internal static async Task AssertWorkStatusChipTogglesAsync(IPage page)
    {
        var chip = page.Locator(".work-status-chips .lobsy-choice-tile, .work-status-chips .work-status-chip").First;
        await chip.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await AssertHitTargetAsync(page, chip, "werkstatus chip");
        var before = await chip.GetAttributeAsync("class") ?? "";
        await chip.ClickAsync(new() { Timeout = 10_000 });
        await page.WaitForTimeoutAsync(350);
        var after = await chip.GetAttributeAsync("class") ?? "";
        Assert.Contains("is-selected", after, StringComparison.Ordinal);
        Assert.DoesNotContain("is-selected", before, StringComparison.Ordinal);
    }

    internal static async Task AssertGeolocateButtonRespondsAsync(IPage page)
    {
        var geo = page.Locator(".ob-fields__address .kb-address__geo").First;
        await geo.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await AssertHitTargetAsync(page, geo, "locate button");
        await geo.ClickAsync(new() { Timeout = 10_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const busy = document.querySelector('.ob-fields__address .kb-address__geo[aria-busy="true"]');
              if (busy) return false;
              return document.querySelector('.ob-wizard__hint--geo')
                || document.querySelector('[data-testid=geolocation-confirm-address]')
                || document.querySelector('.ob-fields .ob-wizard__hint');
            }
            """,
            null,
            new() { Timeout = 60_000 });
    }

    internal static async Task AssertBirthDateFieldInteractiveAsync(IPage page)
    {
        var field = page.Locator("[data-testid=onboarding-birth-date]").First;
        await field.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var text = field.Locator(".jobsy-date-field__text").First;
        if (await text.CountAsync() > 0)
        {
            await AssertHitTargetAsync(page, text, "birth date text field");
            await text.FillAsync("15-06-2000");
            var value = await text.InputValueAsync();
            Assert.Contains("15", value, StringComparison.Ordinal);
            var picker = field.Locator(".jobsy-date-field__picker-btn").First;
            await AssertHitTargetAsync(page, picker, "birth date picker button");
            return;
        }

        var native = field.Locator("input[type=date]").First;
        await native.FillAsync("2000-06-15");
        Assert.Equal("2000-06-15", await native.InputValueAsync());
    }

    internal static async Task RegisterFreshCandidateAsync(IPage page, string baseUrl, string emailPrefix)
    {
        var email = $"{emailPrefix}.{Guid.NewGuid():N}@jobsy.local";
        await CandidateJourneyE2e.ClearCapturedEmailCodesAsync(baseUrl);
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Func");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code), "Expected OTP from E2E mail capture.");
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });
    }

    internal static async Task BeginKennismakenStepAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await VroegResultaatPlaywrightTestsHelpers.EnsureWizardStartedAsync(page);
        await page.Locator("[data-testid=onboarding-step-1-illustrated], #onboarding-field-firstname input").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
    }

    internal static async Task FillKennismakenRequiredFieldsAsync(IPage page)
    {
        await page.Locator("input[autocomplete=given-name]").FillAsync("Func");
        await page.Locator("input[autocomplete=family-name]").FillAsync("Test");
        await ResolveHomeAddressAsync(page, "2671GB");
        await FillBirthDateAsync(page);
        await AssertWorkStatusChipTogglesAsync(page);
    }

    internal static async Task RunThroughWizardToFinishAsync(IPage page, string baseUrl)
    {
        await BeginKennismakenStepAsync(page, baseUrl);
        await FillKennismakenRequiredFieldsAsync(page);
        await VroegResultaatPlaywrightTestsHelpers.ClickNextAsync(page);

        for (var guard = 0; guard < 40; guard++)
        {
            if (await page.Locator(".ob-wizard__finish").CountAsync() > 0)
            {
                break;
            }

            if (await page.Locator(".test-likert").CountAsync() > 0)
            {
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 12, 4);
                await CandidateJourneyE2e.TryAdvanceMiniTestWizardAsync(page);
                continue;
            }

            var later = page.GetByTestId("onboarding-tests-later");
            if (await later.CountAsync() > 0 && await later.IsVisibleAsync())
            {
                await later.ClickAsync(new() { Force = true, Timeout = 15_000 });
                await page.WaitForTimeoutAsync(800);
                if (page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                continue;
            }

            if (await page.Locator(".ob-wizard__presets .lobsy-choice-tile, .ob-wizard__presets button").CountAsync() > 0)
            {
                await page.Locator(".ob-wizard__presets .lobsy-choice-tile, .ob-wizard__presets button").First.ClickAsync();
            }

            if (await page.Locator("#onboarding-field-transport .lobsy-choice-tile, .ob-wizard__transport button").CountAsync() > 0)
            {
                await page.Locator("#onboarding-field-transport .lobsy-choice-tile, .ob-wizard__transport button").First.ClickAsync();
                var travel = page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button").First;
                if (await travel.CountAsync() > 0)
                {
                    await travel.ClickAsync();
                }
            }

            var skip = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex("Overslaan|Weet ik nog niet", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
            if (await skip.CountAsync() > 0)
            {
                await skip.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(400);
                continue;
            }

            if (await page.GetByTestId("onboarding-next").CountAsync() > 0)
            {
                await VroegResultaatPlaywrightTestsHelpers.ClickNextAsync(page);
            }
            else
            {
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
            }

            await page.WaitForTimeoutAsync(500);
        }
    }

    internal static async Task ResolveHomeAddressAsync(IPage page, string query)
    {
        var address = page.Locator("#onboarding-home-address, .ob-fields input[inputmode=text]").First;
        await address.ClickAsync();
        await address.FillAsync("");
        await address.PressSequentiallyAsync(query, new() { Delay = 35 });
        await page.WaitForTimeoutAsync(900);
        var suggestion = page.Locator("button.address-suggestions__item, .address-suggestions__item, .kb-address__suggest button").First;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync(new() { Timeout = 10_000 });
                return;
            }

            var hintOk = await page.EvaluateAsync<bool>(
                "() => { const hint = document.querySelector('.ob-fields .ob-wizard__hint'); return !!(hint?.textContent?.includes('✓')); }");
            if (hintOk)
            {
                return;
            }

            await page.WaitForTimeoutAsync(500);
        }

        throw new InvalidOperationException("Address autocomplete did not resolve.");
    }

    internal static async Task FillBirthDateAsync(IPage page)
    {
        var birthText = page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").First;
        if (await birthText.CountAsync() > 0)
        {
            await birthText.FillAsync("15-06-2000");
            await birthText.EvaluateAsync("el => el.dispatchEvent(new Event('blur', { bubbles: true }))");
            return;
        }

        var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birth.CountAsync() > 0)
        {
            await birth.FillAsync("2000-06-15");
            await birth.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }
    }

    private static async Task<string> TargetSelectorAsync(ILocator target)
    {
        return await target.EvaluateAsync<string>(
            """
            el => {
              if (el.id) return `#${CSS.escape(el.id)}`;
              const testId = el.closest('[data-testid]')?.getAttribute('data-testid');
              if (testId) return `[data-testid="${testId}"]`;
              return el.tagName.toLowerCase();
            }
            """);
    }

    private sealed class HitAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
    }
}
