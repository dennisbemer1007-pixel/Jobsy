using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Follow-up usability capture (1440×900 + 390×844) into <c>/cursor/stores/self/usability2</c>.
/// Run: <c>JOBSY_E2E_USABILITY2=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 ConnectionStrings__JobsyDb=… dotnet test --filter Usability2</c>
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateUsability2PlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_USABILITY2"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Usability2_follow_up_desktop_and_mobile()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        Environment.SetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT", "/cursor/stores/self/usability2");
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl));

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in new[]
                 {
                     (KbE2e.Desktop.Width, KbE2e.Desktop.Height, "desktop"),
                     (KbE2e.Mobile.Width, KbE2e.Mobile.Height, "mobile")
                 })
        {
            await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl));
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);

            await Shot(page, tag, "paspoort-overview");
            await VisitPaspoortTabsAsync(page, baseUrl, tag);
            await VisitProfileSaveFeedbackAsync(page, baseUrl, tag);
            await VisitNotificationBellAsync(page, baseUrl, tag);
            await VisitMailSettingsSaveAsync(page, baseUrl, tag);
            await VisitSettingsAndPrivacyHubsAsync(page, baseUrl, tag);
            await VisitPrivacyDataAsync(page, baseUrl, tag);
            await VisitApplyFlowAsync(page, baseUrl, tag);
            await VisitIncompletePassportPdfLockedAsync(page, baseUrl, tag);
            await VisitDnaTestSampleAsync(page, baseUrl, tag);
            await VisitProactiveMatchingAsync(page, baseUrl, tag);
            await VisitApplicationsEmptyStateAsync(page, baseUrl, tag);

            await KbE2e.AssertNoCircuitErrorAsync(page);
        }
    }

    private static async Task Shot(IPage page, string tag, string slug, bool after = false)
    {
        var prefix = after ? "after" : "before";
        var path = Path.Combine(CandidateJourneyE2e.ShotDir(tag), $"{prefix}-{slug}.png");
        await CandidateJourneyE2e.ShotViewportAsync(page, path);
    }

    private static async Task VisitPaspoortTabsAsync(IPage page, string baseUrl, string tag)
    {
        foreach (var tab in new[] { "overview", "dna", "tests", "fit", "career", "proof", "data" })
        {
            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab={tab}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            try
            {
                await page.Locator("main, .passport-page, .panel-page, h1").First.WaitForAsync(new() { Timeout = 45_000 });
            }
            catch (TimeoutException)
            {
                continue;
            }

            await Shot(page, tag, $"paspoort-tab-{tab}");
        }

        if (tag == "desktop")
        {
            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=overview", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);

            var downloadBtn = page.Locator("[data-testid=passport-download-pdf]").First;
            if (await downloadBtn.CountAsync() > 0 && await downloadBtn.IsVisibleAsync())
            {
                try
                {
                    await downloadBtn.ClickAsync(new() { Timeout = 15_000 });
                    await page.WaitForTimeoutAsync(1_200);
                    await Shot(page, tag, "paspoort-pdf-trigger", after: true);
                }
                catch (Exception)
                {
                    // Download may be disabled or blocked in headless; tab screenshots remain.
                }
            }
        }
    }

    private static async Task VisitProfileSaveFeedbackAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=data", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await Shot(page, tag, "profile-data");

        var openForWork = page.Locator(".passport-data__switch-input").First;
        if (await openForWork.CountAsync() > 0)
        {
            var wasOn = await openForWork.IsCheckedAsync();
            await openForWork.SetCheckedAsync(!wasOn);
            await page.Locator(".passport-data .state-message[role=status], .profile-save-bar__message")
                .Filter(new() { HasText = "opgeslagen" })
                .First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
            await Shot(page, tag, "profile-save-feedback", after: true);
            await openForWork.SetCheckedAsync(wasOn);
            await page.WaitForTimeoutAsync(800);
        }

        var privacyToggle = page.Locator("#passport-data-btn-privacy");
        if (await privacyToggle.CountAsync() > 0)
        {
            await privacyToggle.ClickAsync();
            await page.Locator("#consent").WaitForAsync(new() { Timeout = 15_000 });
            await page.EvaluateAsync("() => document.getElementById('consent')?.scrollIntoView({ block: 'center' })");
            await Shot(page, tag, "profile-consent-section", after: true);
        }
    }

    private static async Task VisitNotificationBellAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/applications", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await Shot(page, tag, "notifications-page");

        var bell = page.Locator(".notification-bell__toggle").First;
        if (await bell.CountAsync() > 0)
        {
            await bell.ClickAsync();
            await page.Locator(".notification-bell__panel").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
            await Shot(page, tag, "notification-bell-open", after: true);
            var close = page.Locator(".notification-bell__close").First;
            if (await close.CountAsync() > 0)
            {
                await close.ClickAsync();
            }
        }
    }

    private static async Task VisitMailSettingsSaveAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/account/mail-instellingen", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await Shot(page, tag, "mail-settings");

        var reminder = page.Locator("input[data-reminder-emails]").First;
        if (await reminder.CountAsync() > 0)
        {
            var wasChecked = await reminder.IsCheckedAsync();
            if (wasChecked)
            {
                await reminder.UncheckAsync(new() { Force = true });
            }
            else
            {
                await reminder.CheckAsync(new() { Force = true });
            }

            await page.Locator("button[type=submit]").First.ClickAsync();
            await page.WaitForURLAsync("**/mail-instellingen?saved=1**", new() { Timeout = 30_000 });
            await page.Locator("[role=status]").First.WaitForAsync(new() { Timeout = 15_000 });
            await Shot(page, tag, "mail-settings-saved", after: true);

            if (wasChecked != await reminder.IsCheckedAsync())
            {
                if (wasChecked)
                {
                    await reminder.CheckAsync(new() { Force = true });
                }
                else
                {
                    await reminder.UncheckAsync(new() { Force = true });
                }

                await page.Locator("button[type=submit]").First.ClickAsync();
            }
        }
    }

    private static async Task VisitSettingsAndPrivacyHubsAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/settings", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.Locator("h1").Filter(new() { HasText = "Instellingen" }).First.WaitForAsync(new() { Timeout = 30_000 });
        await Shot(page, tag, "candidate-settings");

        await page.GotoAsync($"{baseUrl}/candidate/privacy", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.Locator("h1").Filter(new() { HasText = "Privacy" }).First.WaitForAsync(new() { Timeout = 30_000 });
        await Shot(page, tag, "candidate-privacy");
    }

    private static async Task VisitPrivacyDataAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/privacy/data", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.Locator("a[href='/privacy/data/export']").WaitForAsync(new() { Timeout = 30_000 });
        await Shot(page, tag, "privacy-data-export");
    }

    private static async Task VisitApplyFlowAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/vacancies", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);

        var links = page.Locator("a[href*='/vacancies/']");
        var count = await links.CountAsync();
        if (count == 0)
        {
            return;
        }

        for (var i = 0; i < Math.Min(count, 8); i++)
        {
            await links.Nth(i).ClickAsync();
            await page.WaitForTimeoutAsync(1_200);
            await Shot(page, tag, $"vacancy-detail-{i}");

            if (await page.Locator(".apply-panel--success, .kb-detail-applied, button:has-text('Je hebt gesolliciteerd')").CountAsync() > 0)
            {
                await page.EvaluateAsync("() => document.getElementById('apply')?.scrollIntoView({ block: 'center' })");
                await page.WaitForTimeoutAsync(400);
                await Shot(page, tag, "apply-already-sent", after: true);
                return;
            }

            var applyBtn = page.Locator("button:has-text('Solliciteer'), a:has-text('Solliciteer')").First;
            if (await applyBtn.CountAsync() == 0)
            {
                await page.GoBackAsync();
                continue;
            }

            await applyBtn.ClickAsync();
            await page.WaitForTimeoutAsync(800);
            await page.EvaluateAsync("() => document.getElementById('apply')?.scrollIntoView({ block: 'center' })");

            var feedback = page.Locator(".apply-panel--success, .apply-panel--warn, .apply-panel--verify, .apply-flow").First;
            if (await feedback.CountAsync() > 0)
            {
                await Shot(page, tag, "apply-flow-open");
            }

            var send = page.Locator("button:has-text('Verstuur'), button:has-text('Sollicitatie versturen'), button:has-text('Ga door')").First;
            if (await send.CountAsync() > 0 && await send.IsEnabledAsync())
            {
                await send.ClickAsync();
                await page.WaitForTimeoutAsync(1_500);
            }

            if (await page.Locator(".apply-panel--success, .apply-panel--warn, .apply-panel--verify").CountAsync() > 0)
            {
                await Shot(page, tag, "apply-feedback", after: true);
                return;
            }

            await page.GoBackAsync();
        }
    }

    private static async Task VisitIncompletePassportPdfLockedAsync(IPage page, string baseUrl, string tag)
    {
        await using var isolated = await page.Context.Browser!.NewContextAsync(KbE2e.ContextFor(
            tag == "mobile" ? KbE2e.Mobile.Width : KbE2e.Desktop.Width,
            tag == "mobile" ? KbE2e.Mobile.Height : KbE2e.Desktop.Height));
        await PlaywrightCookieConsent.AcceptAsync(isolated);
        var inc = await isolated.NewPageAsync();
        if (!await KbE2e.TryLoginIncompleteAsync(inc, baseUrl))
        {
            return;
        }

        await inc.GotoAsync($"{baseUrl}/candidate/paspoort?tab=proof", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(inc);
        await CandidateJourneyE2e.ShotViewportAsync(inc, Path.Combine(CandidateJourneyE2e.ShotDir(tag), $"before-paspoort-proof-incomplete.png"));
    }

    private static async Task VisitDnaTestSampleAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/culture", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await Shot(page, tag, "dna-test-culture-start");

        if (await page.Locator(".test-flow, fieldset.q-likert").CountAsync() > 0)
        {
            await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 12, 3);
            await Shot(page, tag, "dna-test-culture-progress", after: true);
        }
    }

    private static async Task VisitProactiveMatchingAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await Shot(page, tag, "proactive-matching");
    }

    private static async Task VisitApplicationsEmptyStateAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/applications?filter=gesprek", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await Shot(page, tag, "applications-filter-empty");
    }
}
