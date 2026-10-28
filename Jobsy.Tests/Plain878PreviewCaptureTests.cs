using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>JOBSY_PLAIN_MOCKUP_CAPTURE=1 dotnet test --filter Plain_878_preview_capture</summary>
public sealed class Plain878PreviewCaptureTests : IAsyncLifetime, IAsyncDisposable
{
    private IllustratedPagesWebFactory? _web;

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_PLAIN_MOCKUP_CAPTURE") != "1")
        {
            return;
        }

        _web = new IllustratedPagesWebFactory(plainSkin: true);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Fact]
    public async Task Plain_878_preview_capture()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_PLAIN_MOCKUP_CAPTURE") != "1")
        {
            return;
        }

        Assert.NotNull(_web);
        var baseUrl = _web!.ServerAddress;
        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_PLAIN_ARTIFACT_DIR")
            ?? "/opt/cursor/artifacts";
        Directory.CreateDirectory(artifactDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, mobile, tag) in new[]
                 {
                     (1440, 900, false, "1440"),
                     (390, 844, true, "390"),
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
            });
            var page = await context.NewPageAsync();
            var ret = Uri.EscapeDataString("/candidate/paspoort");
            await page.GotoAsync($"{baseUrl}/__test/sign-in?return={ret}", new() { Timeout = 60_000 });
            await page.WaitForURLAsync("**/candidate/paspoort**", new() { Timeout = 60_000 });
            await page.WaitForSelectorAsync(".cp-plain878 main.wrap", new() { Timeout = 30_000 });
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"preview-paspoort-{tag}.png"),
                FullPage = true,
            });

            var bk = baseUrl
                + "/banenkaart?maxMinutes=60&transport=Auto&originLat=52.3725&originLng=4.8921&originLabel=Amsterdam";
            await page.GotoAsync(bk, new() { Timeout = 60_000 });
            await page.WaitForSelectorAsync("[data-plain-banenkaart=\"1\"]", new() { Timeout = 30_000 });
            await page.WaitForTimeoutAsync(1500);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"preview-banenkaart-{tag}.png"),
                FullPage = false,
            });
        }
    }
}
