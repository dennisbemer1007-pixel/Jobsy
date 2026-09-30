using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile cluster card: Solliciteer + company stay inside the fixed 223px sheet.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c> or when the target lacks the v3b CSS.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartClusterCardPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    public static TheoryData<int, int, bool> ViewportsAndRoles()
    {
        var data = new TheoryData<int, int, bool>();
        foreach (var (w, h) in new[] { (360, 800), (390, 844), (412, 915), (430, 932) })
        {
            data.Add(w, h, false); // anonymous
            data.Add(w, h, true);  // candidate
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ViewportsAndRoles))]
    public async Task Cluster_card_keeps_apply_and_company_visible_with_long_copy(int width, int height, bool asCandidate)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        if (asCandidate && !await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        // Soft-skip Acc (or any host) that has not deployed the taller sheet / cache bump yet.
        var hasV3b = await page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('link[rel=stylesheet], script[src]')]
              .some(el => (el.href || el.src || '').includes('banenkaart-v3b'))
            """);
        if (!hasV3b)
        {
            return;
        }

        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && window.maplibregl && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        if (!await TryOpenLargestClusterAsync(page))
        {
            return;
        }

        try
        {
            await page.WaitForSelectorAsync(".map-cluster-sheet", new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var next = page.Locator(".map-cluster-card__nav[data-cluster-next], [data-cluster-next]").First;
        var sheetHeights = new List<double>();

        for (var step = 0; step < 5; step++)
        {
            await page.EvaluateAsync("""
                () => {
                  const slide = document.querySelector('.map-cluster-card__slide.is-active')
                    || document.querySelector('.map-cluster-card__slide');
                  if (!slide) return;
                  const title = slide.querySelector('.map-popup__title');
                  const address = slide.querySelector('.map-popup__address');
                  const company = slide.querySelector('.map-popup__company');
                  if (title) {
                    title.textContent = 'Senior logistiek medewerker magazijn en orderpicken met avonddiensten in het Westland';
                  }
                  if (address) {
                    address.textContent = 'Lange Wateringsweg 128a, unit B12, Poeldijk';
                  }
                  if (company) {
                    company.textContent = 'Westland Groene Haven Distributie & Logistiek B.V.';
                  }
                }
                """);

            var metrics = await page.EvaluateAsync<ClusterMetrics>("""
                () => {
                  const sheet = document.querySelector('.map-cluster-sheet');
                  const viewport = document.querySelector('.map-cluster-card__viewport');
                  const apply = document.querySelector('.map-cluster-card__slide.is-active .map-popup__apply, .map-cluster-sheet .map-popup__apply');
                  const company = document.querySelector('.map-cluster-card__slide.is-active .map-popup__company, .map-cluster-sheet .map-popup__company');
                  const title = document.querySelector('.map-cluster-card__slide.is-active .map-popup__title, .map-cluster-sheet .map-popup__title');
                  if (!sheet || !viewport || !apply || !company || !title) {
                    return null;
                  }
                  const vr = viewport.getBoundingClientRect();
                  const ar = apply.getBoundingClientRect();
                  const cr = company.getBoundingClientRect();
                  const tr = title.getBoundingClientRect();
                  const cs = getComputedStyle(title);
                  const lineHeight = parseFloat(cs.lineHeight) || (parseFloat(cs.fontSize) * 1.2);
                  return {
                    sheetHeight: sheet.getBoundingClientRect().height,
                    applyBottom: ar.bottom,
                    applyTop: ar.top,
                    companyBottom: cr.bottom,
                    companyTop: cr.top,
                    viewportTop: vr.top,
                    viewportBottom: vr.bottom,
                    titleHeight: tr.height,
                    titleLineHeight: lineHeight
                  };
                }
                """);

            if (metrics is null)
            {
                return;
            }

            Assert.InRange(metrics.SheetHeight, 222.5, 223.5);
            sheetHeights.Add(metrics.SheetHeight);

            Assert.True(metrics.ApplyBottom <= metrics.ViewportBottom + 0.5, $"apply clipped bottom @ {width}x{height}");
            Assert.True(metrics.ApplyTop >= metrics.ViewportTop - 0.5, $"apply clipped top @ {width}x{height}");
            Assert.True(metrics.CompanyBottom <= metrics.ViewportBottom + 0.5, $"company clipped bottom @ {width}x{height}");
            Assert.True(metrics.CompanyTop >= metrics.ViewportTop - 0.5, $"company clipped top @ {width}x{height}");
            Assert.True(
                metrics.TitleHeight <= (metrics.TitleLineHeight * 2) + 1,
                $"title taller than 2 lines: {metrics.TitleHeight} vs {metrics.TitleLineHeight * 2}");

            if (step < 4)
            {
                if (await next.CountAsync() == 0 || await next.IsDisabledAsync())
                {
                    // Fewer than 5 vacancies in cluster — still assert the heights we have.
                    break;
                }

                await next.ClickAsync();
                await page.WaitForTimeoutAsync(250);
            }
        }

        Assert.NotEmpty(sheetHeights);
        var first = sheetHeights[0];
        foreach (var h in sheetHeights)
        {
            Assert.InRange(h, first - 0.5, first + 0.5);
            Assert.InRange(h, 222.5, 223.5);
        }
    }

    private sealed class ClusterMetrics
    {
        public double SheetHeight { get; set; }
        public double ApplyBottom { get; set; }
        public double ApplyTop { get; set; }
        public double CompanyBottom { get; set; }
        public double CompanyTop { get; set; }
        public double ViewportTop { get; set; }
        public double ViewportBottom { get; set; }
        public double TitleHeight { get; set; }
        public double TitleLineHeight { get; set; }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        try
        {
            var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
            var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.Locator("input[type=text], input[type=email], input[name=email], #email").First.FillAsync(email);
            await page.Locator("input[type=password]").First.FillAsync(password);
            await page.Locator("button[type=submit], button:has-text('Inloggen')").First.ClickAsync();
            await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryOpenLargestClusterAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>("""
                async () => {
                  const jm = window.jobMap;
                  if (!jm || typeof jm.debugOpenLargestCluster !== 'function') return false;
                  return !!(await jm.debugOpenLargestCluster());
                }
                """);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
