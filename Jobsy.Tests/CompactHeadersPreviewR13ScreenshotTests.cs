using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>R13: paspoort desktop band fade + tab shell; carrière 1440 regression.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR13ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/workspace/artifacts/compact-headers-preview-r13";

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
    public async Task Capture_compact_header_preview_r13_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var width in new[] { 1440, 1920 })
        {
            var mPass = await CaptureAsync("/candidate/paspoort", width, $"paspoort-c-{width}.png");
            Assert.True(mPass.HeroUsesOnboardingLayout, $"paspoort {width}: preview C must use onboarding compact hero.");
            Assert.True(mPass.HeroFullBleedWidth, $"paspoort {width}: hero band must span viewport (not ~1100 box).");
            Assert.True(mPass.BandUsesPaspoortToken, $"paspoort {width}: band token {CompactHeroPreviewBand.Token}.");
            if (width >= 1024)
            {
                Assert.True(mPass.TabsShellBackgroundTransparent,
                    $"paspoort {width}: tabs-shell pseudo background off at desktop.");
            }

            report.Add(new { page = "paspoort", width, mPass.HeroUsesOnboardingLayout, mPass.HeroFullBleedWidth, mPass.TabsShellBackgroundTransparent, mPass.BandUsesPaspoortToken });
        }

        var mCareer = await CaptureAsync("/carriere", 1440, "carriere-c-1440.png");
        Assert.True(mCareer.BandUsesPaspoortToken, "carriere 1440: band token unchanged.");
        Assert.True(mCareer.HeroFullBleedWidth, "carriere 1440: full-bleed hero.");
        report.Add(new { page = "carriere", width = 1440, mCareer.HeroFullBleedWidth, mCareer.BandUsesPaspoortToken });

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r13-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<PageMetrics> CaptureAsync(string path, int width, string fileName)
    {
        var height = width >= 1920 ? 1080 : 900;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var browserPage = await context.NewPageAsync();
        browserPage.SetDefaultTimeout(120_000);
        var pathWithQuery = path + "?heroPreview=c";
        await browserPage.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(pathWithQuery)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await browserPage.GotoAsync($"{_web.ServerAddress}{pathWithQuery}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await browserPage.AddStyleTagAsync(new()
        {
            Content = """
                .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                """
        });

        if (path.Contains("carriere", StringComparison.Ordinal))
        {
            await browserPage.Locator(".career-page--compact-preview-c .cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
        }
        else
        {
            await browserPage.Locator(".passport-page--compact-preview-c .cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
        }

        await browserPage.EvaluateAsync("() => window.scrollTo(0, 0)");
        await browserPage.WaitForTimeoutAsync(900);
        var metrics = await EvaluateMetricsAsync(browserPage, path);
        await File.WriteAllBytesAsync(
            Path.Combine(ArtifactsDir, fileName),
            await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false }));
        return metrics;
    }

    private static async Task<PageMetrics> EvaluateMetricsAsync(IPage browserPage, string path)
    {
        var audit = await browserPage.EvaluateAsync<PageMetrics>(
            """
            () => {
              const isPassport = window.location.pathname.includes('paspoort');
              const hero = document.querySelector(isPassport
                ? '.passport-page--compact-preview-c .cand-compact-hero--onboarding'
                : '.career-page--compact-preview-c .cand-compact-hero--onboarding');
              const heroBox = hero ? hero.getBoundingClientRect() : { width: 0, left: 0 };
              const shellBefore = document.querySelector('.passport-page--illustrated .passport-tabs-shell::before');
              const shell = document.querySelector('.passport-page--illustrated .passport-tabs-shell');
              let tabsShellBackgroundTransparent = true;
              if (shell && window.innerWidth >= 1024) {
                const beforeDisplay = getComputedStyle(shell, '::before').display;
                const afterDisplay = getComputedStyle(shell, '::after').display;
                tabsShellBackgroundTransparent = beforeDisplay === 'none' && afterDisplay === 'none';
              }
              return {
                heroUsesOnboardingLayout: !!hero,
                heroFullBleedWidth: heroBox.width >= window.innerWidth - 4 && heroBox.left <= 1,
                tabsShellBackgroundTransparent,
                bandUsesPaspoortToken: false
              };
            }
            """);

        var heroSelector = path.Contains("carriere", StringComparison.Ordinal)
            ? ".career-page--compact-preview-c .cand-compact-hero--onboarding"
            : ".passport-page--compact-preview-c .cand-compact-hero--onboarding";
        audit.BandUsesPaspoortToken = await browserPage.EvaluateAsync<bool>(
            """
            ({ heroSel, tokenHex }) => {
              const el = document.querySelector(heroSel);
              if (!el) return false;
              const bg = getComputedStyle(el).backgroundColor;
              const probe = document.createElement('div');
              probe.style.backgroundColor = tokenHex;
              document.body.appendChild(probe);
              const expected = getComputedStyle(probe).backgroundColor;
              probe.remove();
              return bg === expected;
            }
            """,
            new { heroSel = heroSelector, tokenHex = CompactHeroPreviewBand.Token.ToLowerInvariant() });

        return audit;
    }

    private sealed class PageMetrics
    {
        public bool HeroUsesOnboardingLayout { get; set; }
        public bool HeroFullBleedWidth { get; set; }
        public bool TabsShellBackgroundTransparent { get; set; }
        public bool BandUsesPaspoortToken { get; set; }
    }
}
