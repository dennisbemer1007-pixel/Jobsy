using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Manual walkthrough captures — set WALKTHROUGH_CAPTURE_NAME (e.g. paspoort-before-390).</summary>
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
        else if (name.Contains("paspoort", StringComparison.OrdinalIgnoreCase))
        {
            var isBefore = name.Contains("before", StringComparison.OrdinalIgnoreCase);
            Environment.SetEnvironmentVariable(
                "JOBSY_WALKTHROUGH_SLOW_CORE",
                isBefore ? "1" : null);
            Environment.SetEnvironmentVariable("JOBSY_WALKTHROUGH_SLOW_SECONDARY", null);

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            if (isBefore)
            {
                await page.Locator(".page-skeleton--profile").WaitForAsync(new() { Timeout = 15_000 });
                await page.WaitForTimeoutAsync(200);
            }
            else
            {
                await WaitForPassportContentAsync(page, timeoutMs: 15_000);
                await Assertions.Expect(page.Locator(".page-skeleton")).ToHaveCountAsync(0, new() { Timeout = 2_000 });
                await Assertions.Expect(page.Locator(".panel-inline-error")).ToHaveCountAsync(0, new() { Timeout = 500 });
                await page.WaitForTimeoutAsync(300);
            }
        }
        else
        {
            Environment.SetEnvironmentVariable("JOBSY_WALKTHROUGH_SLOW_CORE", null);
            Environment.SetEnvironmentVariable(
                "JOBSY_WALKTHROUGH_SLOW_SECONDARY",
                name.Contains("before", StringComparison.OrdinalIgnoreCase) ? "1" : null);
            var waitMs = 2_000;
            if (int.TryParse(Environment.GetEnvironmentVariable("WALKTHROUGH_WAIT_MS"), out var parsed) && parsed >= 0)
            {
                waitMs = parsed;
            }

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(waitMs);
        }

        var path = Path.Combine(ArtifactsDir, $"{name}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = false });
        Assert.True(File.Exists(path));

        Environment.SetEnvironmentVariable("JOBSY_WALKTHROUGH_SLOW_CORE", null);
        Environment.SetEnvironmentVariable("JOBSY_WALKTHROUGH_SLOW_SECONDARY", null);
    }

    [Fact]
    public async Task Paspoort_before_and_after_390_captures_differ_when_env_set()
    {
        if (Environment.GetEnvironmentVariable("WALKTHROUGH_CAPTURE_PAIR") != "paspoort-390")
        {
            return;
        }

        Directory.CreateDirectory(ArtifactsDir);
        var beforePath = Path.Combine(ArtifactsDir, "paspoort-before-390.png");
        var afterPath = Path.Combine(ArtifactsDir, "paspoort-after-390.png");

        Environment.SetEnvironmentVariable("WALKTHROUGH_CAPTURE_NAME", "paspoort-before-390");
        await Capture_when_WALKTHROUGH_CAPTURE_NAME_is_set();

        Environment.SetEnvironmentVariable("WALKTHROUGH_CAPTURE_NAME", "paspoort-after-390");
        await Capture_when_WALKTHROUGH_CAPTURE_NAME_is_set();

        Assert.True(File.Exists(beforePath));
        Assert.True(File.Exists(afterPath));
        var beforeBytes = await File.ReadAllBytesAsync(beforePath);
        var afterBytes = await File.ReadAllBytesAsync(afterPath);
        Assert.False(beforeBytes.SequenceEqual(afterBytes), "Before/after paspoort @390 screenshots must differ visibly.");
    }

    private static async Task WaitForPassportContentAsync(IPage page, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var ready = await page.EvaluateAsync<bool>(
                """
                () => {
                  if (!document.querySelector('.passport-page')) return false;
                  if (document.querySelector('.page-skeleton')) return false;
                  const card = document.querySelector('.passport-layout__card--mobile, .passport-layout__sidebar');
                  const overview = document.querySelector('.passport-overview');
                  return !!(card || overview);
                }
                """);
            if (ready)
            {
                return;
            }

            await page.WaitForTimeoutAsync(50);
        }

        throw new TimeoutException("Paspoort content not visible for after capture.");
    }
}
