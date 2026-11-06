using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidatePaspoortDangerModalLayoutPlaywrightTests : IAsyncLifetime
{
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

    [Theory]
    [InlineData(390, 844, "reset", ".profile-reset-modal")]
    [InlineData(390, 844, "delete", ".unsubscribe-modal")]
    [InlineData(1280, 900, "reset", ".profile-reset-modal")]
    [InlineData(1280, 900, "delete", ".unsubscribe-modal")]
    public async Task Danger_dialog_is_centered_and_not_clipped(int width, int height, string danger, string modalSelector)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var returnPath = $"/candidate/paspoort?tab=data&danger={danger}";
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(returnPath)}");
        await page.GotoAsync($"{baseUrl}{returnPath}", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(modalSelector).WaitForAsync(new() { Timeout = 30_000 });

        var layout = await page.EvaluateAsync<ModalLayout>(
            """
            (selector) => {
              const el = document.querySelector(selector);
              if (!el) return null;
              const r = el.getBoundingClientRect();
              return {
                left: r.left,
                right: r.right,
                width: r.width,
                viewportWidth: window.innerWidth
              };
            }
            """,
            modalSelector);

        Assert.NotNull(layout);
        Assert.True(layout!.Left >= 15, $"Modal left edge clipped: left={layout.Left} at {width}px.");
        Assert.True(layout.Right <= layout.ViewportWidth - 15,
            $"Modal right edge clipped: right={layout.Right}, vw={layout.ViewportWidth} at {width}px.");
        Assert.True(layout.Width <= layout.ViewportWidth - 32 + 1,
            $"Modal wider than calc(100vw - 32px): width={layout.Width}, vw={layout.ViewportWidth}.");
        var center = layout.Left + layout.Width / 2;
        var viewportCenter = layout.ViewportWidth / 2;
        Assert.True(Math.Abs(center - viewportCenter) < 8,
            $"Modal not centered: center={center}, viewportCenter={viewportCenter} at {width}px.");
    }

    private sealed class ModalLayout
    {
        public double Left { get; set; }
        public double Right { get; set; }
        public double Width { get; set; }
        public double ViewportWidth { get; set; }
    }
}
