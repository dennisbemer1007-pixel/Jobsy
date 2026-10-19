using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LogoChatFinalScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final-r9";
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
    public async Task Render_logo_chat_final_r9_viewport_captures()
    {
        Directory.CreateDirectory(OutDir);
        var vacancyPath = $"/vacancies/{SampleVacancyId:D}?encouragePreview=wist-cv-vs-passport";

        await CaptureApplySentPanelAsync(vacancyPath, "vacancy-apply-sent-1440", width: 1440, height: 900, maxLines: 3);
        await CaptureApplySentPanelAsync(vacancyPath, "vacancy-apply-sent-390", width: 390, height: 844, mobile: true, maxLines: 4);

        IllustratedPagesApiStub.MockInterviewReplyOverride = LongOpeningReply;
        try
        {
            await CaptureMockInterviewLongQuestionAsync(vacancyPath, "mock-interview-open-390-long-question", 390, 844);
            await CaptureMockInterviewLongQuestionAsync(vacancyPath, "mock-interview-open-360-long-question", 360, 740, assertReadAloudVisible: true);
        }
        finally
        {
            IllustratedPagesApiStub.MockInterviewReplyOverride = null;
        }
    }

    private async Task CaptureMockInterviewLongQuestionAsync(
        string path,
        string name,
        int width,
        int height,
        bool assertReadAloudVisible = false)
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
        if (assertReadAloudVisible)
        {
            await AssertMockInterviewReadAloudVisibleAsync(page);
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
        });
    }

    private async Task CaptureApplySentPanelAsync(
        string path,
        string name,
        int width,
        int height,
        bool mobile = false,
        int maxLines = 4)
    {
        await using var context = await NewCandidateContextAsync(width, height, mobile);
        var page = await OpenVacancyAsync(context, path + "#apply");
        await page.Locator(".apply-panel--success").WaitForAsync(new() { Timeout = 60_000 });
        var copy = page.Locator(".apply-panel--success .profile-encourage__copy");
        await copy.WaitForAsync(new() { Timeout = 30_000 });
        var layout = await page.EvaluateAsync<double[]>(
            """
            () => {
              const panel = document.querySelector('.apply-panel--success');
              const root = document.querySelector('.apply-panel--success .profile-encourage');
              const bubble = document.querySelector('.apply-panel--success .profile-encourage__copy');
              if (!panel || !root || !bubble) return [0, 0, 0, 0, 0];
              const p = panel.getBoundingClientRect();
              const r = root.getBoundingClientRect();
              const b = bubble.getBoundingClientRect();
              const cardRatio = r.width > 0 ? b.width / r.width : 0;
              const panelRatio = p.width > 0 ? r.width / p.width : 0;
              return [p.width, r.width, b.width, cardRatio, panelRatio];
            }
            """);
        Assert.True(layout[0] > 0 && layout[1] > 0 && layout[2] > 0);
        Assert.True(layout[3] >= 0.75,
            $"White speech bubble (.profile-encourage__copy) must fill the grey card (panel={layout[0]:F1} card={layout[1]:F1} bubble={layout[2]:F1} cardRatio={layout[3]:P0}).");

        var lineCount = await page.EvaluateAsync<int>(
            """
            () => {
              const text = document.querySelector('.apply-panel--success .profile-encourage__text');
              if (!text) return 99;
              const rects = [...text.getClientRects()].filter(r => r.width > 0 && r.height > 0);
              if (rects.length > 0) return rects.length;
              const style = getComputedStyle(text);
              const lh = parseFloat(style.lineHeight);
              if (!Number.isFinite(lh) || lh <= 0) return 99;
              return Math.max(1, Math.round(text.getBoundingClientRect().height / lh));
            }
            """);
        Assert.True(lineCount <= maxLines,
            $"Nudge copy should wrap to at most {maxLines} lines at this viewport (actual {lineCount}).");

        var noOverlap = await page.EvaluateAsync<bool>(
            """
            () => {
              const root = document.querySelector('.apply-panel--success .profile-encourage');
              const close = document.querySelector('.apply-panel--success .profile-encourage__close');
              const text = document.querySelector('.apply-panel--success .profile-encourage__text');
              if (!root || !close || !text) return false;
              const r = root.getBoundingClientRect();
              const c = close.getBoundingClientRect();
              const t = text.getBoundingClientRect();
              const closeInCardCorner = Math.abs(r.right - c.right) <= 12 && c.top - r.top <= 14;
              const overlap = c.left < t.right - 2 && c.right > t.left + 2 && c.bottom > t.top + 2 && c.top < t.bottom - 2;
              return closeInCardCorner && !overlap;
            }
            """);
        Assert.True(noOverlap, "Close control must sit in the card corner without overlapping nudge text.");

        var mark = page.Locator(".apply-panel--success .profile-encourage__mascot");
        var src = await mark.GetAttributeAsync("src") ?? "";
        Assert.Contains("lobsy-merkteken", src, StringComparison.OrdinalIgnoreCase);
        await page.Locator(".apply-panel--success .profile-encourage").ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, name + ".png"),
        });
    }

    private static async Task AssertMockInterviewReadAloudVisibleAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const scroll = document.querySelector('.mock-interview-modal__scroll');
              if (scroll) scroll.scrollTop = 0;
              const btn = document.querySelector('.mock-interview-bubble--assistant .read-aloud, .mock-interview-bubble--assistant [data-read-aloud]');
              btn?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            }
            """);
        var readAloud = page.Locator(".mock-interview-bubble--assistant .read-aloud, .mock-interview-bubble--assistant [data-read-aloud]").First;
        await readAloud.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        var inViewport = await page.EvaluateAsync<bool>(
            """
            () => {
              const btn = document.querySelector('.mock-interview-bubble--assistant .read-aloud, .mock-interview-bubble--assistant [data-read-aloud]');
              const compose = document.querySelector('.mock-interview-compose');
              if (!btn || !compose) return false;
              const b = btn.getBoundingClientRect();
              const c = compose.getBoundingClientRect();
              return b.height > 0 && b.bottom <= c.top + 2;
            }
            """);
        Assert.True(inViewport, "Lees voor control must be visible above the compose area on narrow viewports.");
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
