using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR5ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r5";

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
        IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
        _web = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
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
    public async Task Capture_compact_hero_rollout_r5_nl()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        var pages = new (string Path, string Slug)[]
        {
            ("/bedrijvenkaart", "bedrijvenkaart"),
            ("/banenkaart?weergave=kaart", "banenkaart"),
            ("/candidate/settings", "instellingen"),
            ("/candidate/applications", "sollicitaties"),
            ("/candidate/ontdekkingsreis", "reis"),
            ("/candidate/paspoort", "paspoort"),
            ("/candidate/start", "onboarding"),
        };

        foreach (var width in new[] { 390, 1440 })
        {
            foreach (var (path, slug) in pages)
            {
                if (width == 390 && slug is "onboarding")
                {
                    continue;
                }

                var file = $"{slug}-{width}-nl.png";
                var metrics = await CaptureAsync(path, width, slug, Path.Combine(ArtifactsDir, file));
                report.Add(new { slug, width, file, metrics });
            }
        }

        var onboarding390 = Path.Combine(ArtifactsDir, "onboarding-390-nl.png");
        report.Add(new
        {
            slug = "onboarding",
            width = 390,
            file = "onboarding-390-nl.png",
            metrics = await CaptureAsync("/candidate/start", 390, "onboarding", onboarding390)
        });

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r5-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<object> CaptureAsync(string path, int width, string slug, string outPath)
    {
        var height = width <= 390 ? 844 : 900;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = width <= 390,
            HasTouch = width <= 390
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        if (slug == "banenkaart")
        {
            await CompactHeadersRolloutR4ScreenshotTests.DismissBanenkaartStartPromptAsync(page);
            await CompactHeadersRolloutR4ScreenshotTests.WaitForBanenkaartMapReadyAsync(page);
        }

        var chromeCss = slug == "banenkaart"
            ? """
              .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
              """
            : """
              .pub-header, .bottom-nav, .candidate-bottom-nav,
              .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
              .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
              """;
        await page.AddStyleTagAsync(new() { Content = chromeCss });

        if (slug == "banenkaart")
        {
            await page.EvaluateAsync(
                """
                () => {
                  const m = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                  if (m && typeof m.resize === 'function') m.resize();
                }
                """);
            await CompactHeadersRolloutR4ScreenshotTests.WaitForBanenkaartMapReadyAsync(page);
        }

        if (slug == "onboarding")
        {
            await page.Locator(".ob-illustrated-step, .ob-wizard").First.WaitForAsync(new() { Timeout = 60_000 });
        }

        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(400);

        if (slug == "banenkaart" && width <= 390)
        {
            var mapOk = await CompactHeadersRolloutR4ScreenshotTests.MapRegionHasNonUniformPixelsAsync(page);
            Assert.True(mapOk, "banenkaart map region must show rendered tiles.");
        }

        await page.ScreenshotAsync(new() { Path = outPath, Type = ScreenshotType.Png, FullPage = false });

        return new { Path = path, Width = width };
    }
}
