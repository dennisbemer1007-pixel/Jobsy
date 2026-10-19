using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class FunctionalOnboardingInteractivityHelpers
{
    internal static async Task AssertHitTargetAsync(IPage page, ILocator target, string label)
    {
        await target.ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(150);
        var selector = await TargetSelectorAsync(target);
        var audit = await page.EvaluateAsync<HitAudit>(
            """
            ({ selector }) => {
              const el = document.querySelector(selector);
              if (!el) return { ok: false, reason: 'missing-target' };
              el.scrollIntoView({ block: 'center', inline: 'nearest' });
              const r = el.getBoundingClientRect();
              if (!Number.isFinite(r.width) || !Number.isFinite(r.height) || r.width <= 0 || r.height <= 0) {
                return { ok: false, reason: 'zero-size-or-non-finite-rect' };
              }
              const x = Math.min(window.innerWidth - 1, Math.max(1, r.left + r.width / 2));
              const y = Math.min(window.innerHeight - 1, Math.max(1, r.top + r.height / 2));
              if (!Number.isFinite(x) || !Number.isFinite(y)) {
                return { ok: false, reason: 'non-finite-hit-coordinates' };
              }
              const hit = document.elementFromPoint(x, y);
              if (!hit) return { ok: false, reason: 'no-hit' };
              const ok = hit === el || el.contains(hit);
              return {
                ok,
                reason: ok ? 'ok' : `blocked-by-${hit.tagName}${hit.className ? '.' + String(hit.className).split(' ')[0] : ''}`
              };
            }
            """,
            new { selector });

        Assert.True(audit.Ok, $"{label}: {audit.Reason}");
    }

    internal static async Task AssertWorkStatusChipTogglesAsync(IPage page)
    {
        var chips = page.Locator(".work-status-chips .lobsy-choice-tile, .work-status-chips .work-status-chip");
        await chips.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);

        var selected = false;
        var count = await chips.CountAsync();
        for (var i = 0; i < count && !selected; i++)
        {
            var chip = chips.Nth(i);
            await AssertHitTargetAsync(page, chip, "werkstatus chip");
            await chip.ClickAsync(new() { Timeout = 10_000, Force = true });
            await page.WaitForTimeoutAsync(450);
            selected = await page.Locator(".work-status-chips .is-selected, .work-status-chips [aria-pressed=\"true\"]").CountAsync() > 0;
        }

        Assert.True(selected, "werkstatus chip did not select after trying all chips");
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
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 120_000 });
            try
            {
                await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(page);
                break;
            }
            catch (TimeoutException) when (attempt == 0)
            {
                await page.WaitForTimeoutAsync(1500);
            }
        }
        await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
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
        await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-nearby-panel-mobile], [data-testid=onboarding-nearby-panel-desktop], [data-testid=early-reward-nearby]").First
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).WaitForAsync(new() { Timeout = 120_000 });
        await page.GetByTestId("onboarding-tests-later").First.ClickAsync(new() { Force = true, Timeout = 20_000 });
        await page.WaitForURLAsync(
            u => u.ToString().Contains("/banenkaart", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 120_000 });
        await page.Locator(".jobsy-discovery").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
    }

    internal static async Task ResolveHomeAddressAsync(IPage page, string query)
    {
        for (var round = 0; round < 2; round++)
        {
            if (await TryResolveHomeAddressOnceAsync(page, query))
            {
                return;
            }

            await page.WaitForTimeoutAsync(1200);
        }

        throw new InvalidOperationException("Address autocomplete did not resolve.");
    }

    private static async Task<bool> TryResolveHomeAddressOnceAsync(IPage page, string query)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var postcode = query.Replace(" ", "", StringComparison.Ordinal);
        var address = page.Locator("#onboarding-home-address").First;
        if (await address.CountAsync() == 0)
        {
            address = page.Locator(".ob-fields input[inputmode=text], .ob-fields input[autocomplete=postal-code]").First;
        }

        await address.ScrollIntoViewIfNeededAsync();
        await address.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await address.FillAsync("");
        await address.PressSequentiallyAsync(postcode, new() { Delay = 45 });
        await page.WaitForTimeoutAsync(1200);

        var suggestion = page.Locator(".ob-fields__address button.address-suggestions__item, button.address-suggestions__item").First;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ScrollIntoViewIfNeededAsync();
                await suggestion.ClickAsync(new() { Timeout = 15_000, Force = true });
                await page.WaitForTimeoutAsync(800);
                return true;
            }

            if (await AddressLooksResolvedAsync(page))
            {
                return true;
            }

            if (attempt == 4)
            {
                await address.PressAsync("Tab");
                await page.WaitForTimeoutAsync(600);
            }

            if (attempt == 8 && postcode.Length == 6)
            {
                await address.ClickAsync(new() { Force = true });
                await address.FillAsync($"{postcode[..4]} {postcode[4..]}");
                await page.EvaluateAsync(
                    """
                    () => {
                      const el = document.getElementById('onboarding-home-address');
                      el?.blur();
                      window.jobsyGeo?.blurElement?.('onboarding-home-address');
                    }
                    """);
                await page.WaitForTimeoutAsync(1500);
            }

            if (attempt == 12)
            {
                await address.ClickAsync(new() { Force = true });
                await address.PressAsync("ArrowDown");
                await address.PressAsync("Enter");
                await page.WaitForTimeoutAsync(800);
            }

            await page.WaitForTimeoutAsync(1000);
        }

        return await AddressLooksResolvedAsync(page);
    }

    private static async Task<bool> AddressLooksResolvedAsync(IPage page)
    {
        var hintOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const hints = document.querySelectorAll('.ob-fields .ob-wizard__hint, .ob-fields__address .ob-wizard__hint');
              for (const hint of hints) {
                if (hint.textContent?.includes('✓')) return true;
              }
              const input = document.getElementById('onboarding-home-address');
              if (input instanceof HTMLInputElement && input.value.replace(/\s/g, '').length >= 6) {
                return true;
              }
              return false;
            }
            """);

        if (hintOk)
        {
            return true;
        }

        return await page.Locator("[data-testid=onboarding-nearby-panel-desktop], [data-testid=onboarding-nearby-panel-mobile]").CountAsync() > 0;
    }

    internal static async Task FillBirthDateAsync(IPage page)
    {
        var birthText = page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").First;
        if (await birthText.CountAsync() > 0)
        {
            await birthText.ScrollIntoViewIfNeededAsync();
            await birthText.ClickAsync(new() { Force = true });
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
