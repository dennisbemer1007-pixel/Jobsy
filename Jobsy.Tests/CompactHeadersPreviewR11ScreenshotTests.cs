using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>R11: career 390 text/art separation, onboarding mobile regression, h1 + clip checks.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR11ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/workspace/artifacts/compact-headers-preview-r11";

    private IllustratedPagesWebFactory? _web;
    private IllustratedPagesWebFactory? _onboardingWeb;
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
        IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        _onboardingWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
        IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
        _ = _web.ServerAddress;
        _ = _onboardingWeb.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
        _onboardingWeb?.Dispose();
    }

    [Fact]
    public async Task Capture_compact_header_preview_r11_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var width in new[] { 390, 1440 })
        {
            var mCareer = await CaptureAsync(_web!.ServerAddress, "/carriere", width, "nl-NL", $"carriere-c-{width}.png", onboarding: false);
            AssertPageContract("carriere", width, mCareer);
            if (width <= 390)
            {
                Assert.True(mCareer.CareerLeadArtOverlap,
                    "carriere 390: lead must not overlap hero art figures.");
                Assert.True(mCareer.HeroHeadingInViewport,
                    "carriere 390: hero h1 must appear in the top capture.");
            }

            report.Add(Row("carriere", width, mCareer));

            var mOb = await CaptureAsync(_onboardingWeb!.ServerAddress, "/candidate/start", width, "nl-NL",
                $"onboarding-kennismaken-c-{width}-top.png", onboarding: true);
            AssertPageContract("onboarding-kennismaken", width, mOb);
            if (width <= 390)
            {
                Assert.True(mOb.OnboardingUsesMobileCoverOnly,
                    "onboarding 390: compact hero must be hidden; mobile cover only.");
                Assert.False(mOb.OnboardingMobileCoverBordered,
                    "onboarding 390: mobile cover must be full-bleed (no card border).");
            }

            report.Add(Row("onboarding-kennismaken", width, mOb));
        }

        var mPass = await CaptureAsync(_web!.ServerAddress, "/candidate/paspoort", 390, "nl-NL", "paspoort-c-390.png", onboarding: false);
        AssertPageContract("paspoort", 390, mPass);
        report.Add(Row("paspoort", 390, mPass));

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r11-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object Row(string page, int width, PageMetrics m) => new
    {
        page,
        width,
        m.VisibleH1Count,
        m.VisibleH1Texts,
        m.ClippedTextCount,
        m.ClippedSelectors,
        m.CareerLeadArtOverlap,
        m.HeroHeadingInViewport,
        m.OnboardingUsesMobileCoverOnly,
        m.OnboardingMobileCoverBordered,
        m.BandUsesPaspoortToken
    };

    private static void AssertPageContract(string page, int width, PageMetrics m)
    {
        Assert.Equal(1, m.VisibleH1Count);
        Assert.Equal(0, m.ClippedTextCount);
        Assert.Contains(m.VisibleH1Texts, t => !string.IsNullOrWhiteSpace(t));
        if (page is "carriere" or "paspoort")
        {
            Assert.True(m.BandUsesPaspoortToken,
                $"{page} c@{width}: band token {CompactHeroPreviewBand.Token}.");
        }
    }

    private async Task<PageMetrics> CaptureAsync(
        string server,
        string path,
        int width,
        string locale,
        string fileName,
        bool onboarding)
    {
        var height = width <= 390 ? 844 : 900;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = locale,
            IsMobile = width <= 390,
            HasTouch = width <= 390
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var browserPage = await context.NewPageAsync();
        browserPage.SetDefaultTimeout(120_000);
        if (onboarding)
        {
            IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        }

        var pathWithQuery = path + "?heroPreview=c";
        try
        {
            await browserPage.GotoAsync(
                $"{server}/__test/sign-in?return={Uri.EscapeDataString(pathWithQuery)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await browserPage.GotoAsync($"{server}{pathWithQuery}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await browserPage.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });

            if (onboarding)
            {
                await browserPage.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 120_000 });
            }
            else if (path.Contains("carriere", StringComparison.Ordinal))
            {
                await browserPage.Locator(".career-page--compact-preview-c .cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
            }
            else
            {
                await browserPage.Locator(".cand-compact-hero").First.WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.EvaluateAsync("() => window.scrollTo(0, 0)");
            await browserPage.WaitForTimeoutAsync(900);
            var metrics = await EvaluateMetricsAsync(browserPage, path, width);
            await File.WriteAllBytesAsync(
                Path.Combine(ArtifactsDir, fileName),
                await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false }));
            return metrics;
        }
        finally
        {
            if (onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static async Task<PageMetrics> EvaluateMetricsAsync(IPage browserPage, string path, int width)
    {
        var auditJson = await browserPage.EvaluateAsync<JsonElement>(
            """
            () => {
              const isVisible = (el) => {
                if (!el) return false;
                if (el.closest('.visually-hidden') || el.classList.contains('visually-hidden')) return false;
                const r = el.getBoundingClientRect();
                if (r.width < 1 || r.height < 1) return false;
                const s = getComputedStyle(el);
                if (s.display === 'none' || s.visibility === 'hidden' || s.opacity === '0') return false;
                return true;
              };
              const isClipped = (el) => {
                const s = getComputedStyle(el);
                if (s.overflow === 'hidden' && (s.textOverflow === 'ellipsis' || s.whiteSpace === 'nowrap')) return true;
                return el.scrollHeight > el.clientHeight + 1;
              };
              const textSel = 'h1, .cand-hero__lead, .ob-wizard__lead, .ob-illustrated-step__mobile-lead, .ob-illustrated-step__mobile-cover-copy h1, .ob-illustrated-step__mobile-cover-copy p';
              const clipped = [...document.querySelectorAll(textSel)].filter(el => isVisible(el) && isClipped(el));
              const h1s = [...document.querySelectorAll('h1')].filter(isVisible);
              const compact = document.querySelector('.ob-illustrated-step--compact-preview-c .cand-compact-hero--onboarding');
              const cover = document.querySelector('.ob-illustrated-step--compact-preview-c .ob-illustrated-step__mobile-cover');
              let coverBordered = false;
              if (cover) {
                const cs = getComputedStyle(cover);
                coverBordered = parseFloat(cs.borderWidth) > 0.5 || parseFloat(cs.borderTopWidth) > 0.5;
              }
              const lead = document.querySelector('.career-page--compact-preview-c .cand-hero__lead');
              const art = document.querySelector('.career-page--compact-preview-c .cand-hero__compact-c-art');
              const heroH1 = document.querySelector('.career-page--compact-preview-c .cand-compact-hero--onboarding h1');
              let leadArtOk = true;
              if (lead && art && isVisible(lead) && isVisible(art)) {
                const ar = art.getBoundingClientRect();
                const artLeft = ar.left;
                const rects = [...lead.getClientRects()];
                if (!rects.length) {
                  rects.push(lead.getBoundingClientRect());
                }
                leadArtOk = rects.every(r => r.width < 1 || r.right <= artLeft + 1);
              }
              let heroHeadingInViewport = true;
              if (heroH1 && isVisible(heroH1)) {
                const hr = heroH1.getBoundingClientRect();
                heroHeadingInViewport = hr.top >= -2 && hr.top < 320 && hr.height > 8;
              }
              return {
                visibleH1Texts: h1s.map(h => (h.textContent || '').trim()).filter(Boolean),
                clippedSelectors: clipped.slice(0, 8).map(el => {
                  const cls = el.className && typeof el.className === 'string' ? el.className.split(' ')[0] : '';
                  return el.tagName + (cls ? '.' + cls : '');
                }),
                careerLeadArtOverlap: leadArtOk,
                heroHeadingInViewport,
                onboardingCompactHidden: !compact || getComputedStyle(compact).display === 'none',
                onboardingCoverVisible: !!cover && isVisible(cover),
                onboardingMobileCoverBordered: coverBordered
              };
            }
            """);

        var audit = auditJson.ValueKind == JsonValueKind.Object
            ? auditJson.Deserialize<TextAudit>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TextAudit()
            : new TextAudit();

        var heroSelector = path.Contains("carriere", StringComparison.Ordinal)
            ? ".career-page--compact-preview-c .cand-compact-hero--onboarding"
            : path.Contains("paspoort", StringComparison.Ordinal)
                ? ".passport-page--illustrated .cand-compact-hero"
                : width <= 390
                    ? ".ob-illustrated-step--compact-preview-c .ob-illustrated-step__mobile-cover"
                    : ".ob-illustrated-step--compact-preview-c .cand-compact-hero--onboarding";

        var token = CompactHeroPreviewBand.Token.ToLowerInvariant();
        var bandOk = await browserPage.EvaluateAsync<bool>(
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
            new { heroSel = heroSelector, tokenHex = token });

        return new PageMetrics
        {
            VisibleH1Count = audit.VisibleH1Texts?.Count ?? 0,
            VisibleH1Texts = audit.VisibleH1Texts ?? [],
            ClippedTextCount = audit.ClippedSelectors?.Count ?? 0,
            ClippedSelectors = audit.ClippedSelectors ?? [],
            CareerLeadArtOverlap = audit.CareerLeadArtOverlap,
            HeroHeadingInViewport = audit.HeroHeadingInViewport,
            OnboardingUsesMobileCoverOnly = audit.OnboardingCompactHidden && audit.OnboardingCoverVisible,
            OnboardingMobileCoverBordered = audit.OnboardingMobileCoverBordered,
            BandUsesPaspoortToken = bandOk
        };
    }

    private sealed class PageMetrics
    {
        public int VisibleH1Count { get; set; }
        public List<string> VisibleH1Texts { get; set; } = [];
        public int ClippedTextCount { get; set; }
        public List<string> ClippedSelectors { get; set; } = [];
        public bool CareerLeadArtOverlap { get; set; }
        public bool HeroHeadingInViewport { get; set; } = true;
        public bool OnboardingUsesMobileCoverOnly { get; set; }
        public bool OnboardingMobileCoverBordered { get; set; }
        public bool BandUsesPaspoortToken { get; set; }
    }

    private sealed class TextAudit
    {
        public List<string> VisibleH1Texts { get; set; } = [];
        public List<string> ClippedSelectors { get; set; } = [];
        public bool CareerLeadArtOverlap { get; set; }
        public bool HeroHeadingInViewport { get; set; } = true;
        public bool OnboardingCompactHidden { get; set; }
        public bool OnboardingCoverVisible { get; set; }
        public bool OnboardingMobileCoverBordered { get; set; }
    }
}
