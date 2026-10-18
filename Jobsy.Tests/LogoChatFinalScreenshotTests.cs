using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LogoChatFinalScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final-r2";
    private static readonly Guid SampleVacancyId = IllustratedPagesApiStub.SampleVacancyId;

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
    public async Task Render_logo_chat_final_r2_on_real_illustrated_vacancy()
    {
        Directory.CreateDirectory(OutDir);
        var www = Path.GetFullPath(Path.Combine(FindRoot(), "Jobsy.Web/wwwroot"));
        var vacancyPath = $"/vacancies/{SampleVacancyId:D}";

        await CaptureHeaderAsync(1440, 900, "header-with-logo-1440");
        await CaptureHeaderAsync(390, 844, "header-with-logo-390");
        await CaptureVacancyEntryAsync(vacancyPath, 1440, 900, "vacancy-chat-entry-1440");
        await CaptureVacancyEntryAsync(vacancyPath, 390, 844, "vacancy-chat-entry-390");
        await CaptureMockInterviewOpenAsync(vacancyPath, 390, 844, "mock-interview-open-390");

        File.Copy(
            Path.Combine(www, "brand/png/lobsy-apple-touch-icon-180.png"),
            Path.Combine(OutDir, "pwa-apple-touch-icon-180.png"),
            overwrite: true);
        File.Copy(
            Path.Combine(www, "brand/png/lobsy-favicon-32.png"),
            Path.Combine(OutDir, "favicon-32.png"),
            overwrite: true);
    }

    private async Task CaptureHeaderAsync(int width, int height, string name)
    {
        await using var context = await NewCandidateContextAsync(width, height);
        var page = await OpenVacancyAsync(context, $"/vacancies/{SampleVacancyId:D}");
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        var header = page.Locator(".app-header");
        await header.ScreenshotAsync(new() { Path = Path.Combine(OutDir, name + ".png") });
        await AssertHeaderLogoLoadedAsync(page);
    }

    private async Task CaptureVacancyEntryAsync(string path, int width, int height, string name)
    {
        await using var context = await NewCandidateContextAsync(width, height);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        var pillSlot = width >= 1024 ? ".lobsy-coach-cta--kb-rail" : ".lobsy-coach-cta--kb-mobile";
        await Microsoft.Playwright.Assertions.Expect(page.Locator($"{pillSlot} .lobsy-coach-cta__label")).ToBeVisibleAsync();
        await AssertVacancyStartsBelowHeaderAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, name + ".png"), FullPage = true });
    }

    private async Task CaptureMockInterviewOpenAsync(string path, int width, int height, string name)
    {
        await using var context = await NewCandidateContextAsync(width, height);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        var pill = page.Locator("[data-testid=vacancy-mock-interview-cta] button").First;
        await pill.ClickAsync();
        await page.Locator(".mock-interview-modal").WaitForAsync(new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
        });
    }

    private async Task<IPage> OpenVacancyAsync(IBrowserContext context, string path)
    {
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        return page;
    }

    private async Task<IBrowserContext> NewCandidateContextAsync(int width, int height)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = width < 500,
            HasTouch = width < 500,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        return context;
    }

    private static async Task AssertHeaderLogoLoadedAsync(IPage page)
    {
        var logo = page.Locator(".app-header .brand__logo, .app-header .lobsy-logo--horizontal").First;
        var width = await logo.EvaluateAsync<int>("el => el.naturalWidth");
        Assert.True(width > 0, "Header logo naturalWidth should be > 0");
    }

    private static async Task AssertVacancyStartsBelowHeaderAsync(IPage page)
    {
        var header = await page.Locator(".app-header").BoundingBoxAsync();
        var detail = await page.Locator("[data-testid=kb-detail]").BoundingBoxAsync();
        Assert.NotNull(header);
        Assert.NotNull(detail);
        var gap = detail!.Y - (header!.Y + header.Height);
        Assert.InRange(gap, -4, 120);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
