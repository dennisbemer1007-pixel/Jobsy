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

    [Fact]
    public async Task Applications_list_column_is_wide_at_1280()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await NewApplicationsContextAsync(browser, baseUrl, new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (await TrySignInForApplicationsAsync(page, baseUrl, "/candidate/applications"))
        {
            // navigated
        }
        else
        {
            throw new InvalidOperationException($"Could not log in at {baseUrl}.");
        }

        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 120_000 });
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
            const hero = document.querySelector('.sollicitaties-hero-strip');
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
        var hasKreeftStrip = await page.EvaluateAsync<bool>(@"() => {
            const wrap = document.querySelector('.sollicitaties-page-wrap');
            return !!wrap?.querySelector('.sollicitaties-hero-strip, [data-testid=""kreeft-header-strip""]');
        }");
        Assert.False(hasKreeftStrip, "Kreeft header strip was removed on candidate pages (#1339).");
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

        if (metrics.List is not null && metrics.Layout is not null)
        {
            Assert.True(
                metrics.List.W >= 600,
                $"list column width {metrics.List.W} at 1280 (x={metrics.List.X:F0}..{metrics.List.Right:F0}, cols={metrics.Layout.GridCols})");
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
    }

    [Fact]
    public async Task Mobile_applications_and_berichten_have_no_kreeft_strip_and_consistent_header_at_390()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await NewApplicationsContextAsync(browser, baseUrl, KbE2e.ContextFor(390, 844));
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (!await TrySignInForApplicationsAsync(page, baseUrl, "/candidate/applications"))
        {
            throw new InvalidOperationException($"Could not log in at {baseUrl}.");
        }

        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });
        var appsHeader = await MeasureSollicMobileHeaderAsync(page);
        _output.WriteLine(
            $"390 applications header: contentOffset={appsHeader.ContentOffset:F1}, pageTop={appsHeader.PageTop:F1}, kreeftStrip={appsHeader.HasKreeftStrip}");
        Assert.False(appsHeader.HasKreeftStrip, "Sollicitaties must not render a Kreeft hero strip (#1339).");
        Assert.True(appsHeader.PageTop >= 0, $"panel-page visible (top={appsHeader.PageTop:F0}).");
        Assert.InRange(appsHeader.ContentOffset, 0, 140);

        await page.GotoAsync(
            baseUrl + "/candidate/applications?tab=berichten",
            new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });

        var tab = await page.QuerySelectorAsync("[data-testid=\"sollicitaties-berichten-tab\"]");
        if (tab is null)
        {
            _output.WriteLine("Skip berichten header parity: tab not rendered (AfterMatch off or no demo).");
            return;
        }

        var berichtenHeader = await MeasureSollicMobileHeaderAsync(page);
        _output.WriteLine(
            $"390 berichten header: contentOffset={berichtenHeader.ContentOffset:F1}, pageTop={berichtenHeader.PageTop:F1}, kreeftStrip={berichtenHeader.HasKreeftStrip}");
        Assert.False(berichtenHeader.HasKreeftStrip);
        Assert.True(Math.Abs(appsHeader.ContentOffset - berichtenHeader.ContentOffset) <= 8,
            $"intro offset should match between tabs (apps={appsHeader.ContentOffset:F1}, berichten={berichtenHeader.ContentOffset:F1}).");
        Assert.True(Math.Abs(appsHeader.PageTop - berichtenHeader.PageTop) <= 4,
            $"panel top should match between tabs (apps={appsHeader.PageTop:F1}, berichten={berichtenHeader.PageTop:F1}).");
    }

    private static Task<SollicMobileHeaderMetrics> MeasureSollicMobileHeaderAsync(IPage page) =>
        page.EvaluateAsync<SollicMobileHeaderMetrics>(@"() => {
            const wrap = document.querySelector('.sollicitaties-page-wrap');
            const pageRoot = document.querySelector('[data-testid=""applications-page-ssr""]');
            const intro = pageRoot?.querySelector('.sollicitaties-intro');
            const hasKreeftStrip = !!wrap?.querySelector('.sollicitaties-hero-strip, [data-testid=""kreeft-header-strip""]');
            const pr = pageRoot ? pageRoot.getBoundingClientRect() : null;
            const contentOffset = intro && pageRoot ? intro.offsetTop : 0;
            return {
                hasKreeftStrip,
                contentOffset,
                pageTop: pr ? pr.top : 0,
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
        await using var context = await NewApplicationsContextAsync(browser, baseUrl, KbE2e.MobileContext());
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (!await TrySignInForApplicationsAsync(page, baseUrl, "/candidate/applications"))
        {
            throw new InvalidOperationException($"Could not log in at {baseUrl}.");
        }

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
        await using var context = await NewApplicationsContextAsync(browser, baseUrl, KbE2e.ContextFor(390, 885));
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (!await TrySignInForApplicationsAsync(page, baseUrl, "/candidate/applications?tab=berichten"))
        {
            throw new InvalidOperationException($"Could not log in at {baseUrl}.");
        }

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
        Assert.Contains("after-match", firstHref, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("panel=messages", firstHref, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return=", firstHref, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BerichtenRowMetrics
    {
        public double W { get; set; }
        public double H { get; set; }
        public string? Display { get; set; }
        public string? Visibility { get; set; }
    }

    private sealed class SollicMobileHeaderMetrics
    {
        public bool HasKreeftStrip { get; set; }
        public double ContentOffset { get; set; }
        public double PageTop { get; set; }
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

    private async Task<IBrowserContext> NewApplicationsContextAsync(
        IBrowser browser,
        string baseUrl,
        BrowserNewContextOptions options)
    {
        if (_localBaseUrl is not null && string.Equals(baseUrl, _localBaseUrl, StringComparison.Ordinal))
        {
            return await browser.NewContextAsync(options);
        }

        if (FunctionalE2eGate.IsRequired || KbE2e.SupportsTestSignIn(baseUrl))
        {
            await FunctionalE2eAuth.WarmupStackAsync(baseUrl);
            return await FunctionalE2eAuth.NewCandidateContextAsync(browser, baseUrl, options);
        }

        return await browser.NewContextAsync(options);
    }

    private async Task<bool> TrySignInForApplicationsAsync(IPage page, string baseUrl, string returnPath)
    {
        if (_localBaseUrl is not null && string.Equals(baseUrl, _localBaseUrl, StringComparison.Ordinal))
        {
            await KbE2e.GotoTestSignInAsync(page, baseUrl, returnPath);
            return true;
        }

        if (KbE2e.SupportsTestSignIn(baseUrl) || FunctionalE2eGate.IsRequired)
        {
            await page.GotoAsync(baseUrl + returnPath, new() { WaitUntil = WaitUntilState.NetworkIdle });
            return true;
        }

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        if (!await TryAcceptatieOrDefaultLoginAsync(page, baseUrl))
        {
            return false;
        }

        if (baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase))
        {
            var ensure = await page.APIRequest.PostAsync(baseUrl + "/api/me/acceptatie-sollicitaties-demo-ensure");
            _output.WriteLine($"demo-ensure: {(int)ensure.Status} {ensure.StatusText}");
        }

        await page.GotoAsync(baseUrl + returnPath, new() { WaitUntil = WaitUntilState.NetworkIdle });
        return true;
    }

    private static async Task<bool> TryAcceptatieOrDefaultLoginAsync(IPage page, string baseUrl)
    {
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? KbE2e.DefaultPassword;
        var preferred = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL");
        if (!string.IsNullOrWhiteSpace(preferred)
            && await KbE2e.TryLoginAsync(page, baseUrl, preferred.Trim(), password))
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
