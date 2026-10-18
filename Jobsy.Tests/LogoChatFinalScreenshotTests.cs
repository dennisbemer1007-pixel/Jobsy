using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LogoChatFinalScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final-r4";
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
    public async Task Render_logo_chat_final_r4_viewport_captures()
    {
        Directory.CreateDirectory(OutDir);
        var vacancyPath = $"/vacancies/{SampleVacancyId:D}";

        await CaptureVacancyDesktopViewportAsync(vacancyPath, "vacancy-chat-entry-1440");
        await CaptureMockInterviewOpenAsync(vacancyPath, "mock-interview-open-390");
    }

    private async Task CaptureVacancyDesktopViewportAsync(string path, string name)
    {
        await using var context = await NewCandidateContextAsync(1440, 900);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".kb-detail__section--copy .vacancy-description-html").WaitForAsync(new() { Timeout = 30_000 });
        await Microsoft.Playwright.Assertions.Expect(page.Locator(".lobsy-coach-cta--kb-rail .lobsy-coach-cta__label")).ToBeVisibleAsync();
        await AssertNoEmptyDescriptionShellAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
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
        var question = page.Locator(".mock-interview-block--question p, .mock-interview-bubble--assistant p").First;
        await question.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var questionText = (await question.InnerTextAsync()).Trim();
        Assert.True(questionText.Length > 20, "Opening coach question should contain text.");
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

}
