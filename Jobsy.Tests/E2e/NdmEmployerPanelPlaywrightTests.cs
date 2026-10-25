using Jobsy.Infrastructure.Data;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Headless Chrome checks for werkgever after-match in-page panels (PR #1043 follow-up).
/// Run local: JOBSY_NDM_EMPLOYER_PANELS=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201
/// Run acceptatie: JOBSY_NDM_EMPLOYER_PANELS=1 JOBSY_NDM_EMPLOYER_ACC=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class NdmEmployerPanelPlaywrightTests
{
    private const string EnvRun = "JOBSY_NDM_EMPLOYER_PANELS";
    private const string EnvAcc = "JOBSY_NDM_EMPLOYER_ACC";
    private const string AccBaseUrl = "https://acceptatie.lobsy.nl";
    private const string EmployerEmail = "ondernemer@jobsy.local";
    private const string Password = "Jobsy123!";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static bool RequireAcceptatie =>
        string.Equals(Environment.GetEnvironmentVariable(EnvAcc), "1", StringComparison.Ordinal);

    private static string OutDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_NDM_EMPLOYER_ARTIFACTS"))
            ? "/opt/cursor/artifacts/ndm-employer-panels"
            : Environment.GetEnvironmentVariable("JOBSY_NDM_EMPLOYER_ARTIFACTS")!;

    [Fact]
    public async Task Werkgever_after_match_panels_stay_in_page_with_screenshots()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable");
        if (RequireAcceptatie)
        {
            Assert.StartsWith(AccBaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase);
        }

        Directory.CreateDirectory(OutDir);

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(KbE2e.ContextFor(1440, 900));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(60_000);
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));

        if (RequireAcceptatie)
        {
            var ensure = await page.APIRequest.PostAsync($"{baseUrl}/api/werkgever-client/after-match-demo-ensure");
            var ensureBody = await ensure.TextAsync();
            Assert.True(ensure.Ok, $"after-match demo ensure failed: {ensure.Status} {ensureBody}");
            using var doc = System.Text.Json.JsonDocument.Parse(ensureBody);
            Assert.True(
                doc.RootElement.GetProperty("applied").GetBoolean(),
                $"after-match demo ensure not applied: {ensureBody}");
        }

        await AssertAfterMatchSkipsBlazorRuntimeAsync(page, baseUrl);
        await AssertProposePanelOpensInPageAsync(page, baseUrl);
        await AssertReschedulePanelsInPageAsync(page, baseUrl);
        await AssertMessagesThreadSwitchInPageAsync(page, baseUrl);
        await AssertOutcomeOfferClickNoNavigationAsync(page, baseUrl);
        await AssertScheduledHomeAsync(page, baseUrl);
        await AssertDemo0006LoadsAsync(page, baseUrl);
        await AssertOutcomeNotYetOnWaitAppAsync(page, baseUrl);
        await AssertRescheduleCanonicalUrlNoRedirectLoopAsync(page, baseUrl);
        await AssertPickSlotsWaitingCopyAsync(page, baseUrl);
        await AssertScheduledOutcomeInPageAsync(page, baseUrl);

        await using var mobileCtx = await browser.NewContextAsync(KbE2e.ContextFor(390, 844));
        await PlaywrightCookieConsent.AcceptAsync(mobileCtx);
        var mobilePage = await mobileCtx.NewPageAsync();
        mobilePage.SetDefaultTimeout(60_000);
        await PlaywrightCookieConsent.AcceptOnPageAsync(mobilePage);
        Assert.True(await KbE2e.TryLoginAsync(mobilePage, baseUrl, EmployerEmail, Password));
        await AssertMessagesMobileListReachableAsync(mobilePage, baseUrl);
    }

    private static async Task AssertAfterMatchSkipsBlazorRuntimeAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-enhance-nav", "false");
        await Assertions.Expect(page.Locator("script[src*='blazor.web.js']")).ToHaveCountAsync(0);
    }

    private static async Task AssertProposePanelOpensInPageAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=propose";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        var root = page.Locator("[data-ndm-employer-ssr]");
        await Assertions.Expect(root).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-ndm-wg-main]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-ndm-propose-panel]")).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "03-propose-panel.png"), FullPage = true });

        await page.GotoAsync($"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match", new() { WaitUntil = WaitUntilState.NetworkIdle });
        var loads = 0;
        page.Load += (_, _) => loads++;
        await page.GetByRole(AriaRole.Button, new() { Name = "Momenten voorstellen" }).ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        await Assertions.Expect(page.Locator("[data-ndm-propose-panel]")).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "03-propose-click.png"), FullPage = true });
    }

    private static async Task AssertReschedulePanelsInPageAsync(IPage page, string baseUrl)
    {
        var basePath = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.RescheduleApp:D}/after-match";
        var response = await page.GotoAsync(basePath, new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.NotNull(response);
        Assert.False(response!.Status is >= 300 and < 400, "reschedule demo must not redirect-loop");
        Assert.DoesNotContain("reschedule-ssr", page.Url, StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Akkoord, maandag 26 okt 14:00" })).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "09-reschedule-home.png"), FullPage = true });

        var loads = 0;
        page.Load += (_, _) => loads++;
        await page.GetByRole(AriaRole.Button, new() { Name = "Ander moment voorstellen" }).ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        await Assertions.Expect(page.Locator("[data-ndm-propose-panel]")).ToBeVisibleAsync();
        await page.Locator("[data-ndm-propose-mode=\"agenda\"]").ClickAsync();
        await Assertions.Expect(page.Locator("[data-ndm-agenda-block]")).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "09-reschedule-propose.png"), FullPage = true });

        loads = 0;
        await page.GetByRole(AriaRole.Button, new() { Name = "Gesprek afzeggen" }).ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        await Assertions.Expect(page.Locator("[data-ndm-cancel-panel]")).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "09-reschedule-cancel.png"), FullPage = true });
    }

    private static async Task AssertMessagesThreadSwitchInPageAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=messages";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        var threadLink = page.GetByRole(AriaRole.Button, new() { Name = "Kandidaat #4790" }).First;
        await Assertions.Expect(threadLink).ToBeVisibleAsync();

        var loads = 0;
        page.Load += (_, _) => loads++;
        await threadLink.ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        Assert.Contains("thread=", page.Url, StringComparison.OrdinalIgnoreCase);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "11-messages-thread.png"), FullPage = true });

        await page.GotoAsync($"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match", new() { WaitUntil = WaitUntilState.NetworkIdle });
        loads = 0;
        await page.GetByRole(AriaRole.Button, new() { Name = "Bericht sturen" }).ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        await Assertions.Expect(page.Locator("[data-ndm-employer-messages-ssr]")).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "11-messages-from-home.png"), FullPage = true });
    }

    private static async Task AssertOutcomeOfferClickNoNavigationAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.ScheduledApp:D}/after-match?panel=outcome";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        var loads = 0;
        page.Load += (_, _) => loads++;
        await page.GetByRole(AriaRole.Button, new() { Name = "Aanbod" }).ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "12-outcome-offer-click.png"), FullPage = true });
    }

    private static async Task AssertScheduledHomeAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.ScheduledApp:D}/after-match";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Je gesprek staat erin" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".what")).Not.ToContainTextAsync("Nu: momenten voorstellen");
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "03-scheduled-home.png"), FullPage = true });

        await page.GotoAsync($"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.PickSlotsApp:D}/after-match", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "02-pick-slots-home.png"), FullPage = true });

        await page.GotoAsync($"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.NotNowApp:D}/after-match", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "05-notnow-home.png"), FullPage = true });
    }

    private static async Task AssertOutcomeNotYetOnWaitAppAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=outcome";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-ndm-wg-main]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".state-message")).ToHaveCountAsync(0);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "01-outcome-not-yet.png"), FullPage = true });
    }

    private static async Task AssertRescheduleCanonicalUrlNoRedirectLoopAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.RescheduleApp:D}/after-match?x=1";
        var response = await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.NotNull(response);
        Assert.True(response!.Ok, $"expected 200, got {response.Status}");
        await Assertions.Expect(page.Locator("[data-ndm-employer-ssr]")).ToBeVisibleAsync();
    }

    private static async Task AssertPickSlotsWaitingCopyAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.PickSlotsApp:D}/after-match";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".what")).ToContainTextAsync("Momenten verstuurd, wacht op Sanne");
        await Assertions.Expect(page.Locator(".what")).Not.ToContainTextAsync("Nu: momenten voorstellen");
    }

    private static async Task AssertScheduledOutcomeInPageAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.ScheduledApp:D}/after-match";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        var loads = 0;
        page.Load += (_, _) => loads++;
        await page.GetByRole(AriaRole.Button, new() { Name = "Uitkomst invullen" }).ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, loads);
        await Assertions.Expect(page.Locator("[data-ndm-outcome-panel]:not([hidden])")).ToBeVisibleAsync();
        Assert.Contains("panel=outcome", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertMessagesMobileListReachableAsync(IPage page, string baseUrl)
    {
        var ensure = await page.APIRequest.PostAsync($"{baseUrl}/api/werkgever-client/after-match-demo-ensure");
        Assert.True(ensure.Ok, $"mobile demo ensure failed: {ensure.Status}");
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=messages";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-ndm-employer-messages-ssr]');
              if (!root) return false;
              const params = new URL(location.href).searchParams;
              if (params.get('thread')) return true;
              const list = root.querySelector('.tlist');
              return list && getComputedStyle(list).display !== 'none';
            }
            """,
            null,
            new() { Timeout = 20_000 });
        var list = page.Locator("[data-ndm-employer-messages-ssr] .tlist [data-ndm-thread-select]");
        await Assertions.Expect(list.First).ToBeVisibleAsync();
        Assert.True(
            await list.CountAsync() >= 2,
            "expected at least 2 message threads in mobile list");
        await Assertions.Expect(list.Filter(new() { HasText = "#4790" })).ToBeVisibleAsync();
        await Assertions.Expect(list.Filter(new() { HasText = "Sanne" })).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "11-messages-mobile-list.png"), FullPage = true });
    }

    private static async Task AssertDemo0006LoadsAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.Verkoop4790App:D}/after-match?panel=messages";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-ndm-employer-messages-ssr]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".state-message")).ToHaveCountAsync(0);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "06-messages-4790.png"), FullPage = true });
    }
}
