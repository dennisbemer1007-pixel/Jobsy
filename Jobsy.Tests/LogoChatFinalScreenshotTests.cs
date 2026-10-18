using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LogoChatFinalScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final-r5";
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
    public async Task Render_logo_chat_final_r5_viewport_captures()
    {
        Directory.CreateDirectory(OutDir);
        var vacancyPath = $"/vacancies/{SampleVacancyId:D}";

        await CaptureVacancyDesktopAsync(vacancyPath, "vacancy-chat-entry-1440-fullpage", fullPage: true);
        await CaptureVacancyDesktopAsync(vacancyPath, "vacancy-chat-entry-1440-viewport", fullPage: false);
        await CaptureMockInterviewOpenAsync(vacancyPath, "mock-interview-open-390", width: 390, height: 844, mobile: true);
        await CaptureMockInterviewOpenAsync(vacancyPath, "mock-interview-open-1440", width: 1440, height: 900, mobile: false);
        await CaptureApplySentPanelAsync(vacancyPath, "vacancy-apply-sent-1440");
    }

    private async Task CaptureVacancyDesktopAsync(string path, string name, bool fullPage)
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
            FullPage = fullPage,
        });
    }

    private async Task CaptureMockInterviewOpenAsync(string path, string name, int width, int height, bool mobile)
    {
        await using var context = await NewCandidateContextAsync(width, height, mobile);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        var pill = mobile
            ? page.Locator(".lobsy-coach-cta--kb-mobile button")
            : page.Locator(".lobsy-coach-cta--kb-rail button");
        await pill.ClickAsync();
        var modal = page.Locator(".mock-interview-modal");
        await modal.WaitForAsync(new() { Timeout = 30_000 });
        await Microsoft.Playwright.Assertions.Expect(page.Locator(".share-modal-backdrop")).ToBeVisibleAsync();
        await Microsoft.Playwright.Assertions.Expect(page.Locator(".mock-interview-compose__send")).ToBeVisibleAsync();
        var question = page.Locator(".mock-interview-block--question p, .mock-interview-bubble--assistant p").First;
        await question.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var questionText = (await question.InnerTextAsync()).Trim();
        Assert.True(questionText.Length > 20, "Opening coach question should contain text.");
        Assert.DoesNotContain('\u2014', questionText);
        Assert.DoesNotContain('\u2013', questionText);
        if (mobile)
        {
            var box = await modal.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.Height >= 280, "Mock interview sheet should fill most of the mobile viewport.");
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
        });
    }

    private async Task CaptureApplySentPanelAsync(string path, string name)
    {
        await using var context = await NewCandidateContextAsync(1440, 900);
        var page = await OpenVacancyAsync(context, path + "#apply");
        await page.Locator(".apply-panel--success").WaitForAsync(new() { Timeout = 60_000 });
        var mark = page.Locator(".apply-panel--success .profile-encourage__mascot");
        await mark.WaitForAsync(new() { Timeout = 30_000 });
        var src = await mark.GetAttributeAsync("src") ?? "";
        Assert.Contains("lobsy-merkteken", src, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mascot", src, StringComparison.OrdinalIgnoreCase);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = false,
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

    private async Task<IBrowserContext> NewCandidateContextAsync(int width, int height, bool mobile = false)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = mobile,
            HasTouch = mobile,
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
