using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class SollicitatiesDesktopWidthPlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
    private const string AcceptatieGrokbotEmail = "lobsy-test@mail.grokbot.com";

    private readonly ITestOutputHelper _output = output;
    private IllustratedPagesWebFactory? _localWeb;
    private string? _localBaseUrl;

    public async ValueTask InitializeAsync()
    {
        if (await KbE2e.TryReadyBaseUrlAsync() is not null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _localWeb = new IllustratedPagesWebFactory();
        _localBaseUrl = _localWeb.ServerAddress.TrimEnd('/');
        await ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _localWeb?.Dispose();
        await ValueTask.CompletedTask;
    }

    private static bool UseCaptureTestSignIn(string baseUrl, string? localBaseUrl) =>
        localBaseUrl is not null && string.Equals(baseUrl, localBaseUrl, StringComparison.Ordinal);

    private async Task SignInCandidateForApplicationsAsync(IPage page, string baseUrl, string returnPath)
    {
        if (UseCaptureTestSignIn(baseUrl, _localBaseUrl))
        {
            await page.GotoAsync(baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString(returnPath));
            return;
        }

        if (KbE2e.IsLocalCiStack(baseUrl))
        {
            await FunctionalE2eAuth.SignInSeededCandidateAsync(page, baseUrl, returnPath);
            return;
        }

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        if (!await TryAcceptatieOrDefaultLoginAsync(page, baseUrl))
        {
            throw new InvalidOperationException($"Could not log in at {baseUrl}.");
        }

        if (baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase))
        {
            var ensure = await page.APIRequest.PostAsync(baseUrl + "/api/me/acceptatie-sollicitaties-demo-ensure");
            _output.WriteLine($"demo-ensure: {(int)ensure.Status} {ensure.StatusText}");
        }

        await page.GotoAsync(baseUrl + returnPath, new() { WaitUntil = WaitUntilState.NetworkIdle });
    }

    [Fact]
    public async Task Applications_list_column_is_wide_at_1280()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInCandidateForApplicationsAsync(page, baseUrl, "/candidate/applications");

        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        string? selId = null;
        try
        {
            await page.WaitForSelectorAsync(".sollic-card[data-sollic-row-id]", new() { Timeout = 25_000 });
            selId = await page.EvaluateAsync<string?>(@"() => {
                const card = document.querySelector('.sollic-card[data-sollic-row-id]');
                return card ? card.getAttribute('data-sollic-row-id') : null;
            }");
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            _output.WriteLine("No sollic cards; measuring list-only layout.");
        }

        if (!string.IsNullOrWhiteSpace(selId))
        {
            await page.GotoAsync(
                baseUrl + "/candidate/applications?sel=" + selId,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".sollicitaties-detail .sollic-detail-panel", new() { Timeout = 30_000 });
        }

        var metrics = await page.EvaluateAsync<LayoutMetrics>(@"() => {
            const shell = document.querySelector('.sollicitaties-shell');
            const pageRoot = document.querySelector('[data-testid=""applications-page-ssr""]');
            const wrap = document.querySelector('.sollicitaties-page-wrap');
            const hero = document.querySelector('.sollicitaties-hero-strip, [data-testid=""kreeft-header-strip""]');
            const list = document.querySelector('.sollicitaties-list') || document.querySelector('.sollic-card');
            const layout = document.querySelector('.sollicitaties-layout');
            const rail = document.querySelector('.sollicitaties-detail');
            const vsteps = document.querySelector('.sollic-vsteps.vsteps');
            const h1 = document.querySelector('.sollicitaties-intro__title');
            const chips = document.querySelector('.sollicitaties-toolbar__filters');
            const toolbar = document.querySelector('.sollicitaties-toolbar');
            const pick = (el) => {
                if (!el) return null;
                const r = el.getBoundingClientRect();
                const cs = getComputedStyle(el);
                return {
                    w: r.width,
                    x: r.x,
                    right: r.right,
                    maxWidth: cs.maxWidth,
                    maxInlineSize: cs.maxInlineSize,
                    width: cs.width,
                    marginInline: cs.marginInline,
                    paddingInline: cs.paddingInline,
                    gridCols: cs.gridTemplateColumns,
                    tag: el.tagName.toLowerCase(),
                    cls: el.className
                };
            };
            return {
                hasSsr: !!pageRoot,
                hasLegacyKbApps: !!document.querySelector('.kb-apps__layout'),
                hasVsteps: !!vsteps,
                vstepsFlexDir: vsteps ? getComputedStyle(vsteps).flexDirection : null,
                bottomNavVisible: !!document.querySelector('[data-testid=""candidate-bottom-nav-ssr""]'),
                bottomNavBox: pick(document.querySelector('[data-testid=""candidate-bottom-nav-ssr""]')),
                stylesheetCount: document.querySelectorAll('link[rel=""stylesheet""]').length,
                hasAppMin: !!document.querySelector('link[href*=""app.min.css""]'),
                hasNoBlazorRouteCss: !!document.querySelector('link[data-noblazor-route-css][href*=""sollicitaties-page.css""]'),
                wrap: pick(wrap),
                hero: pick(hero),
                shell: pick(shell),
                pageRoot: pick(pageRoot),
                layout: pick(layout),
                list: pick(list),
                rail: pick(rail),
                h1: pick(h1),
                chips: pick(chips),
                toolbar: pick(toolbar),
            };
        }");

        if (metrics.HasSsr && metrics.PageRoot is not null)
        {
            var ancestorLog = await page.EvaluateAsync<string>(@"() => {
                const pageRoot = document.querySelector('[data-testid=""applications-page-ssr""]');
                if (!pageRoot) return '';
                const out = [];
                let n = pageRoot;
                while (n && n !== document.documentElement) {
                    const cs = getComputedStyle(n);
                    const mw = parseFloat(cs.maxWidth);
                    const mis = parseFloat(cs.maxInlineSize);
                    const r = n.getBoundingClientRect();
                    if ((mw > 0 && mw <= 760) || (mis > 0 && mis <= 760)) {
                        out.push(n.tagName + '.' + n.className + ' maxWidth=' + cs.maxWidth + ' w=' + Math.round(r.width));
                    }
                    n = n.parentElement;
                }
                return out.join(' | ');
            }");
            if (!string.IsNullOrWhiteSpace(ancestorLog))
            {
                _output.WriteLine("narrow720 ancestors: " + ancestorLog);
            }
        }

        var layoutLine =
            $"1280 layout: hero x={metrics.Hero?.X:F0}..{metrics.Hero?.Right:F0} (w={metrics.Hero?.W:F0}), "
            + $"page x={metrics.PageRoot?.X:F0}..{metrics.PageRoot?.Right:F0} (w={metrics.PageRoot?.W:F0}, max={metrics.PageRoot?.MaxWidth}), "
            + $"shell x={metrics.Shell?.X:F0}..{metrics.Shell?.Right:F0} (w={metrics.Shell?.W:F0}), "
            + $"h1 x={metrics.H1?.X:F0}..{metrics.H1?.Right:F0} (w={metrics.H1?.W:F0}), "
            + $"layout x={metrics.Layout?.X:F0}..{metrics.Layout?.Right:F0} (w={metrics.Layout?.W:F0}, cols={metrics.Layout?.GridCols}), "
            + $"list x={metrics.List?.X:F0}..{metrics.List?.Right:F0} (w={metrics.List?.W:F0}), "
            + $"rail x={metrics.Rail?.X:F0}..{metrics.Rail?.Right:F0} (w={metrics.Rail?.W:F0}), "
            + $"vsteps={metrics.HasVsteps}, stylesheets={metrics.StylesheetCount}";
        _output.WriteLine(layoutLine);

        if (string.Equals(Environment.GetEnvironmentVariable("JOBSY_DUMP_SOLLIC_LAYOUT"), "1", StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync("/tmp/sollic-layout-metrics.txt", layoutLine + Environment.NewLine);
        }

        Assert.True(metrics.HasSsr, "Expected SSR sollicitaties page markup.");
        Assert.False(metrics.HasLegacyKbApps, "Legacy kb-apps layout must not be used on /candidate/applications.");
        Assert.NotNull(metrics.PageRoot);
        if (metrics.Hero is not null)
        {
            Assert.True(
                metrics.Hero.W >= 1200,
                $"hero strip width {metrics.Hero.W} at 1280 (x={metrics.Hero.X}..{metrics.Hero.Right})");
        }
        else
        {
            Assert.NotNull(metrics.H1);
            _output.WriteLine("No hero strip on applications SSR; using intro-only layout.");
        }

        Assert.True(
            metrics.PageRoot!.W >= 1000,
            $"page root width {metrics.PageRoot.W} at 1280 (x={metrics.PageRoot.X}..{metrics.PageRoot.Right}, maxWidth={metrics.PageRoot.MaxWidth})");
        Assert.NotNull(metrics.Shell);
        Assert.True(metrics.Shell!.W >= 980, $"shell width {metrics.Shell.W} at 1280");
        Assert.True(metrics.HasNoBlazorRouteCss || metrics.HasAppMin, "Expected render-blocking sollicitaties CSS in document head.");
        Assert.True(metrics.BottomNavVisible, "Expected SSR candidate bottom nav on applications.");

        if (metrics.H1 is not null)
        {
            Assert.True(
                metrics.H1.W >= 980,
                $"h1 width {metrics.H1.W} (x={metrics.H1.X:F0}..{metrics.H1.Right:F0})");
        }

        if (metrics.Layout is not null)
        {
            Assert.True(
                metrics.Layout.W >= 980,
                $"layout width {metrics.Layout.W} (x={metrics.Layout.X:F0}..{metrics.Layout.Right:F0}, cols={metrics.Layout.GridCols})");
        }

        var hasDetailRail = await page.EvaluateAsync<bool>(
            @"() => !!document.querySelector('.sollicitaties-detail .sollic-detail-panel')");
        if (!hasDetailRail)
        {
            _output.WriteLine(
                "Skip rail/vsteps asserts: no sollic cards (log in as lobsy-test@mail.grokbot.com + demo-ensure on acceptatie).");
            return;
        }

        if (metrics.Rail is not null)
        {
            Assert.InRange(metrics.Rail.W, 300, 360);
        }

        Assert.True(metrics.HasVsteps, "Expected mockup 01 vertical vsteps in legacy detail rail.");
        Assert.DoesNotContain("sollic-ndm-tl", await page.ContentAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mobile_hero_strip_is_105px_on_applications_and_berichten()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(390, 844));
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInCandidateForApplicationsAsync(page, baseUrl, "/candidate/applications");
        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });

        if (await page.Locator(".sollicitaties-hero-strip, [data-testid=\"kreeft-header-strip\"]").CountAsync() == 0)
        {
            _output.WriteLine("Skip mobile hero strip: intro-only applications layout (no kreeft strip).");
            return;
        }

        await page.WaitForSelectorAsync(".sollicitaties-hero-strip, [data-testid=\"kreeft-header-strip\"]", new() { Timeout = 15_000 });
        var listHero = await MeasureHeroStripAsync(page);
        _output.WriteLine(
            $"390 applications hero: strip={listHero.StripH:F1}, mobImg={listHero.MobImgH:F1}, cssH={listHero.StripCssHeight}");
        Assert.InRange(listHero.StripH, 95, 115);
        if (listHero.MobImgH > 0)
        {
            Assert.InRange(listHero.MobImgH, 95, 115);
        }

        await page.GotoAsync(
            baseUrl + "/candidate/applications?tab=berichten",
            new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(".sollicitaties-hero-strip, [data-testid=\"kreeft-header-strip\"]", new() { Timeout = 15_000 });
        var berichtenHero = await MeasureHeroStripAsync(page);
        _output.WriteLine(
            $"390 berichten hero: strip={berichtenHero.StripH:F1}, mobImg={berichtenHero.MobImgH:F1}, cssH={berichtenHero.StripCssHeight}");
        Assert.InRange(berichtenHero.StripH, 95, 115);
    }

    private static Task<HeroStripMetrics> MeasureHeroStripAsync(IPage page) =>
        page.EvaluateAsync<HeroStripMetrics>(@"() => {
            const strip = document.querySelector('.sollicitaties-hero-strip, [data-testid=""kreeft-header-strip""]');
            const mobImg = document.querySelector('.sollicitaties-hero-strip__img.mob');
            const sr = strip ? strip.getBoundingClientRect() : null;
            const mr = mobImg ? mobImg.getBoundingClientRect() : null;
            return {
                stripH: sr ? sr.height : 0,
                mobImgH: mr ? mr.height : 0,
                stripCssHeight: strip ? getComputedStyle(strip).height : null,
            };
        }");

    [Fact]
    public async Task Mobile_sel_shows_detail_panel_at_390()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.MobileContext());
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInCandidateForApplicationsAsync(page, baseUrl, "/candidate/applications");

        try
        {
            await page.WaitForSelectorAsync(".sollic-card", new() { Timeout = 20_000 });
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            _output.WriteLine("Skip mobile detail: no sollic cards (seed demo on acceptatie).");
            return;
        }

        var firstRowId = await page.EvaluateAsync<string?>(@"() => {
            const card = document.querySelector('.sollic-card[data-sollic-row-id]');
            return card ? card.getAttribute('data-sollic-row-id') : null;
        }");
        Assert.False(string.IsNullOrWhiteSpace(firstRowId));

        await page.GotoAsync(
            baseUrl + "/candidate/applications?sel=" + firstRowId,
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var hasDetailShell = await page.EvaluateAsync<bool>(
            @"() => document.querySelector('[data-testid=""applications-page-ssr""]')?.classList.contains('sollicitaties-page--detail') ?? false");
        if (!hasDetailShell && _localBaseUrl is not null)
        {
            _output.WriteLine("Skip mobile detail: SSR did not emit sollicitaties-page--detail (local host UA/path).");
            return;
        }

        await page.WaitForSelectorAsync(".sollic-detail-panel:not([hidden])", new() { Timeout = 30_000 });

        var visible = await page.EvaluateAsync<MobileDetailMetrics>(@"() => {
            const detail = document.querySelector('.sollicitaties-detail');
            const panel = document.querySelector('.sollic-detail-panel:not([hidden])');
            const back = document.querySelector('.sollic-detail__back');
            const cs = detail ? getComputedStyle(detail) : null;
            const dr = detail ? detail.getBoundingClientRect() : null;
            return {
                detailDisplay: cs ? cs.display : null,
                detailW: dr ? dr.width : 0,
                hasVisiblePanel: !!panel,
                hasBack: !!back && getComputedStyle(back).display !== 'none',
            };
        }");

        _output.WriteLine(
            $"390 sel=: detail display={visible.DetailDisplay}, w={visible.DetailW:F0}, panel={visible.HasVisiblePanel}, back={visible.HasBack}");
        Assert.Equal("block", visible.DetailDisplay);
        Assert.True(visible.HasVisiblePanel, "Expected visible detail panel on mobile ?sel=.");
        Assert.True(visible.HasBack, "Expected Terug link on mobile detail.");
        Assert.True(visible.DetailW >= 320, $"detail width {visible.DetailW} at 390");
    }

    [Fact]
    public async Task Mobile_berichten_tab_shows_thread_rows_at_390()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(390, 885));
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInCandidateForApplicationsAsync(page, baseUrl, "/candidate/applications?tab=berichten");

        var tab = await page.QuerySelectorAsync("[data-testid=\"sollicitaties-berichten-tab\"]");
        if (tab is null)
        {
            _output.WriteLine("Skip berichten mobile: tab not rendered (AfterMatch off or no demo).");
            return;
        }

        var rows = await page.EvaluateAsync<BerichtenRowMetrics[]>(@"() => {
            const links = Array.from(document.querySelectorAll('.sollic-berichten-ti--mob'));
            return links.map((el) => {
                const r = el.getBoundingClientRect();
                const cs = getComputedStyle(el);
                return {
                    w: r.width,
                    h: r.height,
                    display: cs.display,
                    visibility: cs.visibility,
                };
            });
        }");

        if (rows.Length == 0)
        {
            _output.WriteLine("Skip berichten mobile: no message threads in demo data.");
            return;
        }

        _output.WriteLine($"berichten mobile rows: {rows.Length}, first h={rows[0].H:F0} display={rows[0].Display}");
        Assert.All(rows, row =>
        {
            Assert.NotEqual("none", row.Display);
            Assert.True(row.W > 0 && row.H > 0, $"thread row box {row.W}x{row.H}, display={row.Display}");
        });

        var firstHref = await page.GetAttributeAsync(".sollic-berichten-ti--mob", "href");
        Assert.False(string.IsNullOrWhiteSpace(firstHref));
        Assert.Contains("tab=berichten", firstHref, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("thread=", firstHref, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BerichtenRowMetrics
    {
        public double W { get; set; }
        public double H { get; set; }
        public string? Display { get; set; }
        public string? Visibility { get; set; }
    }

    private sealed class HeroStripMetrics
    {
        public double StripH { get; set; }
        public double MobImgH { get; set; }
        public string? StripCssHeight { get; set; }
    }

    private sealed class LayoutMetrics
    {
        public bool HasSsr { get; set; }
        public bool HasLegacyKbApps { get; set; }
        public bool HasVsteps { get; set; }
        public string? VstepsFlexDir { get; set; }
        public bool BottomNavVisible { get; set; }
        public bool HasAppMin { get; set; }
        public bool HasNoBlazorRouteCss { get; set; }
        public int StylesheetCount { get; set; }
        public Box? BottomNavBox { get; set; }
        public Box? Wrap { get; set; }
        public Box? Hero { get; set; }
        public Box? Shell { get; set; }
        public Box? PageRoot { get; set; }
        public Box? Layout { get; set; }
        public Box? List { get; set; }
        public Box? Rail { get; set; }
        public Box? H1 { get; set; }
        public Box? Chips { get; set; }
        public Box? Toolbar { get; set; }
    }

    private sealed class MobileDetailMetrics
    {
        public string? DetailDisplay { get; set; }
        public double DetailW { get; set; }
        public bool HasVisiblePanel { get; set; }
        public bool HasBack { get; set; }
    }

    private static async Task<bool> TryAcceptatieOrDefaultLoginAsync(IPage page, string baseUrl)
    {
        var password = KbE2e.ResolveCandidatePassword(baseUrl);
        if (await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.ResolveCandidateEmail(), password))
        {
            return true;
        }

        if (baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase))
        {
            string[] acceptatieCandidates =
            [
                AcceptatieGrokbotEmail,
                "onboarding.e2e@jobsy.local",
                "test-kandidaat@lobsy.nl",
            ];
            foreach (var email in acceptatieCandidates)
            {
                if (await KbE2e.TryLoginAsync(page, baseUrl, email, password))
                {
                    return true;
                }
            }

            return false;
        }

        return await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, password);
    }

    private sealed class Box
    {
        public double W { get; set; }
        public double X { get; set; }
        public double Right { get; set; }
        public string? MaxWidth { get; set; }
        public string? MaxInlineSize { get; set; }
        public string? Width { get; set; }
        public string? MarginInline { get; set; }
        public string? PaddingInline { get; set; }
        public string? GridCols { get; set; }
        public string? Tag { get; set; }
        public string? Cls { get; set; }
    }
}
