using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>R12: paspoort cover pattern, career art, onboarding logo, word-break + clip gates.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR12ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/workspace/artifacts/compact-headers-preview-r12";

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
    public async Task Capture_compact_header_preview_r12_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        var mCareer = await CaptureAsync(_web!.ServerAddress, "/carriere", 390, "nl-NL", "carriere-c-390.png", onboarding: false);
        AssertHeroContract("carriere", 390, mCareer);
        Assert.True(mCareer.CareerLeadArtOverlap, "carriere 390: lead lines must stay left of art.");
        Assert.True(mCareer.CareerArtVisible, "carriere 390: faded art must show in the right column.");
        report.Add(Row("carriere", 390, mCareer));

        var mOb = await CaptureAsync(_onboardingWeb!.ServerAddress, "/candidate/start", 390, "nl-NL",
            "onboarding-kennismaken-c-390-top.png", onboarding: true);
        AssertHeroContract("onboarding-kennismaken", 390, mOb);
        Assert.True(mOb.OnboardingLogoHeightPx >= 36, $"onboarding 390: logo height {mOb.OnboardingLogoHeightPx}px (min 36).");
        report.Add(Row("onboarding-kennismaken", 390, mOb));

        foreach (var width in new[] { 390, 1440 })
        {
            var mPass = await CaptureAsync(_web!.ServerAddress, "/candidate/paspoort", width, "nl-NL", $"paspoort-c-{width}.png", onboarding: false);
            AssertHeroContract("paspoort", width, mPass);
            if (width <= 390)
            {
                Assert.False(mPass.MidWordBreakInHero, "paspoort 390: greeting must not break mid-word.");
                Assert.True(mPass.PassportTabsClearOfOverview, "paspoort 390: tabs must not overlap overview at rest.");
            }

            report.Add(Row("paspoort", width, mPass));
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r12-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object Row(string page, int width, PageMetrics m) => new
    {
        page,
        width,
        m.VisibleH1Count,
        m.ClippedTextCount,
        m.MidWordBreakInHero,
        m.CareerLeadArtOverlap,
        m.CareerArtVisible,
        m.OnboardingLogoHeightPx,
        m.PassportTabsClearOfOverview,
        m.BandUsesPaspoortToken
    };

    private static void AssertHeroContract(string page, int width, PageMetrics m)
    {
        Assert.Equal(1, m.VisibleH1Count);
        Assert.Equal(0, m.ClippedTextCount);
        if (page is "carriere" or "paspoort")
        {
            Assert.True(m.BandUsesPaspoortToken, $"{page} c@{width}: band token {CompactHeroPreviewBand.Token}.");
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
                await browserPage.Locator(".passport-page--illustrated .cand-compact-hero").First.WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.EvaluateAsync("() => window.scrollTo(0, 0)");
            await browserPage.WaitForTimeoutAsync(900);
            var metrics = await EvaluateMetricsAsync(browserPage, path, onboarding);
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

    private static async Task<PageMetrics> EvaluateMetricsAsync(IPage browserPage, string path, bool onboarding)
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
              const hasMidWordBreak = (el) => {
                if (!el || !isVisible(el)) return false;
                const doc = el.ownerDocument;
                const textNodes = [];
                const walker = doc.createTreeWalker(el, NodeFilter.SHOW_TEXT);
                let n;
                while (n = walker.nextNode()) textNodes.push(n);
                for (const tn of textNodes) {
                  const raw = tn.textContent || '';
                  const parts = raw.split(/(\s+|[,.!?;:])/);
                  let offset = 0;
                  for (const part of parts) {
                    if (!part || /^\s+$/.test(part) || /^[,.!?;:]$/.test(part)) {
                      offset += part.length;
                      continue;
                    }
                    const letters = part.replace(/[^A-Za-zÀ-ÿ]/g, '');
                    if (letters.length < 4) {
                      offset += part.length;
                      continue;
                    }
                    const range = doc.createRange();
                    range.setStart(tn, offset);
                    range.setEnd(tn, offset + part.length);
                    if (range.getClientRects().length > 1) return true;
                    offset += part.length;
                  }
                }
                return false;
              };
              const textSel = 'h1, .cand-hero__lead, .ob-wizard__lead, .ob-illustrated-step__mobile-lead, .ob-illustrated-step__mobile-cover-copy h1, .ob-illustrated-step__mobile-cover-copy p, .passport-page--illustrated .cand-compact-hero h1, .passport-page--illustrated .cand-compact-hero .cand-hero__lead';
              const clipped = [...document.querySelectorAll(textSel)].filter(el => isVisible(el) && isClipped(el));
              const h1s = [...document.querySelectorAll('h1')].filter(isVisible);
              const lead = document.querySelector('.career-page--compact-preview-c .cand-hero__lead');
              const art = document.querySelector('.career-page--compact-preview-c .cand-hero__compact-c-art');
              let leadArtOk = true;
              let careerArtVisible = true;
              if (lead && art && isVisible(lead) && isVisible(art)) {
                const ar = art.getBoundingClientRect();
                const rects = [...lead.getClientRects()];
                if (!rects.length) rects.push(lead.getBoundingClientRect());
                leadArtOk = rects.every(r => r.width < 1 || r.right <= ar.left + 1);
                careerArtVisible = ar.width >= window.innerWidth * 0.28 && ar.height >= 40;
              }
              const passH1 = document.querySelector('.passport-page--illustrated .cand-compact-hero h1');
              const logoImg = document.querySelector('.ob-wizard--compact-preview-c .ob-wizard__topbar-logo img, .ob-wizard--compact-preview-c .ob-wizard__topbar-brand img');
              let logoH = 0;
              if (logoImg && isVisible(logoImg)) {
                logoH = logoImg.getBoundingClientRect().height;
              }
              const tabsShell = document.querySelector('.passport-page--illustrated .passport-tabs-shell');
              const overview = document.querySelector('.passport-page--illustrated .passport-overview');
              let tabsClear = true;
              if (tabsShell && overview && isVisible(tabsShell) && isVisible(overview)) {
                const tr = tabsShell.getBoundingClientRect();
                const or = overview.getBoundingClientRect();
                tabsClear = tr.top >= or.bottom - 2;
              }
              return {
                visibleH1Count: h1s.length,
                clippedCount: clipped.length,
                midWordBreak: passH1 ? hasMidWordBreak(passH1) : false,
                careerLeadArtOverlap: leadArtOk,
                careerArtVisible,
                onboardingLogoHeightPx: Math.round(logoH),
                passportTabsClearOfOverview: tabsClear
              };
            }
            """);

        var audit = auditJson.Deserialize<TextAudit>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TextAudit();

        var heroSelector = onboarding
            ? ".ob-illustrated-step--compact-preview-c .ob-illustrated-step__mobile-cover"
            : path.Contains("carriere", StringComparison.Ordinal)
                ? ".career-page--compact-preview-c .cand-compact-hero--onboarding"
                : ".passport-page--illustrated .cand-compact-hero";

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
            VisibleH1Count = audit.VisibleH1Count,
            ClippedTextCount = audit.ClippedCount,
            MidWordBreakInHero = audit.MidWordBreak,
            CareerLeadArtOverlap = audit.CareerLeadArtOverlap,
            CareerArtVisible = audit.CareerArtVisible,
            OnboardingLogoHeightPx = audit.OnboardingLogoHeightPx,
            PassportTabsClearOfOverview = audit.PassportTabsClearOfOverview,
            BandUsesPaspoortToken = bandOk
        };
    }

    private sealed class PageMetrics
    {
        public int VisibleH1Count { get; set; }
        public int ClippedTextCount { get; set; }
        public bool MidWordBreakInHero { get; set; }
        public bool CareerLeadArtOverlap { get; set; } = true;
        public bool CareerArtVisible { get; set; } = true;
        public int OnboardingLogoHeightPx { get; set; }
        public bool PassportTabsClearOfOverview { get; set; } = true;
        public bool BandUsesPaspoortToken { get; set; }
    }

    private sealed class TextAudit
    {
        public int VisibleH1Count { get; set; }
        public int ClippedCount { get; set; }
        public bool MidWordBreak { get; set; }
        public bool CareerLeadArtOverlap { get; set; } = true;
        public bool CareerArtVisible { get; set; } = true;
        public int OnboardingLogoHeightPx { get; set; }
        public bool PassportTabsClearOfOverview { get; set; } = true;
    }
}
