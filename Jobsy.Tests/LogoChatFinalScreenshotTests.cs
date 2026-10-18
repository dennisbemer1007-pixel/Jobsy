using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LogoChatFinalScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final-r3";
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
    public async Task Render_logo_chat_final_r3_on_real_illustrated_vacancy()
    {
        Directory.CreateDirectory(OutDir);
        var www = Path.GetFullPath(Path.Combine(FindRoot(), "Jobsy.Web/wwwroot"));
        var vacancyPath = $"/vacancies/{SampleVacancyId:D}";

        await CaptureHeaderAsync(1440, 900, "header-with-logo-1440");
        await CaptureHeaderAsync(390, 844, "header-with-logo-390");
        await CaptureVacancyDesktopAsync(vacancyPath, "vacancy-chat-entry-1440");
        await CaptureVacancyMobileViewportAsync(vacancyPath, "vacancy-chat-entry-390");
        await CaptureMockInterviewOpenAsync(vacancyPath, "mock-interview-open-390");

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

    private async Task CaptureVacancyDesktopAsync(string path, string name)
    {
        await using var context = await NewCandidateContextAsync(1440, 900);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".kb-detail__section--copy .vacancy-description-html").WaitForAsync(new() { Timeout = 30_000 });
        await Microsoft.Playwright.Assertions.Expect(page.Locator(".lobsy-coach-cta--kb-rail .lobsy-coach-cta__label")).ToBeVisibleAsync();
        await AssertVacancyStartsBelowHeaderAsync(page);
        await AssertNoEmptyDescriptionShellAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, name + ".png"), FullPage = true });
    }

    private async Task CaptureVacancyMobileViewportAsync(string path, string baseName)
    {
        await using var context = await NewCandidateContextAsync(390, 844);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".kb-detail__section--copy .vacancy-description-html").WaitForAsync(new() { Timeout = 30_000 });
        await Microsoft.Playwright.Assertions.Expect(page.Locator(".lobsy-coach-cta--kb-mobile .lobsy-coach-cta__label")).ToBeVisibleAsync();
        await AssertVacancyStartsBelowHeaderAsync(page);
        await AssertNoEmptyDescriptionShellAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, baseName + "-viewport-top.png"),
            FullPage = false,
        });
        await page.EvaluateAsync("window.scrollTo(0, Math.max(document.body.scrollHeight * 0.42, 480))");
        await page.WaitForTimeoutAsync(350);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, baseName + "-viewport-scrolled.png"),
            FullPage = false,
        });
    }

    private async Task CaptureMockInterviewOpenAsync(string path, string name)
    {
        await using var context = await NewCandidateContextAsync(390, 844);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        var pill = page.Locator("[data-testid=vacancy-mock-interview-cta] button").First;
        await pill.ClickAsync();
        var modal = page.Locator(".mock-interview-modal");
        await modal.WaitForAsync(new() { Timeout = 30_000 });
        await Microsoft.Playwright.Assertions.Expect(page.Locator(".share-modal-backdrop")).ToBeVisibleAsync();
        var box = await modal.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.Height >= 280, "Mock interview sheet should fill most of the mobile viewport.");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = false,
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
        var logo = page.Locator(".app-header .lobsy-logo__img").First;
        await logo.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
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

    private static async Task AssertNoEmptyDescriptionShellAsync(IPage page)
    {
        var emptyShells = await page.EvaluateAsync<int>(
            """
            () => {
              let n = 0;
              for (const el of document.querySelectorAll('.kb-detail__section--copy')) {
                const text = (el.innerText || '').replace(/\s+/g, ' ').trim();
                if (text.length === 0) n++;
              }
              return n;
            }
            """);
        Assert.Equal(0, emptyShells);
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
