using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class HotfixFitPillScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/hotfix-fitpill";
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
    public async Task Capture_hotfix_fitpill_banenkaart_popups()
    {
        Directory.CreateDirectory(ArtifactsDir);
        await CaptureBanenkaartPopupsAsync(ArtifactsDir, viewportOnly: true, popupElementShot: true);
        var pngs = Directory.GetFiles(ArtifactsDir, "*.png");
        Assert.Equal(2, pngs.Length);
    }

    private async Task CaptureBanenkaartPopupsAsync(string artifactsDir = ArtifactsDir, bool viewportOnly = false, bool popupElementShot = false)
    {
        const string withPhotoId = "b1000001-0000-0000-0000-000000000001";

        foreach (var (width, height, tag, vacancyId, suffix) in new[]
                 {
                     (1440, 900, "1440", withPhotoId, "popup-photo"),
                     (390, 844, "390", withPhotoId, "popup-photo"),
                 })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await page.GotoAsync(
                $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString("/banenkaart?weergave=kaart")}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{_web.ServerAddress}/banenkaart?weergave=kaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await WaitForBanenkaartReadyAsync(page, preferListForCards: false);
            await page.WaitForFunctionAsync(
                "() => window.jobMap && typeof window.jobMap.__testOpenVacancyById === 'function'",
                null,
                new() { Timeout = 120_000 });
            await page.EvaluateAsync(
                """
                async (id) => {
                  const map = window.jobMap;
                  if (map.__testGetMap && map.__testGetMap()) {
                    map.__testGetMap().jumpTo({ center: [4.59, 52.22], zoom: 10.5, animate: false });
                  }
                  await map.reloadPins('/api/vacancies/pins?transport=Fiets&maxMinutes=30&radiusKm=40');
                  for (let i = 0; i < 40 && map.__testGetPinCount() < 1; i++) {
                    await new Promise(r => setTimeout(r, 100));
                  }
                  map.__testOpenVacancyById(id);
                }
                """,
                vacancyId);
            await page.Locator(".map-popup:not(.map-popup--skeleton)").WaitForAsync(new() { Timeout = 90_000 });
            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .kb-start-prompt { display: none !important; }
                    .jobsy-discovery.kb-start-prompt-open::before { display: none !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });
            await page.WaitForTimeoutAsync(400);

            var popup = page.Locator(".map-popup:not(.map-popup--skeleton)").First;
            await popup.WaitForAsync(new() { Timeout = 90_000 });
            await popup.ScrollIntoViewIfNeededAsync();

            if (suffix == "popup-photo")
            {
                var photo = popup.Locator(".map-popup__photo:not(.map-popup__photo--figure)");
                Assert.True(await photo.CountAsync() > 0, "Vacaturefoto moet zichtbaar zijn in popup met foto.");
            }
            else
            {
                var figure = popup.Locator(".map-popup__photo--illustrated, .map-popup__photo--figure");
                Assert.True(await figure.CountAsync() > 0, "Illustratie-fallback verwacht in popup zonder foto.");
            }

            var popupText = await popup.InnerTextAsync();
            Assert.DoesNotContain("% past bij jou", popupText, StringComparison.OrdinalIgnoreCase);

            var file = Path.Combine(artifactsDir, $"banenkaart-{suffix}-{tag}.png");
            if (popupElementShot)
            {
                await page.Locator(".map-cluster-card").First.ScreenshotAsync(new() { Path = file });
            }
            else if (viewportOnly)
            {
                await page.ScreenshotAsync(new() { Path = file });
            }
            else
            {
                await popup.ScreenshotAsync(new() { Path = file });
            }

            Assert.True(File.Exists(file));
        }
    }

    private static async Task WaitForBanenkaartReadyAsync(IPage page, bool preferListForCards = true)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        if (preferListForCards)
        {
            var listBtn = page.Locator("[data-testid=kb-view-lijst]").First;
            if (await listBtn.CountAsync() > 0 && await listBtn.IsVisibleAsync())
            {
                await listBtn.ClickAsync();
            }
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              if (document.querySelector('.job-card') || document.querySelector('[data-testid=kb-list-row]')) return true;
              const m = window.jobMap;
              return m && m.__testGetPinCount && m.__testGetPinCount() >= 1;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var error = page.Locator(".state-message.error");
        if (await error.CountAsync() > 0)
        {
            var text = await error.First.InnerTextAsync();
            Assert.Fail($"Banenkaart load error: {text}");
        }
    }

}
