using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class SollicitatiesDesktopWidthPlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
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
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (_localBaseUrl is not null && string.Equals(baseUrl, _localBaseUrl, StringComparison.Ordinal))
        {
            await page.GotoAsync(baseUrl + "/__test/sign-in?return=/candidate/applications");
        }
        else
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);

            var email = baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
                || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                ? KbE2e.DefaultCompleteEmail
                : "onboarding.e2e@jobsy.local";
            if (!await KbE2e.TryLoginAsync(page, baseUrl, email, KbE2e.DefaultPassword))
            {
                throw new InvalidOperationException($"Could not log in as {email} at {baseUrl}.");
            }

            await page.GotoAsync(baseUrl + "/candidate/applications", new() { WaitUntil = WaitUntilState.NetworkIdle });
        }
        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var metrics = await page.EvaluateAsync<LayoutMetrics>(@"() => {
            const shell = document.querySelector('.sollicitaties-shell');
            const list = document.querySelector('.sollicitaties-list') || document.querySelector('.sollic-card');
            const layout = document.querySelector('.sollicitaties-layout');
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
                hasSsr: !!document.querySelector('[data-testid=""applications-page-ssr""]'),
                hasLegacyKbApps: !!document.querySelector('.kb-apps__layout'),
                bottomNavVisible: !!document.querySelector('[data-testid=""candidate-bottom-nav-ssr""]'),
                bottomNavBox: pick(document.querySelector('[data-testid=""candidate-bottom-nav-ssr""]')),
                stylesheetCount: document.querySelectorAll('link[rel=""stylesheet""]').length,
                hasAppMin: !!document.querySelector('link[href*=""app.min.css""]'),
                hasNoBlazorRouteCss: !!document.querySelector('link[data-noblazor-route-css][href*=""sollicitaties-page.css""]'),
                shell: pick(shell),
                layout: pick(layout),
                list: pick(list),
                h1: pick(h1),
                chips: pick(chips),
                toolbar: pick(toolbar),
            };
        }");

        var layoutLine =
            $"1280 layout: shell x={metrics.Shell?.X:F0}..{metrics.Shell?.Right:F0} (w={metrics.Shell?.W:F0}), "
            + $"h1 x={metrics.H1?.X:F0}..{metrics.H1?.Right:F0} (w={metrics.H1?.W:F0}), "
            + $"chips x={metrics.Chips?.X:F0}..{metrics.Chips?.Right:F0} (w={metrics.Chips?.W:F0}), "
            + $"toolbar x={metrics.Toolbar?.X:F0}..{metrics.Toolbar?.Right:F0} (w={metrics.Toolbar?.W:F0}), "
            + $"bottomNav x={metrics.BottomNavBox?.X:F0}..{metrics.BottomNavBox?.Right:F0} (w={metrics.BottomNavBox?.W:F0}), "
            + $"stylesheets={metrics.StylesheetCount}, noblazorHeadCss={metrics.HasNoBlazorRouteCss}";
        _output.WriteLine(layoutLine);
        if (string.Equals(Environment.GetEnvironmentVariable("JOBSY_DUMP_SOLLIC_LAYOUT"), "1", StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync("/tmp/sollic-layout-metrics.txt", layoutLine + Environment.NewLine);
        }

        Assert.True(metrics.HasSsr, "Expected SSR sollicitaties page markup.");
        Assert.False(metrics.HasLegacyKbApps, "Legacy kb-apps layout must not be used on /candidate/applications.");
        Assert.NotNull(metrics.Shell);
        Assert.True(
            metrics.Shell!.W >= 1000,
            $"shell width {metrics.Shell.W} at 1280 (x={metrics.Shell.X}..{metrics.Shell.Right}, maxWidth={metrics.Shell.MaxWidth})");
        Assert.True(metrics.HasNoBlazorRouteCss || metrics.HasAppMin, "Expected render-blocking sollicitaties CSS in document head.");
        Assert.True(metrics.BottomNavVisible, "Expected SSR candidate bottom nav on applications.");
        if (metrics.BottomNavBox is not null)
        {
            Assert.True(metrics.BottomNavBox.W >= 1200, $"bottom nav width {metrics.BottomNavBox.W}");
        }

        if (metrics.H1 is not null)
        {
            Assert.True(
                metrics.H1.W >= 980,
                $"h1 width {metrics.H1.W} (x={metrics.H1.X:F0}..{metrics.H1.Right:F0})");
        }

        var chipsRow = metrics.Toolbar ?? metrics.Chips;
        if (chipsRow is not null)
        {
            Assert.True(
                chipsRow.W >= 980,
                $"toolbar/chips row width {chipsRow.W} (x={chipsRow.X:F0}..{chipsRow.Right:F0})");
        }

        if (metrics.List is not null)
        {
            Assert.True(metrics.List.W >= 980, $"list/card width {metrics.List.W} at 1280");
        }
    }

    private sealed class LayoutMetrics
    {
        public bool HasSsr { get; set; }
        public bool HasLegacyKbApps { get; set; }
        public bool BottomNavVisible { get; set; }
        public bool HasAppMin { get; set; }
        public bool HasNoBlazorRouteCss { get; set; }
        public int StylesheetCount { get; set; }
        public Box? BottomNavBox { get; set; }
        public Box? Shell { get; set; }
        public Box? Layout { get; set; }
        public Box? List { get; set; }
        public Box? H1 { get; set; }
        public Box? Chips { get; set; }
        public Box? Toolbar { get; set; }
        public List<Box>? H1Ancestors { get; set; }
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
