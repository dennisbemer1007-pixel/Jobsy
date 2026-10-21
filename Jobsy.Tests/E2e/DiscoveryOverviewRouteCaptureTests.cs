using System.Text;
using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class DiscoveryOverviewRouteCaptureTests
{
    private const string ArtifactsDir = "/opt/cursor/artifacts";

    [Theory]
    [InlineData(1440, 900, false, "1440")]
    [InlineData(390, 844, true, "390")]
    public async Task Capture_real_overview_route(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_DISCOVERY_OVERVIEW_ROUTE") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        Directory.CreateDirectory(ArtifactsDir);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await DiscoveryOverviewRouteE2eFlow.ReachDiscoveryOverviewAsync(page, baseUrl, mobile);

        var routeStrokeWidth = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.querySelector('#discovery-route-path')).strokeWidth");
        Assert.NotEqual("0px", routeStrokeWidth);

        var panelBg = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.querySelector('.journey-route__panel')).backgroundColor");
        Assert.NotEqual("rgba(0, 0, 0, 0)", panelBg);

        var fontFamily = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.body).fontFamily");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, $"ontdekkingsreis-route-product-{tag}.png"),
            FullPage = true,
        });

        Assert.True(
            fontFamily.Contains("Rubik", StringComparison.OrdinalIgnoreCase)
            || fontFamily.Contains("Fraunces", StringComparison.OrdinalIgnoreCase)
            || fontFamily.Contains("Nunito", StringComparison.OrdinalIgnoreCase),
            $"Expected Lobsy UI font stack, got: {fontFamily}");
        Assert.False(
            string.Equals(fontFamily.Trim(), "Times New Roman", StringComparison.OrdinalIgnoreCase),
            "Page must load product CSS, not browser-default Times only.");
    }

    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Click_audit_overview_route(int width, int height, bool mobile)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_DISCOVERY_OVERVIEW_ROUTE_CLICK_AUDIT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        Directory.CreateDirectory(ArtifactsDir);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await DiscoveryOverviewRouteE2eFlow.ReachDiscoveryOverviewAsync(page, baseUrl, mobile);

        var audit = new List<object>();

        async Task RecordAsync(string action, Func<Task> act)
        {
            await act();
            await page.WaitForTimeoutAsync(400);
            audit.Add(new { action, url = page.Url });
        }

        await RecordAsync("toggle_stop_kust", async () =>
        {
            await page.Locator(".journey-route__stop[data-phase=kust] .journey-route__stop-btn").ClickAsync();
        });

        await RecordAsync("toggle_stop_kust_close", async () =>
        {
            await page.Locator(".journey-route__stop[data-phase=kust] .journey-route__stop-btn").ClickAsync();
        });

        await RecordAsync("open_stop_rots", async () =>
        {
            await page.Locator(".journey-route__stop[data-phase=rots] .journey-route__stop-btn").ClickAsync();
        });

        var adjust = page.Locator(".journey-route__panel .journey-route__adjust").First;
        await RecordAsync("aanpassen_first_section", async () => await adjust.ClickAsync());

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(500);

        await RecordAsync("open_stop_light", async () =>
        {
            await page.Locator(".journey-route__stop[data-phase=light] .journey-route__stop-btn").ClickAsync();
        });

        var continueBtn = page.Locator(".journey-route__cta").First;
        if (await continueBtn.IsVisibleAsync())
        {
            await RecordAsync("ga_verder", async () => await continueBtn.ClickAsync());
            await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            audit.Add(new { action = "ga_verder", url = "(no visible Ga verder CTA on current seed)" });
        }

        var startValues = page.Locator(".journey-route__test.is-todo").Last.Locator(".journey-route__test-start");
        await RecordAsync("start_waarden_test", async () => await startValues.ClickAsync());

        var suffix = mobile ? "390" : "1440";
        var reportPath = Path.Combine(ArtifactsDir, $"ontdekkingsreis-route-click-audit-{suffix}.json");
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(audit, new JsonSerializerOptions { WriteIndented = true }));

        var sb = new StringBuilder();
        foreach (var entry in audit)
        {
            var node = (JsonElement)JsonSerializer.SerializeToElement(entry);
            sb.AppendLine($"{node.GetProperty("action")}: {node.GetProperty("url")}");
        }

        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, $"ontdekkingsreis-route-click-audit-{suffix}.txt"), sb.ToString());
    }
}
