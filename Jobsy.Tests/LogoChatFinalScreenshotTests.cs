using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LogoChatFinalScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final-r7";
    private static readonly Guid SampleVacancyId = IllustratedPagesApiStub.SampleVacancyId;

    private const string LongOpeningReply =
        "Vraag: Vertel kort waarom je solliciteert als medewerker dierenpension bij De Kooger Dierenopvang. " +
        "Wat spreekt je aan in de vacature, welke taken uit de tekst zie je voor je, " +
        "en hoe past dat bij wat jij al kunt en leuk vindt om te doen?";

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
        IllustratedPagesApiStub.MockInterviewReplyOverride = null;
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Render_logo_chat_final_r7_viewport_captures()
    {
        Directory.CreateDirectory(OutDir);
        var vacancyPath = $"/vacancies/{SampleVacancyId:D}?encouragePreview=wist-cv-vs-passport";

        await CaptureApplySentPanelAsync(vacancyPath, "vacancy-apply-sent-1440", width: 1440, height: 900);
        await CaptureApplySentPanelAsync(vacancyPath, "vacancy-apply-sent-390", width: 390, height: 844, mobile: true);

        IllustratedPagesApiStub.MockInterviewReplyOverride = LongOpeningReply;
        try
        {
            await CaptureMockInterviewLongQuestionAsync(vacancyPath, "mock-interview-open-390-long-question", 390, 844);
            await CaptureMockInterviewLongQuestionAsync(vacancyPath, "mock-interview-open-360-long-question", 360, 740);
        }
        finally
        {
            IllustratedPagesApiStub.MockInterviewReplyOverride = null;
        }
    }

    private async Task CaptureMockInterviewLongQuestionAsync(string path, string name, int width, int height)
    {
        await using var context = await NewCandidateContextAsync(width, height, mobile: true);
        var page = await OpenVacancyAsync(context, path);
        await page.Locator("[data-testid=kb-detail]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".lobsy-coach-cta--kb-mobile button").ClickAsync();
        var modal = page.Locator(".mock-interview-modal");
        await modal.WaitForAsync(new() { Timeout = 30_000 });
        var question = page.Locator(".mock-interview-block--question p").First;
        await question.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await AssertMockInterviewQuestionNotClippedAsync(page);
        await AssertMockInterviewTipDoesNotOverlapCoachAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
        });
    }

    private async Task CaptureApplySentPanelAsync(string path, string name, int width, int height, bool mobile = false)
    {
        await using var context = await NewCandidateContextAsync(width, height, mobile);
        var page = await OpenVacancyAsync(context, path + "#apply");
        await page.Locator(".apply-panel--success").WaitForAsync(new() { Timeout = 60_000 });
        var encourage = page.Locator(".apply-panel--success .profile-encourage");
        var copy = page.Locator(".apply-panel--success .profile-encourage__copy");
        await copy.WaitForAsync(new() { Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('.apply-panel--success .profile-encourage');
              return root && getComputedStyle(root).display === 'grid';
            }
            """,
            null,
            new() { Timeout = 30_000 });
        var metrics = await page.EvaluateAsync<double[]>(
            """
            () => {
              const root = document.querySelector('.apply-panel--success .profile-encourage');
              const bubble = document.querySelector('.apply-panel--success .profile-encourage__copy');
              const mascot = document.querySelector('.apply-panel--success .profile-encourage__mascot');
              if (!root || !bubble || !mascot) return [0, 0, 0];
              const r = root.getBoundingClientRect();
              const b = bubble.getBoundingClientRect();
              const m = mascot.getBoundingClientRect();
              const rs = getComputedStyle(root);
              const gap = parseFloat(rs.columnGap) || 12;
              const pl = parseFloat(rs.paddingLeft) || 0;
              const pr = parseFloat(rs.paddingRight) || 0;
              const available = r.width - pl - pr - m.width - gap;
              return [r.width, b.width, available];
            }
            """);
        Assert.True(metrics[0] > 0 && metrics[1] > 0 && metrics[2] > 0);
        Assert.True(metrics[1] >= metrics[2] * 0.92,
            $"Encourage bubble should fill the grid track beside the mascot (root={metrics[0]:F1} bubble={metrics[1]:F1} track={metrics[2]:F1}).");
        var mark = page.Locator(".apply-panel--success .profile-encourage__mascot");
        var src = await mark.GetAttributeAsync("src") ?? "";
        Assert.Contains("lobsy-merkteken", src, StringComparison.OrdinalIgnoreCase);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = false,
        });
    }

    private static async Task AssertMockInterviewQuestionNotClippedAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const question = document.querySelector('.mock-interview-block--question p');
              question?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            }
            """);
        var notClipped = await page.EvaluateAsync<bool>(
            """
            () => {
              const question = document.querySelector('.mock-interview-block--question p');
              const compose = document.querySelector('.mock-interview-compose');
              if (!question || !compose) return false;
              const q = question.getBoundingClientRect();
              const c = compose.getBoundingClientRect();
              return q.height > 0 && q.bottom <= c.top + 2;
            }
            """);
        Assert.True(notClipped, "Opening question must sit fully above the answer compose area.");
    }

    private static async Task AssertMockInterviewTipDoesNotOverlapCoachAsync(IPage page)
    {
        var tipBelowCoach = await page.EvaluateAsync<bool>(
            """
            () => {
              const chip = document.querySelector('.mock-interview-chat .mock-interview__chip');
              const bubble = document.querySelector('.mock-interview-bubble--assistant');
              const readAloud = bubble?.querySelector('.read-aloud, [data-read-aloud]');
              if (!chip || !bubble) return false;
              const chipRect = chip.getBoundingClientRect();
              const anchor = readAloud ?? bubble;
              const anchorRect = anchor.getBoundingClientRect();
              return chipRect.top >= anchorRect.bottom - 1;
            }
            """);
        Assert.True(tipBelowCoach, "Tip chip must sit below the coach bubble (no overlap with Lees voor).");
    }

    private async Task<IPage> OpenVacancyAsync(IBrowserContext context, string path)
    {
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await KbE2e.WaitForStyledAppAsync(page);
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
        await context.AddInitScriptAsync(
            """
            for (const k of Object.keys(localStorage)) {
              if (k.startsWith('lobsy-encourage:')) localStorage.removeItem(k);
            }
            """);
        return context;
    }
}
