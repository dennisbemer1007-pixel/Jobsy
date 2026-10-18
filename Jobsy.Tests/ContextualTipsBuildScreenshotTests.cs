using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class ContextualTipsBuildScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/tips-build-r3";
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory(contextualTipsEnabled: true);
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Capture_six_tip_screens_and_flag_proof()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var appId = IllustratedPagesApiStub.SampleApplicationId;
        var offerId = IllustratedPagesApiStub.SampleOfferApplicationId;

        await CaptureAsync("01-gesprek-gepland", $"/candidate/applications/{appId:D}/gesprek", "[data-testid='application-interview-page']");
        await CaptureAsync("02-aanbod-ontvangen", $"/candidate/applications/{offerId:D}/aanbod", "[data-testid='application-offer-page']");
        await CaptureAsync("03-paspoort-cv-tip", "/candidate/paspoort?tab=proof", "[data-testid='passport-education-tip']");
        await CaptureAsync("04-carriere-ontwikkeltip", "/carriere", "[data-testid='career-dev-tip']");
        await CaptureAsync("05-alle-tips", "/candidate/tips", "[data-testid='contextual-tips-index']");
        await CaptureAsync("06-tip-detail", "/candidate/tips/je-eerste-gesprek", "[data-testid='contextual-tip-detail']");

        await CaptureAdminFunctiesAsync(_web!, "admin-functies-flag-aan");
        await using (var offWeb = new IllustratedPagesWebFactory(contextualTipsEnabled: false))
        {
            _ = offWeb.ServerAddress;
            await CaptureAdminFunctiesAsync(offWeb, "admin-functies-flag-uit");
        }

        await using var offWebCandidate = new IllustratedPagesWebFactory(contextualTipsEnabled: false);
        _ = offWebCandidate.ServerAddress;
        await using var ctx = await _browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        await page.GotoAsync($"{offWebCandidate.ServerAddress}/__test/sign-in?return=%2Fcandidate%2Ftips");
        await page.GotoAsync($"{offWebCandidate.ServerAddress}/candidate/tips", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(800);
        var offFile = Path.Combine(ArtifactsDir, "flag-uit-1440.png");
        await page.ScreenshotAsync(new() { Path = offFile, FullPage = true });
        Assert.DoesNotContain("/candidate/tips", page.Url, StringComparison.OrdinalIgnoreCase);

        await using var onCtx = await _browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(onCtx);
        var onPage = await onCtx.NewPageAsync();
        await onPage.GotoAsync($"{_web!.ServerAddress}/__test/sign-in?return=%2Fcandidate%2Ftips");
        await onPage.GotoAsync($"{_web.ServerAddress}/candidate/tips", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await onPage.WaitForSelectorAsync("[data-testid='contextual-tips-index']", new() { Timeout = 60_000 });
        var onFile = Path.Combine(ArtifactsDir, "flag-aan-1440.png");
        await onPage.ScreenshotAsync(new() { Path = onFile, FullPage = true });
    }

    private async Task CaptureAdminFunctiesAsync(IllustratedPagesWebFactory web, string namePrefix)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{web.ServerAddress}/__test/sign-in?role=admin&return=%2Fadmin%2Finstellingen",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{web.ServerAddress}/admin/instellingen", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        var file = Path.Combine(ArtifactsDir, $"{namePrefix}-1440.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = true });
        Assert.True(File.Exists(file));
    }

    private async Task CaptureAsync(string name, string path, string readySelector)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await page.GotoAsync(
                $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(readySelector, new() { Timeout = 60_000 });
            await page.WaitForTimeoutAsync(600);
            var file = Path.Combine(ArtifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
            Assert.True(File.Exists(file));
        }
    }
}
