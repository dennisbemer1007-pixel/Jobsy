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

    internal static async Task SelectAnyWorkStatusChipAsync(IPage page)
    {
        var chips = page.Locator(".work-status-field .work-status-chips .lobsy-choice-tile");
        if (await chips.CountAsync() == 0)
        {
            return;
        }

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });

        var mobile = page.ViewportSize is { Width: < 600 };
        for (var i = 0; i < await chips.CountAsync(); i++)
        {
            var chip = chips.Nth(i);
            await chip.ScrollIntoViewIfNeededAsync();
            try
            {
                if (mobile)
                {
                    await chip.TapAsync(new() { Timeout = 10_000 });
                }
                else
                {
                    await chip.ClickAsync(new() { Timeout = 10_000 });
                }
            }
            catch (PlaywrightException)
            {
                await chip.ClickAsync(new() { Force = true, Timeout = 10_000 });
            }

            if (await WaitForWorkStatusSelectedAsync(page, 8_000))
            {
                return;
            }
        }

        var audits = await DescribeHitAuditsAsync(page, [
            (".work-status-chips .lobsy-choice-tile", "werkstatus-chip"),
            ("[data-testid=onboarding-next]", "volgende"),
        ]);
        throw new InvalidOperationException($"Could not select a work-status chip. Hit audits: {audits}");
    }

    internal static async Task AssertWorkStatusChipTogglesAsync(IPage page)
    {
        var chips = page.Locator(".work-status-field .work-status-chips .lobsy-choice-tile");
        await chips.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);

        var viewport = page.ViewportSize;
        var mobile = viewport is not null && viewport.Width < 600;
        var selected = false;

        for (var round = 0; round < 2 && !selected; round++)
        {

            var count = await chips.CountAsync();
            for (var i = 0; i < count && !selected; i++)
            {
                var chip = chips.Nth(i);
                await chip.ScrollIntoViewIfNeededAsync();
                await page.WaitForTimeoutAsync(200);

                try
                {
                    if (mobile)
                    {
                        await chip.TapAsync(new() { Timeout = 15_000 });
                    }
                    else
                    {
                        await chip.ClickAsync(new() { Timeout = 15_000 });
                    }
                }
                catch (PlaywrightException)
                {
                    await chip.ClickAsync(new() { Timeout = 15_000, Force = true });
                }

                await page.WaitForFunctionAsync(
                    """
                    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'
                    """,
                    null,
                    new() { Timeout = 15_000 });

                selected = await WaitForWorkStatusSelectedAsync(page, 25_000);
                if (selected)
                {
                    break;
                }

                await chip.EvaluateAsync(
                    """
                    el => {
                      if (!(el instanceof HTMLElement)) return;
                      el.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, cancelable: true, pointerType: 'touch' }));
                      el.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, cancelable: true, pointerType: 'touch' }));
                      el.click();
                    }
                    """);
                selected = await WaitForWorkStatusSelectedAsync(page, 12_000);
            }
        }

        if (!selected)
        {
            var audits = await DescribeHitAuditsAsync(page, [
                (".work-status-chips .lobsy-choice-tile", "werkstatus-chip"),
                ("[data-testid=onboarding-next]", "volgende"),
            ]);
            Assert.True(selected, $"werkstatus chip did not select after trying all chips. Hit audits: {audits}");
        }
    }

    internal static async Task<string> DescribeHitAuditsAsync(IPage page, (string Selector, string Label)[] targets)
    {
        var parts = new List<string>();
        foreach (var (selector, label) in targets)
        {
            var audit = await page.EvaluateAsync<string>(
                """
                ({ selector, label }) => {
                  const el = document.querySelector(selector);
                  if (!el) return `${label}:missing`;
                  el.scrollIntoView({ block: 'center', inline: 'nearest' });
                  const r = el.getBoundingClientRect();
                  const x = Math.min(window.innerWidth - 1, Math.max(1, r.left + r.width / 2));
                  const y = Math.min(window.innerHeight - 1, Math.max(1, r.top + r.height / 2));
                  const hit = document.elementFromPoint(x, y);
                  if (!hit) return `${label}:no-hit@(${Math.round(x)},${Math.round(y)})`;
                  const ok = hit === el || el.contains(hit);
                  const tag = hit.tagName.toLowerCase();
                  const cls = hit.className ? '.' + String(hit.className).trim().split(/\\s+/)[0] : '';
                  const testId = hit.getAttribute?.('data-testid');
                  const id = hit.id ? '#' + hit.id : '';
                  const desc = testId ? `[data-testid=${testId}]` : `${tag}${id}${cls}`;
                  return `${label}:${ok ? 'ok' : 'blocked-by-' + desc}@(${Math.round(x)},${Math.round(y)}) circuit=${document.documentElement.getAttribute('data-lobsy-circuit')}`;
                }
                """,
                new { selector, label });
            parts.Add(audit);
        }

        return string.Join("; ", parts);
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

        var mobile = (page.ViewportSize?.Width ?? 1440) < 600;
        if (mobile)
        {
            await page.Locator("[data-testid=onboarding-nearby-panel-mobile]").WaitForAsync(new() { Timeout = 90_000 });
        }
        else
        {
            await page.Locator("[data-testid=onboarding-nearby-panel-desktop], [data-testid=onboarding-nearby-panel-mobile]").First
                .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        }

        if (mobile)
        {
            Assert.Equal(0, await page.Locator("[data-testid=early-reward-nearby]").CountAsync());
        }

        await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
        await SkipTestsLaterAndWaitForBanenkaartAsync(page);
        await page.Locator(".jobsy-discovery").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
    }

    internal static async Task SkipTestsLaterAndWaitForBanenkaartAsync(IPage page)
    {
        if (page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });

        var heading = page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" });
        var footerLater = page.Locator(".ob-wizard__footer [data-testid=onboarding-tests-later]:visible").First;
        var flowLater = page.Locator(".test-flow [data-testid=onboarding-tests-later]:visible, .test-flow__later:visible").First;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (await heading.IsVisibleAsync() && await footerLater.CountAsync() > 0)
            {
                await page.WaitForTimeoutAsync(500);
                await footerLater.ScrollIntoViewIfNeededAsync();
                await Assertions.Expect(footerLater).ToBeEnabledAsync(new() { Timeout = 30_000 });
                await footerLater.ClickAsync(new() { Timeout = 30_000 });
                break;
            }

            if (await flowLater.CountAsync() > 0)
            {
                await flowLater.ScrollIntoViewIfNeededAsync();
                await flowLater.ClickAsync(new() { Force = true, Timeout = 30_000 });
                break;
            }

            await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
            await page.WaitForTimeoutAsync(800);
        }

        try
        {
            await page.Locator(".jobsy-discovery, [data-testid=uwv-kansberoepen-toggle]").First.WaitForAsync(
                new() { State = WaitForSelectorState.Attached, Timeout = 240_000 });
        }
        catch (TimeoutException)
        {
            if (!page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
            {
                var saveError = page.Locator(".ob-wizard .error[role=alert], .ob-wizard .error[role=status]").First;
                if (await saveError.CountAsync() > 0 && await saveError.IsVisibleAsync())
                {
                    var text = await saveError.InnerTextAsync();
                    throw new InvalidOperationException($"Onboarding skip-later failed in UI: {text.Trim()}");
                }
            }

            throw;
        }
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

    private static async Task<bool> WaitForWorkStatusSelectedAsync(IPage page, int timeoutMs)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => !!document.querySelector(
                  '.work-status-chips .lobsy-choice-tile.is-selected, .work-status-chips [aria-pressed="true"]')
                """,
                null,
                new() { Timeout = timeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
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
