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

        await WaitForCircuitReadyAsync(page, 90_000);
        var mobile = (page.ViewportSize?.Width ?? 1440) < 600;
        var count = Math.Min(await chips.CountAsync(), 6);
        for (var i = 0; i < count; i++)
        {
            await TryActivateWorkStatusChipAsync(page, chips.Nth(i), mobile);
            if (await WaitForWorkStatusSelectedAsync(page, 2_500))
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
        const int selectionTimeoutMs = 2_500;
        const int maxChipAttempts = 6;

        await DismissAddressSuggestionsAsync(page);
        await page.WaitForTimeoutAsync(550);
        await WaitForCircuitReadyAsync(page, 10_000);
        var chips = page.Locator(".work-status-field .work-status-chips .lobsy-choice-tile");
        await chips.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        if (await WaitForWorkStatusSelectedAsync(page, 400))
        {
            return;
        }

        await WaitForCircuitReadyAsync(page, 8_000);
        var mobile = (page.ViewportSize?.Width ?? 1440) < 600;

        foreach (var label in new[] { "Scholier", "Student", "Stagiair" })
        {
            var quick = page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true });
            if (await quick.CountAsync() == 0)
            {
                continue;
            }

            await TryActivateWorkStatusChipAsync(page, quick.First, mobile);
            if (await WaitForWorkStatusSelectedAsync(page, selectionTimeoutMs))
            {
                return;
            }
        }

        var count = Math.Min(await chips.CountAsync(), maxChipAttempts);
        for (var i = 0; i < count; i++)
        {
            await TryActivateWorkStatusChipAsync(page, chips.Nth(i), mobile);
            if (await WaitForWorkStatusSelectedAsync(page, selectionTimeoutMs))
            {
                return;
            }
        }

        var audits = await DescribeHitAuditsAsync(page, [
            (".work-status-chips .lobsy-choice-tile", "werkstatus-chip"),
            ("[data-testid=onboarding-next]", "volgende"),
        ]);
        Assert.Fail($"werkstatus chip did not select after trying all chips. Hit audits: {audits}");
    }

    private static async Task TryActivateWorkStatusChipAsync(IPage page, ILocator chip, bool mobile)
    {
        await chip.ScrollIntoViewIfNeededAsync();
        await WaitForCircuitReadyAsync(page, 5_000);
        try
        {
            if (mobile)
            {
                await chip.TapAsync(new() { Timeout = 5_000 });
            }
            else
            {
                await chip.ClickAsync(new() { Timeout = 5_000 });
            }
        }
        catch (PlaywrightException)
        {
            await chip.ClickAsync(new() { Force = true, Timeout = 5_000 });
        }

        if (await WaitForWorkStatusSelectedAsync(page, 600))
        {
            return;
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

    internal static async Task TypeBirthDateSlowlyAsync(IPage page, string digits = "15062000")
    {
        var field = OnboardingBirthDateRoot(page);
        await field.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var ssrInput = page.Locator("#onboarding-field-birthdate input[data-ob-client=birthDate]").First;
        if (await ssrInput.CountAsync() > 0)
        {
            await ssrInput.ClickAsync(new() { Force = true });
            await ssrInput.FillAsync("");
            var formatted = digits.Length >= 8
                ? $"{digits[..2]}-{digits.Substring(2, 2)}-{digits.Substring(4, 4)}"
                : digits;
            await ssrInput.PressSequentiallyAsync(formatted, new() { Delay = 80 });
            await page.WaitForTimeoutAsync(600);
            await WaitForCircuitReadyAsync(page);
            return;
        }

        var text = field.Locator(".jobsy-date-field__text").First;
        await text.ClickAsync(new() { Force = true });
        await text.FillAsync("");
        await text.PressSequentiallyAsync(digits, new() { Delay = 80 });
        await page.WaitForTimeoutAsync(600);
        await WaitForCircuitReadyAsync(page);
    }

    internal static async Task TypeHomeAddressSlowlyAsync(IPage page, string query = "2671GB")
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var address = page.Locator("#onboarding-home-address").First;
        if (await address.CountAsync() == 0)
        {
            address = page.Locator(".ob-fields input[inputmode=text]").First;
        }

        await address.ScrollIntoViewIfNeededAsync();
        await address.ClickAsync(new() { Force = true });
        await address.FillAsync("");
        await address.PressSequentiallyAsync(query, new() { Delay = 90 });
        await page.WaitForTimeoutAsync(1_200);
        await DismissAddressSuggestionsAsync(page);
        await WaitForCircuitReadyAsync(page);
    }

    internal static async Task DismissAddressSuggestionsAsync(IPage page)
    {
        var address = page.Locator("#onboarding-home-address").First;
        if (await address.CountAsync() == 0)
        {
            return;
        }

        await address.BlurAsync();
        await page.Keyboard.PressAsync("Escape");
        var firstName = page.Locator("input[autocomplete=given-name]").First;
        if (await firstName.CountAsync() > 0)
        {
            await firstName.ClickAsync(new() { Force = true });
        }
        else
        {
            await page.Locator(".ob-fields__field-row input").First.ClickAsync(new() { Force = true });
        }

        await page.WaitForTimeoutAsync(250);
    }

    internal static Task WaitForCircuitReadyAsync(IPage page, int timeoutMs = 90_000)
        => KbE2e.WaitForCircuitOrSsrReadyAsync(page, timeoutMs);

    internal static ILocator OnboardingStep1Illustrated(IPage page) =>
        page.Locator("[data-testid=onboarding-step-1-illustrated]").First;

    internal static async Task AssertChoiceTileTogglesWithinMsAsync(IPage page, ILocator tile, int maxMs = 300)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_E2E_FUNCTIONAL_GATE") ?? "").Trim() == "1")
        {
            maxMs = Math.Max(maxMs, 1_200);
        }

        await tile.ScrollIntoViewIfNeededAsync();
        await WaitForCircuitReadyAsync(page);
        var before = await tile.GetAttributeAsync("aria-pressed") ?? "false";
        var started = DateTime.UtcNow;
        await tile.ClickAsync(new() { Timeout = 10_000, Force = true });
        var toggled = false;
        for (var waited = 0; waited <= maxMs; waited += 50)
        {
            var now = await tile.GetAttributeAsync("aria-pressed") ?? "false";
            if (now != before)
            {
                toggled = true;
                break;
            }

            await page.WaitForTimeoutAsync(50);
        }

        var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
        Assert.True(toggled, $"Choice tile did not toggle within {maxMs}ms (took {elapsed:F0}ms).");
        Assert.True(elapsed <= maxMs + 120, $"Choice tile toggle took {elapsed:F0}ms (budget {maxMs}ms).");
        await WaitForCircuitReadyAsync(page);
    }

    internal static async Task FillKennismakenBasicsExceptWorkStatusAsync(IPage page)
    {
        await page.Locator("input[autocomplete=given-name]").FillAsync("Func");
        await page.Locator("input[autocomplete=family-name]").FillAsync("Test");
        await ResolveHomeAddressAsync(page, "2671GB");
        await DismissAddressSuggestionsAsync(page);
        await FillBirthDateAsync(page);
        await WaitForCircuitReadyAsync(page);
    }

    internal static async Task InstallReloadCounterAsync(IPage page)
    {
        await page.AddInitScriptAsync(
            """
            () => {
              if (window.__lobsyNavCountInstalled) return;
              window.__lobsyNavCountInstalled = true;
              window.__lobsyNavCount = 0;
              window.addEventListener('load', () => { window.__lobsyNavCount = (window.__lobsyNavCount || 0) + 1; });
            }
            """);
    }

    internal static async Task AssertNoUnpromptedReloadAsync(IPage page, string label)
    {
        var navCount = await page.EvaluateAsync<int>("() => window.__lobsyNavCount || 0");
        Assert.True(navCount == 0, $"{label}: expected no full document reload (load count={navCount}).");
    }

    internal static async Task WaitForStepIllustrationLoadedAsync(IPage page, int step)
    {
        if (step is >= 1 and <= 6)
        {
            var mobile = (page.ViewportSize?.Width ?? 1440) < 768;
            var selector = mobile
                ? "[data-testid=onboarding-mobile-hero-img]"
                : "[data-testid=onboarding-illustration-img]";
            await page.Locator(selector).First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
            await page.WaitForFunctionAsync(
                """
                ({ selector }) => {
                  const img = document.querySelector(selector);
                  if (!(img instanceof HTMLImageElement)) return false;
                  return img.complete && img.naturalWidth > 0;
                }
                """,
                new { selector },
                new() { Timeout = 60_000 });
            return;
        }

        if (step is >= 7 and <= 10)
        {
            var mobileHero = page.Locator(".test-block-stage__mobile-hero--band, .test-block-stage__mobile-hero--compact").First;
            if (await mobileHero.CountAsync() > 0 && await mobileHero.IsVisibleAsync())
            {
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      const band = document.querySelector('.test-block-stage__mobile-hero--band, .test-block-stage__mobile-hero--compact');
                      if (!band) return false;
                      const img = band.querySelector('img');
                      if (img instanceof HTMLImageElement) {
                        return img.complete && img.naturalWidth > 0;
                      }
                      const bg = getComputedStyle(band).backgroundImage;
                      return bg && bg !== 'none';
                    }
                    """,
                    null,
                    new() { Timeout = 60_000 });
                return;
            }

            var compactArt = page.Locator("[data-testid=test-block-stage] [data-testid=onboarding-illustration-img]").First;
            if (await compactArt.CountAsync() > 0)
            {
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      const img = document.querySelector('[data-testid=test-block-stage] [data-testid=onboarding-illustration-img]');
                      return img instanceof HTMLImageElement && img.complete && img.naturalWidth > 0;
                    }
                    """,
                    null,
                    new() { Timeout = 60_000 });
            }
        }
    }

    internal static async Task AssertOnboardingStepInteractiveAsync(IPage page, int step)
    {
        await WaitForCircuitReadyAsync(page);
        await WaitForStepIllustrationLoadedAsync(page, step);
        await AssertNoUnpromptedReloadAsync(page, $"step-{step}");

        var reconnect = page.Locator("#components-reconnect-modal.components-reconnect-show, #components-reconnect-modal.components-reconnect-failed");
        Assert.Equal(0, await reconnect.CountAsync());

        if (step is >= 7 and <= 10)
        {
            if (await page.Locator(".test-likert").CountAsync() > 0)
            {
                var likert = page.Locator(".test-likert__opt").First;
                await likert.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
                await AssertHitTargetAsync(page, likert, $"step-{step}-likert");
                return;
            }

            if (await page.Locator(".ob-wizard__consent input[type=checkbox]").CountAsync() > 0)
            {
                var consent = page.Locator(".ob-wizard__consent input[type=checkbox]").First;
                await AssertHitTargetAsync(page, consent, $"step-{step}-consent");
                return;
            }
        }

        if (step == 2)
        {
            var preset = page.Locator(".ob-wizard__presets .lobsy-choice-tile").First;
            await AssertHitTargetAsync(page, preset, "step-2-preset");
            return;
        }

        if (step == 3)
        {
            var transport = page.Locator("#onboarding-field-transport .lobsy-choice-tile").First;
            await AssertHitTargetAsync(page, transport, "step-3-transport");
            return;
        }

        var next = page.GetByTestId("onboarding-next");
        if (await next.CountAsync() > 0)
        {
            await AssertHitTargetAsync(page, next, $"step-{step}-volgende");
        }
    }

    internal static async Task ClickWizardNextWhenVisibleAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var next = page.GetByTestId("onboarding-next");
        if (await next.CountAsync() > 0 && await next.IsVisibleAsync())
        {
            var stepBefore = await ReadSsrFormActiveStepAsync(page);
            await PrepareSsrStepBeforeNextAsync(page, stepBefore);
            await next.ClickAsync(new() { Force = true, Timeout = 20_000 });
            await WaitForSsrMainStepAdvanceAsync(page, stepBefore);
            return;
        }

        var flowNext = page.Locator("button.test-flow__next:not([disabled])").First;
        if (await flowNext.CountAsync() > 0)
        {
            await flowNext.ClickAsync(new() { Force = true, Timeout = 15_000 });
            await page.WaitForTimeoutAsync(400);
        }
    }

    internal static async Task AcceptTestConsentIfPresentAsync(IPage page)
    {
        var box = page.Locator(".ob-wizard__consent input[type=checkbox], .ob-wizard__card input[type=checkbox]").First;
        if (await box.CountAsync() == 0)
        {
            return;
        }

        await box.CheckAsync(new() { Force = true });
        await ClickWizardNextWhenVisibleAsync(page);
        await page.WaitForTimeoutAsync(600);
    }

    internal static async Task AdvanceThroughMiniBlockIfPresentAsync(IPage page, int maxAnswers = 16)
    {
        for (var i = 0; i < maxAnswers && await page.Locator(".test-likert").CountAsync() > 0; i++)
        {
            await page.Locator(".test-likert__opt").Nth(2).ClickAsync(new() { Force = true, Timeout = 15_000 });
            await page.WaitForTimeoutAsync(350);
            await ClickWizardNextWhenVisibleAsync(page);
            await WaitForCircuitReadyAsync(page);
        }
    }

    internal static async Task WalkWizardSteps1Through10WithNextAsync(IPage page, string baseUrl)
    {
        await InstallReloadCounterAsync(page);
        await RegisterFreshCandidateAsync(page, baseUrl, "func-steps-1-10");
        await BeginKennismakenStepAsync(page, baseUrl);
        await page.EvaluateAsync("() => { window.__lobsyNavCount = 0; }");

        await FillKennismakenBasicsExceptWorkStatusAsync(page);
        await SelectAnyWorkStatusChipAsync(page);

        for (var step = 1; step <= 10; step++)
        {
            await AssertOnboardingStepInteractiveAsync(page, step);

            switch (step)
            {
                case 1:
                    await ClickWizardNextWhenVisibleAsync(page);
                    await page.Locator("[data-testid=onboarding-step-2-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
                    await WaitForStepIllustrationLoadedAsync(page, 2);
                    break;
                case 2:
                    await page.Locator(".ob-wizard__presets .lobsy-choice-tile").First.ClickAsync(new() { Force = true });
                    await ClickWizardNextWhenVisibleAsync(page);
                    await page.Locator("[data-testid=onboarding-step-3-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
                    await WaitForStepIllustrationLoadedAsync(page, 3);
                    break;
                case 3:
                    await page.Locator("#onboarding-field-transport .lobsy-choice-tile").First.ClickAsync(new() { Force = true });
                    await page.Locator(".ob-wizard__transport .lobsy-choice-tile").Filter(new() { HasText = "30" }).First.ClickAsync(new() { Force = true });
                    await ClickWizardNextWhenVisibleAsync(page);
                    await page.WaitForTimeoutAsync(500);
                    break;
                case 4:
                case 5:
                    await ClickWizardNextWhenVisibleAsync(page);
                    await page.WaitForTimeoutAsync(500);
                    break;
                case 6:
                    var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex("Weet ik nog niet", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
                    if (await unknown.CountAsync() > 0)
                    {
                        await unknown.First.ClickAsync(new() { Force = true });
                    }
                    else
                    {
                        await ClickWizardNextWhenVisibleAsync(page);
                    }

                    await page.WaitForTimeoutAsync(600);
                    break;
                case 7:
                    await AcceptTestConsentIfPresentAsync(page);
                    await AdvanceThroughMiniBlockIfPresentAsync(page);
                    await ClickWizardNextWhenVisibleAsync(page);
                    await page.WaitForTimeoutAsync(600);
                    break;
                default:
                    await AdvanceThroughMiniBlockIfPresentAsync(page);
                    await ClickWizardNextWhenVisibleAsync(page);
                    await page.WaitForTimeoutAsync(600);
                    break;
            }

            await WaitForCircuitReadyAsync(page);
        }

        await AssertNoUnpromptedReloadAsync(page, "after-step-10");
        Assert.True(
            await page.Locator(".test-likert").CountAsync() > 0
            || await page.Locator(".ob-wizard__finish").CountAsync() > 0
            || page.Url.Contains("/candidate/start", StringComparison.OrdinalIgnoreCase),
            "Expected to reach test flow or finish after step 10 walk.");
    }

    internal static async Task AssertRapidWizardChipInteractionsSteps1To3Async(IPage page, string baseUrl)
    {
        await BeginKennismakenStepAsync(page, baseUrl);
        await WaitForCircuitReadyAsync(page);

        var workStatus = page.Locator(".work-status-chips .lobsy-choice-tile");
        var workCount = await workStatus.CountAsync();
        Assert.True(workCount > 0, "Expected werkstatus chips on step 1.");
        for (var i = 0; i < Math.Min(3, workCount); i++)
        {
            await AssertChoiceTileTogglesWithinMsAsync(page, workStatus.Nth(i));
        }

        await SelectAnyWorkStatusChipAsync(page);
        await FillKennismakenBasicsExceptWorkStatusAsync(page);
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-step-2-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);

        var presets = page.Locator(".ob-wizard__presets .lobsy-choice-tile");
        await presets.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var presetCount = await presets.CountAsync();
        for (var i = 0; i < Math.Min(3, presetCount); i++)
        {
            await AssertChoiceTileTogglesWithinMsAsync(page, presets.Nth(i));
        }

        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-step-3-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);

        var transport = page.Locator(".ob-wizard__transport .lobsy-choice-tile").First;
        await transport.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var transportTiles = page.Locator("#onboarding-field-transport .lobsy-choice-tile");
        var transportCount = await transportTiles.CountAsync();
        for (var i = 0; i < Math.Min(3, transportCount); i++)
        {
            await AssertChoiceTileTogglesWithinMsAsync(page, transportTiles.Nth(i));
        }

        await AssertHitTargetAsync(page, page.GetByTestId("onboarding-next"), "volgende-after-chips");
    }

    internal static async Task AssertBirthDateFieldInteractiveAsync(IPage page)
    {
        var ssrBirth = page.Locator("#onboarding-field-birthdate input[data-ob-client=birthDate]").First;
        if (await ssrBirth.CountAsync() > 0)
        {
            await AssertHitTargetAsync(page, ssrBirth, "birth date ssr field");
            await ssrBirth.FillAsync("15-06-2000");
            var ssrValue = await ssrBirth.InputValueAsync();
            Assert.Contains("15", ssrValue, StringComparison.Ordinal);
            return;
        }

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
        _ = await RegisterFreshCandidateEmailAsync(page, baseUrl, emailPrefix);
    }

    internal static async Task<string> RegisterFreshCandidateEmailAsync(IPage page, string baseUrl, string emailPrefix)
    {
        var email = $"{emailPrefix}.{Guid.NewGuid():N}@jobsy.local";
        await CandidateJourneyE2e.ClearCapturedEmailCodesAsync(baseUrl);
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Func");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 90_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code), "Expected OTP from E2E mail capture.");
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });
        return email;
    }

    internal static async Task AssertCandidateHeaderVisibleAsync(IPage page)
    {
        await Assertions.Expect(page.Locator("[data-testid=app-shell-header]")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator(".account-menu__toggle").First)
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    internal static async Task LogoutViaHeaderAsync(IPage page, bool mobile)
    {
        await AssertCandidateHeaderVisibleAsync(page);
        await page.Locator(".account-menu__toggle").First.ClickAsync(new() { Force = mobile });
        var logout = page.GetByTestId("auth-logout-link");
        await Assertions.Expect(logout).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await logout.ClickAsync(new() { Force = mobile });
        await page.WaitForFunctionAsync(
            """
            () => {
              const path = location.pathname || '/';
              if (path.includes('/login') || path.includes('/account-maken')) return true;
              const menu = document.querySelector('.account-menu.is-open');
              const toggle = document.querySelector('.account-menu__toggle');
              return !toggle || !document.querySelector('.auth-user__name');
            }
            """,
            null,
            new() { Timeout = 90_000 });
    }

    internal static async Task OpenStartWelcomeAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page, 120_000);
        var rustigStart = page.Locator("[data-ob-rustig-screen=start]");
        var legacyWelcome = page.Locator(".ob-wizard__welcome");
        try
        {
            await rustigStart.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            await legacyWelcome.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        }
    }

    internal static async Task ClickBeginnenOnWelcomeAsync(IPage page, bool mobile)
    {
        var rustigStart = page.Locator("[data-ob-rustig-screen=start]");
        var useRustig = await rustigStart.CountAsync() > 0;
        if (useRustig)
        {
            await rustigStart.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }

        var begin = page.GetByTestId("onboarding-begin");
        if (await begin.CountAsync() == 0)
        {
            begin = page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("^(Beginnen|Aan de slag)$") });
        }

        await begin.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        if (!mobile)
        {
            await AssertHitTargetAsync(page, begin, "onboarding-begin");
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await begin.ClickAsync(new() { Timeout = 30_000, Force = mobile });
                break;
            }
            catch (PlaywrightException) when (mobile && attempt < 2)
            {
                await page.WaitForTimeoutAsync(400);
            }
        }

        if (useRustig)
        {
            await OnboardingRustigStepScreen(page, 1).WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
        else
        {
            await OnboardingStep1Illustrated(page).WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
    }

    internal static ILocator OnboardingRustigStepScreen(IPage page, int step)
        => page.Locator($"[data-ob-rustig-screen=step-{step:D2}]");

    internal static async Task AssertOnboardingRustigStepScreenAsync(IPage page, int step)
    {
        await OnboardingRustigStepScreen(page, step).WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });

        var legacyMobileTop = page.Locator(".ob-wizard__mobile-top");
        if (await legacyMobileTop.CountAsync() > 0 && await legacyMobileTop.First.IsVisibleAsync())
        {
            Assert.Fail(
                $"Expected rustig step {step:D2} but legacy mobile chrome (.ob-wizard__mobile-top) is visible; use data-ob-rustig-screen only in capture/audit.");
        }
    }

    internal static async Task SelectRustigTransportAndTravelAsync(IPage page, bool mobile, int maxTravelMinutes = 30)
    {
        var transportTile = page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First;
        await transportTile.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await transportTile.ClickAsync(new() { Force = mobile, Timeout = 30_000 });
        await WaitForCircuitReadyAsync(page, 30_000);

        var travel = page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__travel-times .lobsy-choice-tile")
            .Filter(new() { HasText = maxTravelMinutes.ToString() });
        if (await travel.CountAsync() == 0)
        {
            travel = page.Locator(".ob-wizard__travel-times .lobsy-choice-tile, .ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button");
        }

        await travel.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await travel.First.ClickAsync(new() { Force = mobile, Timeout = 30_000 });
        await WaitForCircuitReadyAsync(page, 30_000);
    }

    internal static async Task ReachOnboardingRustigStepAsync(IPage page, int targetStep, bool mobile)
    {
        if (targetStep is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(targetStep), targetStep, "Rustig capture/audit supports steps 1–5.");
        }

        await AssertOnboardingRustigStepScreenAsync(page, 1);
        if (targetStep == 1)
        {
            return;
        }

        await FillKennismakenBasicsExceptWorkStatusAsync(page);
        await SelectAnyWorkStatusChipAsync(page);
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await WaitForCircuitReadyAsync(page, 60_000);
        await AssertOnboardingRustigStepScreenAsync(page, 2);
        if (targetStep == 2)
        {
            return;
        }

        var preset = page.Locator(
            ".ob-rustig .ob-wizard__presets .lobsy-choice-tile, .ob-rustig .ob-wizard__presets button, .ob-wizard__presets .lobsy-choice-tile").First;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (await OnboardingRustigStepScreen(page, 3).IsVisibleAsync())
            {
                break;
            }

            await AssertOnboardingRustigStepScreenAsync(page, 2);
            await preset.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await preset.ClickAsync(new() { Force = mobile, Timeout = 30_000 });
            await WaitForCircuitReadyAsync(page, 60_000);
            await VroegResultaatPlaywrightTests.ClickNextAsync(page);
            await WaitForCircuitReadyAsync(page, 60_000);
        }

        await AssertOnboardingRustigStepScreenAsync(page, 3);
        if (targetStep == 3)
        {
            return;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (await OnboardingRustigStepScreen(page, 4).IsVisibleAsync())
            {
                break;
            }

            if (await OnboardingRustigStepScreen(page, 3).IsVisibleAsync())
            {
                await SelectRustigTransportAndTravelAsync(page, mobile);
                await ClickWizardNextWhenVisibleAsync(page);
                await WaitForCircuitReadyAsync(page, 60_000);
                continue;
            }

            await page.WaitForTimeoutAsync(400);
        }

        await AssertOnboardingRustigStepScreenAsync(page, 4);
        if (targetStep == 4)
        {
            return;
        }

        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await AssertOnboardingRustigStepScreenAsync(page, 5);
    }

    internal static async Task CaptureOnboardingStartHotfixAsync(IPage page, string tag)
    {
        var dir = ResolveOnboardingStartCaptureDir();
        if (dir is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(dir);
            await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"start-{tag}.png"), FullPage = true });
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Optional evidence; never fail the functional gate when the runner cannot write.
        }
    }

    private static string? ResolveOnboardingStartCaptureDir()
    {
        foreach (var candidate in new[]
                 {
                     Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_START_ARTIFACTS"),
                     Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT") is { } root
                         ? Path.Combine(root, "onboarding-start-hotfix")
                         : null,
                     Path.Combine(AppContext.BaseDirectory, "test-output", "onboarding-start-hotfix"),
                     Path.Combine(Path.GetTempPath(), "jobsy-onboarding-start-hotfix"),
                     "/workspace/artifacts/onboarding-start-hotfix",
                 })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (TryEnsureWritableDirectory(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool TryEnsureWritableDirectory(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            var probe = Path.Combine(dir, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    internal static async Task BeginKennismakenStepAsync(IPage page, string baseUrl)
    {
        var mobile = (page.ViewportSize?.Width ?? 1440) < 600;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await WaitForCircuitReadyAsync(page, 120_000);
            try
            {
                if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
                {
                    await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
                    await ClickBeginnenOnWelcomeAsync(page, mobile);
                }
                else
                {
                    await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(page);
                }

                break;
            }
            catch (TimeoutException) when (attempt == 0)
            {
                await page.WaitForTimeoutAsync(1500);
            }
        }

        await OnboardingStep1Illustrated(page).WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.Locator(".work-status-field .work-status-chips .lobsy-choice-tile").First
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

    internal static async Task AdvanceFromTransportToTestConsentAsync(IPage page)
    {
        var nearby = page.Locator("[data-testid=onboarding-nearby-panel-desktop], [data-testid=onboarding-nearby-panel-mobile]").First;
        try
        {
            await nearby.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 20_000 });
        }
        catch (TimeoutException)
        {
            // Nearby panel depends on API signal; still advance through steps 4–6.
        }

        await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
    }

    internal static async Task AdvanceStepsFourThroughSixToConsentAsync(IPage page)
    {
        for (var step = 4; step <= 6; step++)
        {
            if (await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).IsVisibleAsync())
            {
                return;
            }

            if (await page.Locator(".ob-wizard__consent input[type=checkbox]").CountAsync() > 0)
            {
                return;
            }

            if (step is 4 or 5)
            {
                var skip = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex("Overslaan", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
                if (await skip.CountAsync() > 0)
                {
                    await skip.First.ClickAsync(new() { Timeout = 8_000, Force = true });
                    await page.WaitForTimeoutAsync(450);
                    continue;
                }

                await VroegResultaatPlaywrightTests.ClickNextAsync(page);
                await page.WaitForTimeoutAsync(500);
                continue;
            }

            var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex("Weet ik nog niet", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
            if (await unknown.CountAsync() > 0)
            {
                await unknown.First.ClickAsync(new() { Timeout = 8_000, Force = true });
            }
            else
            {
                await VroegResultaatPlaywrightTests.ClickNextAsync(page);
            }

            await page.WaitForTimeoutAsync(600);
        }

        await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
    }

    internal static async Task RunThroughWizardToFinishAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(page);
        await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await AdvanceStepsFourThroughSixToConsentAsync(page);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });

        var mobile = (page.ViewportSize?.Width ?? 1440) < 600;
        if (mobile)
        {
            Assert.Equal(0, await page.Locator("[data-testid=early-reward-nearby]").CountAsync());
        }

        await SkipTestsLaterAndWaitForBanenkaartAsync(page);
        await WaitForBanenkaartDiscoveryShellAsync(page);
    }

    internal static async Task SkipTestsLaterAndWaitForBanenkaartAsync(IPage page)
    {
        if (page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
        {
            await WaitForBanenkaartDiscoveryShellAsync(page);
            return;
        }

        await page.WaitForFunctionAsync(
            KbE2e.CircuitOrSsrReadyJavaScript,
            null,
            new() { Timeout = 90_000 });

        var mobile = page.ViewportSize is { Width: < 600 };
        var consentHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" });
        var flowLater = page.Locator(".test-flow [data-testid=onboarding-tests-later], .test-flow__later").First;

        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
            {
                await WaitForBanenkaartDiscoveryShellAsync(page);
                return;
            }

            if (await consentHeading.IsVisibleAsync())
            {
                await page.WaitForTimeoutAsync(mobile ? 800 : 500);
                var later = page.GetByTestId("onboarding-tests-later").First;
                await later.ScrollIntoViewIfNeededAsync();
                await Assertions.Expect(later).ToBeEnabledAsync(new() { Timeout = 30_000 });
                for (var clickTry = 0; clickTry < 2; clickTry++)
                {
                    await later.ClickAsync(new() { Force = true, Timeout = 30_000 });
                    try
                    {
                        await page.WaitForURLAsync(
                            u => u.ToString().Contains("/banenkaart", StringComparison.OrdinalIgnoreCase),
                            new() { Timeout = 90_000 });
                        break;
                    }
                    catch (TimeoutException) when (clickTry == 0)
                    {
                        await page.WaitForTimeoutAsync(800);
                    }
                }

                await WaitForBanenkaartDiscoveryShellAsync(page);
                return;
            }

            if (await flowLater.CountAsync() > 0)
            {
                await flowLater.ScrollIntoViewIfNeededAsync();
                await flowLater.ClickAsync(new() { Force = true, Timeout = 30_000 });
                await WaitForBanenkaartLocationAsync(page);
                await WaitForBanenkaartDiscoveryShellAsync(page);
                return;
            }

            await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
            await page.WaitForTimeoutAsync(mobile ? 900 : 600);
        }

        try
        {
            await WaitForBanenkaartDiscoveryShellAsync(page);
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

    private static Task WaitForBanenkaartLocationAsync(IPage page)
        => page.WaitForFunctionAsync(
            """
            () => {
              const path = (location.pathname || '').toLowerCase();
              return path === '/banenkaart' || path.startsWith('/banenkaart/');
            }
            """,
            null,
            new() { Timeout = 180_000 });

    private static async Task WaitForBanenkaartDiscoveryShellAsync(IPage page)
    {
        if (!page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase))
        {
            await WaitForBanenkaartLocationAsync(page);
        }

        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await WaitForCircuitReadyAsync(page, 120_000);

        await page.Locator(
                ".jobsy-discovery, [data-testid=uwv-banenkaart-mock], [data-testid=uwv-kansberoepen-toggle], [data-testid=kb-keyword-search], #jobsy-map-boot")
            .First
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 180_000 });
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
        var ssrBirth = page.Locator("#onboarding-field-birthdate input[data-ob-client=birthDate]").First;
        if (await ssrBirth.CountAsync() > 0)
        {
            await ssrBirth.ScrollIntoViewIfNeededAsync();
            await ssrBirth.FillAsync("15-06-2000");
            await ssrBirth.EvaluateAsync("el => el.dispatchEvent(new Event('blur', { bubbles: true }))");
            return;
        }

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

    internal static async Task<int> ReadOnboardingWizardStepAsync(IPage page)
        => await page.EvaluateAsync<int>(
            """
            () => {
              const el = document.querySelector('[data-onboarding-step]');
              if (!el) return 0;
              const n = parseInt(el.getAttribute('data-onboarding-step') || '0', 10);
              return Number.isFinite(n) ? n : 0;
            }
            """);

    internal static async Task<string?> ReadMiniTestCounterAsync(IPage page)
    {
        var counter = page.Locator(".test-flow__counter").First;
        if (await counter.CountAsync() == 0)
        {
            return null;
        }

        return (await counter.InnerTextAsync()).Trim();
    }

    internal static async Task AcceptOnboardingTestConsentAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
        var box = page.Locator(".ob-wizard__consent input[type=checkbox]").First;
        await box.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await box.CheckAsync(new() { Force = true });
        await page.GetByTestId("onboarding-next").ClickAsync(new() { Force = true, Timeout = 20_000 });
        await page.Locator(".test-likert").First.WaitForAsync(new() { Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
    }

    internal static async Task CompleteCurrentMiniTestBlockAsync(IPage page)
    {
        for (var q = 0; q < 5; q++)
        {
            await page.Locator(".test-likert__opt").Nth(2).ClickAsync(new() { Force = true, Timeout = 20_000 });
            await WaitForCircuitReadyAsync(page);
            await page.WaitForTimeoutAsync(350);
        }

        var flowNext = page.Locator("button.test-flow__next:not([disabled])").First;
        await flowNext.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await flowNext.ClickAsync(new() { Force = true, Timeout = 20_000 });
        await WaitForCircuitReadyAsync(page);
    }

    internal static async Task AdvancePastEarlyInsightIfVisibleAsync(IPage page)
    {
        var next = page.GetByTestId("onboarding-next");
        if (await next.CountAsync() > 0 && await next.IsVisibleAsync())
        {
            var counter = await ReadMiniTestCounterAsync(page);
            if (counter is null)
            {
                await next.ClickAsync(new() { Force = true, Timeout = 20_000 });
                await page.Locator(".test-likert").First.WaitForAsync(new() { Timeout = 90_000 });
                await WaitForCircuitReadyAsync(page);
            }
        }
    }

    internal static async Task AssertOnKompasFinishScreenAsync(IPage page)
    {
        await page.Locator(".ob-wizard__finish, .ob-finale").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 180_000 });
    }

    /// <summary>
    /// Completes all four onboarding mini tests (7–10), reloads once after Leuk, and lands on Kompas without revisiting a finished block.
    /// </summary>
    internal static async Task CompleteFourMiniTestsToKompasWithResumeChecksAsync(IPage page, string baseUrl)
    {
        await RegisterFreshCandidateAsync(page, baseUrl, "func-four-mini");
        await BeginKennismakenStepAsync(page, baseUrl);
        await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await AdvanceFromTransportToTestConsentAsync(page);
        await AcceptOnboardingTestConsentAsync(page);

        var maxStepSeenAfterLeavingLeuk = 7;

        for (var block = 0; block < 4; block++)
        {
            var expectedStep = 7 + block;
            await page.Locator(".test-likert").First.WaitForAsync(new() { Timeout = 90_000 });
            var stepNow = await ReadOnboardingWizardStepAsync(page);
            Assert.True(
                stepNow == expectedStep,
                $"Expected onboarding step {expectedStep} before mini block {block + 1}, got {stepNow}.");

            await CompleteCurrentMiniTestBlockAsync(page);
            await AdvancePastEarlyInsightIfVisibleAsync(page);

            if (expectedStep == 8)
            {
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      const el = document.querySelector('[data-onboarding-step]');
                      if (!el) return false;
                      const step = parseInt(el.getAttribute('data-onboarding-step') || '0', 10);
                      return step >= 9 && document.querySelector('.test-likert');
                    }
                    """,
                    null,
                    new() { Timeout = 120_000 });
                maxStepSeenAfterLeavingLeuk = Math.Max(maxStepSeenAfterLeavingLeuk, await ReadOnboardingWizardStepAsync(page));
                Assert.True(
                    maxStepSeenAfterLeavingLeuk >= 9,
                    "Expected to advance past Leuk (step 8) after completing that block.");

                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await WaitForCircuitReadyAsync(page);
                await WaitForCircuitReadyAsync(page);
                var stepAfterReload = await ReadOnboardingWizardStepAsync(page);
                Assert.True(
                    stepAfterReload >= 9,
                    $"After reload, completed Leuk must not reopen (step 8); got step {stepAfterReload}.");
                if (stepAfterReload == 8)
                {
                    var counter = await ReadMiniTestCounterAsync(page);
                    throw new InvalidOperationException($"Leuk reopened after reload; counter={counter ?? "n/a"}");
                }
            }
        }

        if (await page.Locator(".ob-wizard__finish").CountAsync() == 0)
        {
            var finishNext = page.GetByTestId("onboarding-next");
            if (await finishNext.CountAsync() > 0 && await finishNext.IsVisibleAsync())
            {
                await finishNext.ClickAsync(new() { Force = true, Timeout = 30_000 });
            }
            else
            {
                var flowNext = page.Locator("button.test-flow__next:not([disabled])").First;
                if (await flowNext.CountAsync() > 0)
                {
                    await flowNext.ClickAsync(new() { Force = true, Timeout = 20_000 });
                }
            }

            await WaitForCircuitReadyAsync(page);
        }

        await AssertOnKompasFinishScreenAsync(page);

        var finalStep = await ReadOnboardingWizardStepAsync(page);
        Assert.True(
            finalStep == 0 || await page.Locator(".ob-wizard__finish").CountAsync() > 0,
            "Expected Kompas finish screen after all four mini tests.");
    }

    internal static async Task ClickOnboardingNextAsync(IPage page, bool mobile)
    {
        var next = page.GetByTestId("onboarding-next");
        await next.ScrollIntoViewIfNeededAsync();
        await WaitForCircuitReadyAsync(page, 15_000);
        var stepBefore = await ReadSsrFormActiveStepAsync(page);
        await PrepareSsrStepBeforeNextAsync(page, stepBefore);
        await next.ClickAsync(new() { Force = mobile, Timeout = 30_000 });
        await WaitForSsrMainStepAdvanceAsync(page, stepBefore);
    }

    internal static async Task ClickOnboardingBackAsync(IPage page, bool mobile)
    {
        var back = page.GetByTestId("onboarding-back");
        await back.ScrollIntoViewIfNeededAsync();
        await back.ClickAsync(new() { Force = mobile, Timeout = 30_000 });
        await page.WaitForTimeoutAsync(450);
    }

    private static ILocator OnboardingBirthDateRoot(IPage page) =>
        page.Locator("[data-testid=onboarding-birth-date], #onboarding-field-birthdate").First;

    private static async Task<int> ReadSsrFormActiveStepAsync(IPage page)
        => await page.EvaluateAsync<int>(
            """
            () => {
              const f = document.querySelector('form[data-ob-ssr-form]');
              if (!f) return 0;
              const n = parseInt(f.getAttribute('data-ob-active-step') || '0', 10);
              return Number.isFinite(n) ? n : 0;
            }
            """);

    private static async Task PrepareSsrStepBeforeNextAsync(IPage page, int stepBefore)
    {
        if (stepBefore != 6)
        {
            return;
        }

        var dream = page.Locator("#onboarding-rustig-dream-input").First;
        if (await dream.CountAsync() > 0)
        {
            await dream.BlurAsync();
            await page.WaitForTimeoutAsync(250);
        }
    }

    private static async Task WaitForSsrMainStepAdvanceAsync(IPage page, int fromStep, int timeoutMs = 90_000)
    {
        if (fromStep is < 1 or > 6 || await page.Locator("form[data-ob-ssr-form]").CountAsync() == 0)
        {
            await page.WaitForTimeoutAsync(450);
            return;
        }

        await page.WaitForURLAsync($"**/candidate/start?step={fromStep + 1}**", new() { Timeout = timeoutMs });
        await WaitForCircuitReadyAsync(page, timeoutMs);
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
