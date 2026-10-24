using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class SollicitatiesDesktopWidthPlaywrightTests
{
    [Fact]
    public async Task Applications_list_column_is_wide_at_1280()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
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
        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });

        var stylesheetAudit = await page.EvaluateAsync<StylesheetAudit>(@"() => {
            const hrefs = [...document.querySelectorAll('link[rel=""stylesheet""]')].map(l => l.getAttribute('href') || '');
            return {
                hasSollicLayout9: hrefs.some(h => h.includes('sollicitaties-page.css') && h.includes('layout9')),
                hasApplicationsCss: hrefs.some(h => h.includes('applications.css')),
                hasAppMin: hrefs.some(h => h.includes('app.min.css')),
            };
        }");
        Assert.True(stylesheetAudit.HasSollicLayout9, "Expected sollicitaties-page.css layout9 in document head.");
        Assert.False(stylesheetAudit.HasApplicationsCss, "applications.css must not load on SSR sollicitaties (720px cap).");
        Assert.False(stylesheetAudit.HasAppMin, "app.min.css must not load on SSR sollicitaties NoBlazor shell.");

        var metrics = await page.EvaluateAsync<LayoutMetrics>(@"() => {
            const shell = document.querySelector('.sollicitaties-shell');
            const list = document.querySelector('.sollicitaties-list') || document.querySelector('.sollic-card');
            const layout = document.querySelector('.sollicitaties-layout');
            const pick = (el) => {
                if (!el) return null;
                const r = el.getBoundingClientRect();
                const cs = getComputedStyle(el);
                return { w: r.width, x: r.x, maxWidth: cs.maxWidth, gridCols: cs.gridTemplateColumns };
            };
            return {
                hasSsr: !!document.querySelector('[data-testid=""applications-page-ssr""]'),
                hasLegacyKbApps: !!document.querySelector('.kb-apps__layout'),
                shell: pick(shell),
                layout: pick(layout),
                list: pick(list),
            };
        }");

        Assert.True(metrics.HasSsr, "Expected SSR sollicitaties page markup.");
        Assert.False(metrics.HasLegacyKbApps, "Legacy kb-apps layout must not be used on /candidate/applications.");
        Assert.NotNull(metrics.Shell);
        Assert.True(metrics.Shell!.W >= 1000, $"shell width {metrics.Shell.W} at 1280 (x={metrics.Shell.X})");
        if (metrics.List is not null)
        {
            Assert.True(metrics.List.W >= 980, $"list/card width {metrics.List.W} at 1280");
        }
    }

    private sealed class StylesheetAudit
    {
        public bool HasSollicLayout9 { get; set; }
        public bool HasApplicationsCss { get; set; }
        public bool HasAppMin { get; set; }
    }

    private sealed class LayoutMetrics
    {
        public bool HasSsr { get; set; }
        public bool HasLegacyKbApps { get; set; }
        public Box? Shell { get; set; }
        public Box? Layout { get; set; }
        public Box? List { get; set; }
    }

    private sealed class Box
    {
        public double W { get; set; }
        public double X { get; set; }
        public string? MaxWidth { get; set; }
        public string? GridCols { get; set; }
    }
}
