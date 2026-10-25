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
            var ensure = await page.APIRequest.PostAsync($"{baseUrl}/api/me/acceptatie-after-match-demo-ensure");
            Assert.True(ensure.Ok, $"after-match demo ensure failed: {ensure.Status}");
        }

        await AssertAfterMatchSkipsBlazorRuntimeAsync(page, baseUrl);
        await AssertProposePanelOpensInPageAsync(page, baseUrl);
        await AssertReschedulePanelsInPageAsync(page, baseUrl);
        await AssertMessagesThreadSwitchInPageAsync(page, baseUrl);
        await AssertOutcomeOfferClickNoNavigationAsync(page, baseUrl);
        await AssertScheduledHomeAsync(page, baseUrl);
        await AssertDemo0006LoadsAsync(page, baseUrl);
        await AssertOutcomeNotYetOnWaitAppAsync(page, baseUrl);
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
        var basePath = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.RescheduleApp:D}/after-match/reschedule-ssr";
        await page.GotoAsync(basePath, new() { WaitUntil = WaitUntilState.NetworkIdle });
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
        var threadLink = page.GetByRole(AriaRole.Link, new() { Name = "Kandidaat #4790" }).First;
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

    private static async Task AssertDemo0006LoadsAsync(IPage page, string baseUrl)
    {
        var url = $"{baseUrl}/werkgever/sollicitaties/{AfterMatchDemoIds.Verkoop4790App:D}/after-match?panel=messages";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-ndm-employer-messages-ssr]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".state-message")).ToHaveCountAsync(0);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "06-messages-4790.png"), FullPage = true });
    }
}
