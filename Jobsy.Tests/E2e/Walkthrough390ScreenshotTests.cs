using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Manual walkthrough captures — set WALKTHROUGH_CAPTURE_NAME (e.g. cookie-before-390).</summary>
[Collection("PlaywrightSmoke")]
public sealed class Walkthrough390ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots";

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
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
    public async Task Capture_when_WALKTHROUGH_CAPTURE_NAME_is_set()
    {
        var name = Environment.GetEnvironmentVariable("WALKTHROUGH_CAPTURE_NAME");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Directory.CreateDirectory(ArtifactsDir);
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var kind = name.Split('-', 2)[0];
        if (!kind.Equals("cookie", StringComparison.OrdinalIgnoreCase))
        {
            await PlaywrightCookieConsent.AcceptAsync(context);
        }

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        if (kind.Equals("cookie", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load });
            await page.Locator(".cookie-consent").WaitForAsync(new() { Timeout = 15_000 });
        }
        else
        {
            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(2_000);
        }

        var path = Path.Combine(ArtifactsDir, $"{name}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
        Assert.True(File.Exists(path));
    }
}
