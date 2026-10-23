using Jobsy.Tests.E2e;
using Jobsy.Web.Banenkaart;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Live acceptatie banenkaart layout/map checks (guest + logged-in candidate).
/// Set <c>JOBSY_E2E_BASE_URL</c> to override; defaults to https://acceptatie.lobsy.nl.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartAccLivePlaywrightTests
{
    private const string DefaultAccUrl = "https://acceptatie.lobsy.nl";
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(390)]
    [InlineData(1280)]
    public async Task Banenkaart_amsterdam_auto_60_direct_url_shows_discover_list(int width)
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var touch = width < 900;
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = touch ? 844 : 720 },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        var url = baseUrl
            + "/banenkaart?maxMinutes=60&transport=Auto&originLat=52.3725&originLng=4.8921&originLabel=Amsterdam";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var emptyTravel = await page.Locator("[data-testid=kb-empty-travel]").CountAsync();
        var listRows = await page.Locator("[data-testid=kb-list-row]").CountAsync();
        Assert.True(listRows >= 1,
            "Amsterdam + Auto + 60 min should show at least one SSR list row (emptyTravel=" + emptyTravel + ").");
        if (listRows >= BanenkaartSsrData.ListPageSize)
        {
            var loadMore = await page.Locator("[data-testid=kb-load-more]").CountAsync();
            Assert.True(loadMore > 0, "Full discover page should expose Meer laden.");
        }
    }

    [Theory]
    [InlineData(1280, 720, false, false)]
    [InlineData(390, 844, true, false)]
    [InlineData(1280, 720, false, true)]
    [InlineData(390, 844, true, true)]
    public async Task Banenkaart_on_acc_has_map_height_pins_and_clean_console(
        int width,
        int height,
        bool touch,
        bool loggedIn)
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var consoleErrors = new List<string>();
        var cspViolations = new List<string>();
        var pinRequests = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }

            if (msg.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("script-src", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("script-src-attr", StringComparison.OrdinalIgnoreCase))
            {
                cspViolations.Add(msg.Text);
            }
        };
        page.Request += (_, req) =>
        {
            if (req.Url.Contains("/api/vacancies/pins", StringComparison.OrdinalIgnoreCase))
            {
                pinRequests.Add(req.Url);
            }
        };

        if (loggedIn)
        {
            var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
            var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
            if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
                && !await KbE2e.TryLoginAsync(page, baseUrl))
            {
                Assert.Fail("Candidate login failed on acceptatie.");
            }

            await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await page.WaitForTimeoutAsync(1500);
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var mapHeight = await page.Locator("#job-map").EvaluateAsync<double>(
            "el => el.getBoundingClientRect().height");
        Assert.True(mapHeight >= 300, $"#job-map height {mapHeight}px should be at least 300px on acc.");

        var visibleToolbars = await page.EvaluateAsync<int>(
            """
            () => {
              const vis = (el) => {
                if (!el) return false;
                const s = getComputedStyle(el);
                return s.display !== 'none' && s.visibility !== 'hidden' && el.getClientRects().length > 0;
              };
              let n = 0;
              if (vis(document.querySelector('[data-testid=kb-panel-head-mobile]'))) n++;
              if (vis(document.querySelector('[data-testid=kb-panel-head]'))) n++;
              return n;
            }
            """);
        Assert.Equal(1, visibleToolbars);

        if (loggedIn)
        {
            var headerNavLinks = await page.Locator("[data-testid=candidate-header-nav] .candidate-header-nav__link").CountAsync();
            Assert.Equal(0, headerNavLinks);

            var bottomNavLinks = await page.Locator("[data-testid=candidate-bottom-nav-ssr] a.bottom-nav__item").CountAsync();
            Assert.True(bottomNavLinks >= 3,
                "Logged-in banenkaart should use the shared bottom nav (same as paspoort), not header tabs.");

            var hasAuthChrome = await page.EvaluateAsync<bool>(
                """
                () => !Array.from(document.querySelectorAll('link[rel=stylesheet]'))
                  .some(l => (l.href || '').indexOf('app.min.css') !== -1)
                """);
            Assert.True(hasAuthChrome, "Logged-in NoBlazor banenkaart should not load full app.min.css.");

            var authChromeCss = await page.EvaluateAsync<bool>(
                """
                () => Array.from(document.querySelectorAll('link[rel=stylesheet]'))
                  .some(l => (l.href || '').indexOf('banenkaart-auth-chrome.css') !== -1)
                """);
            Assert.True(authChromeCss, "Logged-in banenkaart should load banenkaart-auth-chrome.css.");
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && root.dataset.banenkaartMapReady === '1') return true;
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
              const src = map.getSource && map.getSource('jobsy-pins');
              if (src && src._data && src._data.features && src._data.features.length > 0) return true;
              const canvas = document.querySelector('#job-map .maplibregl-canvas');
              return !!(canvas && canvas.width > 0 && canvas.height > 0);
            }
            """,
            null,
            new() { Timeout = 120_000 });

        Assert.True(pinRequests.Count == 0,
            "Catalog mode should not call /api/vacancies/pins. Saw: " + string.Join("; ", pinRequests));

        if (loggedIn && width >= 900)
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                  if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
                  const layers = ['jobsy-pins-clusters', 'jobsy-pins-unclustered', 'jobsy-pins-clusters-hit'];
                  const feats = map.queryRenderedFeatures({ layers: layers.filter(id => map.getLayer(id)) });
                  return !!(feats && feats.length > 0);
                }
                """,
                null,
                new() { Timeout = 120_000 });
            var renderedPinFeatures = await page.EvaluateAsync<int>(
                """
                () => {
                  const map = window.jobMap.__testGetMap();
                  const layers = ['jobsy-pins-clusters', 'jobsy-pins-unclustered', 'jobsy-pins-clusters-hit'];
                  return (map.queryRenderedFeatures({ layers: layers.filter(id => map.getLayer(id)) }) || []).length;
                }
                """);
            Assert.True(renderedPinFeatures > 0,
                "Desktop logged-in map should render at least one pin/cluster feature.");

            await ApplyTravelFiltersAsync(page, 60, "Auto");
            await page.WaitForLoadStateAsync(LoadState.Load);
            var filtersOpen = await page.EvaluateAsync<bool>(
                "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.classList.contains('filters-open') === true");
            Assert.False(filtersOpen, "Filter drawer should close after Toon banen apply.");

            var emptyTravel = await page.Locator("[data-testid=kb-empty-travel]").CountAsync();
            Assert.Equal(0, emptyTravel);

            var listRows = await page.Locator("[data-testid=kb-list-row]").CountAsync();
            Assert.True(listRows > 0, "60 min Auto should show SSR discover list rows on desktop.");
            var loadMore = await page.Locator("[data-testid=kb-load-more]").CountAsync();
            Assert.True(loadMore > 0, "Discover list should include Meer laden when more than one page.");

            var transportVal = await page.Locator("#kb-filter-transport").InputValueAsync();
            var maxMinutes = await page.Locator("[data-testid=kb-sheet-travel-slider]").InputValueAsync();
            Assert.Equal("Auto", transportVal);
            Assert.Equal("60", maxMinutes);

            await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.WaitForTimeoutAsync(800);

            transportVal = await page.Locator("#kb-filter-transport").InputValueAsync();
            maxMinutes = await page.Locator("[data-testid=kb-sheet-travel-slider]").InputValueAsync();
            Assert.Equal("Auto", transportVal);
            Assert.Equal("60", maxMinutes);

            await OpenFilterDrawerAsync(page);
            var footerVisible = await page.EvaluateAsync<bool>(
                """
                () => {
                  const btn = document.querySelector('[data-testid=kb-filter-apply]');
                  if (!btn) return false;
                  const r = btn.getBoundingClientRect();
                  const sheet = document.querySelector('[data-testid=kb-filter-sheet]');
                  const sr = sheet ? sheet.getBoundingClientRect() : null;
                  if (!sr) return false;
                  return r.top >= sr.top && r.bottom <= sr.bottom + 1;
                }
                """);
            Assert.True(footerVisible, "Filter drawer apply button should stay inside the visible drawer (sticky footer).");
        }

        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(cspViolations.Count == 0,
            "CSP violations: " + string.Join("; ", cspViolations));
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
    }

    [Fact]
    public async Task Desktop_logged_in_banenkaart_ok_after_paspoort_and_hard_reload()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var consoleErrors = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
            && !await KbE2e.TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed on acceptatie.");
        }

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(800);

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.EvaluateAsync(
            """
            () => {
              sessionStorage.setItem('jobsy.bk.ui.v1', JSON.stringify({ filtersOpen: true, showMap: false }));
            }
            """);

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForTimeoutAsync(1200);

        var filtersOpen = await page.EvaluateAsync<bool>(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.classList.contains('filters-open') === true");
        Assert.False(filtersOpen, "Filter drawer must be closed on load after hard reload.");

        var mapHeight = await page.Locator("#job-map canvas, #job-map .maplibregl-canvas").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        Assert.True(mapHeight > 300, "Map canvas height should exceed 300px (got " + mapHeight + ").");

        await WaitForBanenkaartMapReadyAsync(page);
        var basemapFeatures = await CountRenderedBasemapFeaturesAsync(page);
        Assert.True(basemapFeatures >= 5,
            "Desktop basemap should render vector features (got " + basemapFeatures + ").");

        var listRowHeight = await page.Locator("[data-testid=kb-list-row]").First
            .EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        Assert.True(listRowHeight > 0 && listRowHeight < 200,
            "Compact list row height should be under 200px (got " + listRowHeight + ").");

        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
    }

    [Theory]
    [InlineData(390, 844, true)]
    [InlineData(1280, 720, false)]
    public async Task Banenkaart_polish_footer_minus_and_clean_console_on_acc(int width, int height, bool touch)
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var consoleErrors = new List<string>();
        var font404 = 0;
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        page.Response += (_, resp) =>
        {
            if (resp.Status == 404 && resp.Url.Contains("/fonts/", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref font404);
            }
        };

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitForBanenkaartMapReadyAsync(page);

        if (touch)
        {
            var footerOk = await page.EvaluateAsync<bool>(
                """
                () => {
                  const map = document.querySelector('#job-map');
                  const footer = document.querySelector('.app-footer');
                  if (!map) return false;
                  const mr = map.getBoundingClientRect();
                  if (!footer) return true;
                  const fs = getComputedStyle(footer);
                  if (fs.display === 'none' || fs.visibility === 'hidden' || footer.getClientRects().length === 0) {
                    return true;
                  }
                  const fr = footer.getBoundingClientRect();
                  if (fr.height < 1) return true;
                  return fr.top >= mr.bottom - 2;
                }
                """);
            Assert.True(footerOk, "Mobile site footer must not overlap the map on acceptatie.");
        }
        else
        {
            var minusVisible = await page.EvaluateAsync<bool>(
                """
                () => {
                  const el = document.querySelector('.job-map .maplibregl-ctrl-zoom-out');
                  if (!el) return false;
                  const r = el.getBoundingClientRect();
                  const s = getComputedStyle(el);
                  return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none';
                }
                """);
            Assert.True(minusVisible, "Desktop map must show zoom-out (−) on acceptatie.");
        }

        Assert.Equal(0, font404);
        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
    }

    [Fact]
    public async Task Banenkaart_map_survives_viewport_resize_on_acc()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitForBanenkaartMapReadyAsync(page);
        await page.EvaluateAsync(
            """
            () => {
              sessionStorage.setItem('jobsy.bk.ui.v1', JSON.stringify({ filtersOpen: false, showMap: false }));
            }
            """);
        await page.SetViewportSizeAsync(1280, 720);
        await page.WaitForTimeoutAsync(500);

        var mapOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && !root.classList.contains('show-map')) return false;
              const canvas = document.querySelector('#job-map canvas, #job-map .maplibregl-canvas');
              if (!canvas) return false;
              return canvas.getBoundingClientRect().height > 300;
            }
            """);
        Assert.True(mapOk, "Map should remain visible after mobile→desktop resize on acceptatie.");
    }

    [Fact]
    public async Task Desktop_cluster_tap_zooms_in_on_acc()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            HasTouch = false,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitForBanenkaartMapReadyAsync(page);
        await page.EvaluateAsync(
            """
            async () => {
              await new Promise(function (resolve) {
                if (window.jobsyBanenkaartCatalog && window.jobsyBanenkaartCatalog.isReady()) {
                  resolve();
                  return;
                }
                document.addEventListener('jobsy-bk-catalog-ready', resolve, { once: true });
                setTimeout(resolve, 12000);
              });
              const map = window.jobMap.__testGetMap();
              if (window.jobMap.__testFitAllPins) {
                window.jobMap.__testFitAllPins();
              }
              map.setZoom(6.5);
              await new Promise(function (r) { map.once('idle', r); });
            }
            """);

        var zoomBefore = await page.EvaluateAsync<double>(
            "() => window.jobMap.__testGetMap().getZoom()");
        var tapped = await page.EvaluateAsync<bool>(
            "() => !!(window.jobMap.__testTapLargestCluster && window.jobMap.__testTapLargestCluster())");
        Assert.True(tapped, "Expected a visible cluster to tap on acceptatie.");
        await page.WaitForTimeoutAsync(900);
        var zoomAfter = await page.EvaluateAsync<double>(
            "() => window.jobMap.__testGetMap().getZoom()");
        Assert.True(zoomAfter > zoomBefore + 0.12,
            "Cluster tap should zoom in on acc (before=" + zoomBefore + ", after=" + zoomAfter + ").");
    }

    [Fact]
    public async Task Desktop_banenkaart_basemap_stays_visible_after_pin_zoom_and_minus_control()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var page = await context.NewPageAsync();
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
            && !await KbE2e.TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed on acceptatie.");
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitForBanenkaartMapReadyAsync(page);

        var basemapOnLoad = await CountRenderedBasemapFeaturesAsync(page);
        Assert.True(basemapOnLoad >= 5,
            "Initial desktop view should render basemap vector features (got " + basemapOnLoad + ").");

        var maxBasemapZoom = await page.EvaluateAsync<double>(
            """
            () => {
              const map = window.jobMap.__testGetMap();
              return (window.jobsyMapLibre && window.jobsyMapLibre.BASEMAP_MAX_ZOOM) || map.getMaxZoom();
            }
            """);

        await page.EvaluateAsync(
            """
            async () => {
              const map = window.jobMap.__testGetMap();
              if (!map) return;
              const maxZ = (window.jobsyMapLibre && window.jobsyMapLibre.BASEMAP_MAX_ZOOM) || 14;
              map.easeTo({ zoom: maxZ, duration: 0 });
              await new Promise(r => {
                if (!map.isMoving()) { r(); return; }
                map.once('moveend', r);
              });
              const canvas = map.getCanvas();
              const w = canvas.clientWidth || canvas.width || 1280;
              const h = canvas.clientHeight || canvas.height || 720;
              for (let y = 48; y < h - 48; y += 28) {
                for (let x = 32; x < w - 32; x += 28) {
                  const feats = map.queryRenderedFeatures(
                    new maplibregl.Point(x, y),
                    { layers: ['jobsy-pins-unclustered'] });
                  if (feats && feats.length) {
                    const rect = canvas.getBoundingClientRect();
                    const ev = new MouseEvent('click', {
                      bubbles: true,
                      clientX: rect.left + x,
                      clientY: rect.top + y,
                      view: window
                    });
                    canvas.dispatchEvent(ev);
                    return;
                  }
                }
              }
            }
            """);
        await page.WaitForTimeoutAsync(1200);

        var zoomAtPin = await page.EvaluateAsync<double>(
            "() => window.jobMap.__testGetMap().getZoom()");
        Assert.True(zoomAtPin <= maxBasemapZoom + 0.05,
            "Zoom at single pin should not exceed basemap max (" + zoomAtPin + " > " + maxBasemapZoom + ").");

        var basemapAtPin = await CountRenderedBasemapFeaturesAsync(page);
        Assert.True(basemapAtPin >= 5,
            "Basemap should stay visible after zooming to a single pin (got " + basemapAtPin + ").");

        await page.EvaluateAsync(
            """
            () => {
              const map = window.jobMap.__testGetMap();
              if (!map) return;
              map.zoomOut({ duration: 0 });
              map.zoomOut({ duration: 0 });
            }
            """);
        await page.WaitForTimeoutAsync(600);

        var basemapAfterMinus = await CountRenderedBasemapFeaturesAsync(page);
        Assert.True(basemapAfterMinus >= 5,
            "Basemap should stay visible after zoom-out control (got " + basemapAfterMinus + ").");

        var zoomAfterMinus = await page.EvaluateAsync<double>(
            "() => window.jobMap.__testGetMap().getZoom()");
        Assert.True(zoomAfterMinus >= 6.9, "Zoom-out should not leave map below min zoom (got " + zoomAfterMinus + ").");
    }

    [Fact]
    public async Task Onboarding_step_10_has_no_script_src_attr_violation_on_acc()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var cspViolations = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Text.Contains("script-src-attr", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("inline event handler", StringComparison.OrdinalIgnoreCase))
            {
                cspViolations.Add(msg.Text);
            }
        };

        await page.GotoAsync(baseUrl + "/candidate/start?step=10", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(1500);

        Assert.True(cspViolations.Count == 0,
            "Onboarding step 10 CSP script-src-attr: " + string.Join("; ", cspViolations));
    }

    [Fact]
    public async Task Mobile_logged_in_banenkaart_stays_interactive_after_sheet_history_and_background()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var page = await context.NewPageAsync();
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
            && !await KbE2e.TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed on acceptatie.");
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("[data-banenkaart-ssr=\"1\"]", new() { Timeout = 60_000 });

        await OpenFilterDrawerAsync(page);
        await page.Locator("[data-testid=kb-filter-close]").ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && !root.classList.contains('filters-open');
            }
            """);

        await page.Locator("[data-testid=kb-view-lijst]").ClickAsync();
        await page.Locator("[data-testid=kb-view-kaart]").ClickAsync();
        await page.EvaluateAsync("() => window.scrollTo(0, 120)");

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 60_000 });

        await page.EvaluateAsync(
            """
            () => {
              document.dispatchEvent(new Event('visibilitychange'));
              Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' });
              document.dispatchEvent(new Event('visibilitychange'));
            }
            """);

        var interactive = await page.EvaluateAsync<bool>(
            """
            () => {
              const btn = document.querySelector('[data-testid=kb-filters-button]');
              if (!btn || btn.disabled) return false;
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && root.classList.contains('filters-open')) return false;
              const body = document.body;
              const overflow = getComputedStyle(body).overflow;
              if (overflow === 'hidden' && !document.querySelector('.jobsy-discovery.filters-open')) return false;
              return true;
            }
            """);
        Assert.True(interactive, "Banenkaart should stay interactive with no stray body scroll-lock after mobile PWA-style resume.");
    }

    private static async Task WaitForBanenkaartMapReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && root.dataset.banenkaartMapReady === '1') return true;
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
              const canvas = document.querySelector('#job-map .maplibregl-canvas');
              return !!(canvas && canvas.width > 0 && canvas.height > 0);
            }
            """,
            null,
            new() { Timeout = 120_000 });
        await page.WaitForTimeoutAsync(800);
    }

    private static async Task<int> CountRenderedBasemapFeaturesAsync(IPage page)
    {
        return await page.EvaluateAsync<int>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return -1;
              const style = map.getStyle();
              if (!style || !Array.isArray(style.layers)) return -1;
              const layerIds = style.layers
                .filter(l => l && l.id && !l.id.startsWith('jobsy-')
                  && l.type !== 'background' && l.type !== 'sky')
                .map(l => l.id)
                .filter(id => {
                  try { return !!map.getLayer(id); } catch (e) { return false; }
                });
              let feats = [];
              try {
                feats = map.queryRenderedFeatures({ layers: layerIds.slice(0, 48) }) || [];
              } catch (e) {
                return -2;
              }
              return feats.length;
            }
            """);
    }

    private static async Task OpenFilterDrawerAsync(IPage page)
    {
        var btn = page.Locator("[data-testid=kb-filters-button]").First;
        await btn.ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('filters-open');
            }
            """);
    }

    private static async Task ApplyTravelFiltersAsync(IPage page, int minutes, string transport)
    {
        await OpenFilterDrawerAsync(page);
        await page.Locator($"[data-testid=kb-sheet-travel-{minutes}]").ClickAsync();
        await page.Locator($"[data-testid=kb-sheet-transport-{transport}]").ClickAsync();
        await page.Locator("[data-testid=kb-filter-apply]").ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("/banenkaart"), new() { Timeout = 60_000 });
    }

    private static string ResolveBaseUrl()
    {
        var fromEnv = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(fromEnv) ? DefaultAccUrl : fromEnv;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var res = await http.GetAsync(baseUrl + "/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
