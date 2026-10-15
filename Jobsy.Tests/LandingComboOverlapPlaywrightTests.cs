using System.Text;
using System.Text.Json;
using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Ensures the calm combo homepage has no overlapping non-nested blocks and no clipped text
/// at standard viewports (1440 … 360).
/// </summary>
[Collection("PlaywrightSmoke")]
public class LandingComboOverlapPlaywrightTests
{
    private static readonly (int W, int H)[] Viewports =
    [
        (1440, 900),
        (1280, 900),
        (1024, 900),
        (768, 900),
        (390, 844),
        (360, 780),
    ];

    [Fact]
    public async Task Combo_landing_has_no_element_overlap_or_text_overflow_at_standard_viewports()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var factory = new LandingComboOverlapWebFactory();
        var baseUrl = factory.EnsureServerAddress();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var failures = new List<string>();
        foreach (var (w, h) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                Locale = "nl-NL",
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            await page.GotoAsync($"{baseUrl}/?lang=nl", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
            await page.WaitForSelectorAsync(".pub-landing--calm", new() { Timeout = 60_000 });

            var json = await page.EvaluateAsync<string>(OverlapScript);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var err))
            {
                failures.Add($"{w}x{h}: {err.GetString()}");
                continue;
            }

            if (root.TryGetProperty("overlaps", out var overlaps) && overlaps.GetArrayLength() > 0)
            {
                failures.Add($"{w}x{h} overlaps: {overlaps}");
            }

            if (root.TryGetProperty("overflows", out var overflows) && overflows.GetArrayLength() > 0)
            {
                failures.Add($"{w}x{h} text overflow: {overflows}");
            }

            if (root.TryGetProperty("svgOverlaps", out var svgOverlaps) && svgOverlaps.GetArrayLength() > 0)
            {
                failures.Add($"{w}x{h} radar svg: {svgOverlaps}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private const string OverlapScript = """
        () => {
          const root = document.querySelector('.pub-landing--calm');
          if (!root) return JSON.stringify({ error: 'missing .pub-landing--calm' });

          const sel = [
            'a', 'button', 'summary',
            'h1', 'h2', 'h3', 'p',
            '.pub-landing__lift',
            '.pub-pill', '.pub-pill-sample',
            '.pub-landing__passport-card',
            '.pub-landing__passport-footer',
            '.pub-landing__flow',
            '.pub-landing__stat',
            '.pub-landing__get-card',
            '.pub-landing__journey-step',
            '.pub-landing__map-frame',
            '.pub-landing__map-legend li',
            '.pub-landing__map-chart-foot',
            '.pub-landing__emp-card',
            '.pub-landing__faq-card',
            '.pub-landing__map-toolbar',
            '.pub-landing__map-seg'
          ].join(',');

          const gap = 2;
          const minArea = 80;
          const els = [...root.querySelectorAll(sel)].filter(el => {
            if (!(el instanceof HTMLElement)) return false;
            const st = getComputedStyle(el);
            if (st.display === 'none' || st.visibility === 'hidden' || Number(st.opacity) === 0) return false;
            const r = el.getBoundingClientRect();
            if (r.width * r.height < minArea) return false;
            return true;
          });

          const contains = (a, b) => a === b || a.contains(b) || b.contains(a);

          const sameFaqItem = (a, b) => {
            const da = a.closest('.pub-landing__fq');
            const db = b.closest('.pub-landing__fq');
            return da && da === db;
          };

          const overlaps = [];
          for (let i = 0; i < els.length; i++) {
            for (let j = i + 1; j < els.length; j++) {
              const a = els[i], b = els[j];
              if (contains(a, b)) continue;
              if (sameFaqItem(a, b)) continue;
              const ra = a.getBoundingClientRect();
              const rb = b.getBoundingClientRect();
              const hit = !(ra.right <= rb.left + gap || rb.right <= ra.left + gap || ra.bottom <= rb.top + gap || rb.bottom <= ra.top + gap);
              if (hit) {
                overlaps.push({
                  a: (a.className || a.tagName) + ' | ' + (a.textContent || '').trim().slice(0, 40),
                  b: (b.className || b.tagName) + ' | ' + (b.textContent || '').trim().slice(0, 40)
                });
                if (overlaps.length >= 8) break;
              }
            }
            if (overlaps.length >= 8) break;
          }

          const overflowSel = 'h1,h2,h3,p,small,b,span,a,summary,li,.pub-landing__get-tag';
          const overflows = [];
          const isClipped = (el) => {
            const st = getComputedStyle(el);
            if (el.scrollWidth > el.clientWidth + 1) return true;
            const clamp = st.webkitLineClamp || st.lineClamp;
            const hasClamp = clamp && clamp !== 'none' && Number(clamp) > 0;
            const clipY = st.overflowY === 'hidden' || st.overflowY === 'clip'
              || st.textOverflow === 'ellipsis';
            if (hasClamp || clipY) {
              if (el.scrollHeight > el.clientHeight + 2) return true;
            }
            return false;
          };

          for (const el of root.querySelectorAll(overflowSel)) {
            if (!(el instanceof HTMLElement)) continue;
            const st = getComputedStyle(el);
            if (st.display === 'none' || st.visibility === 'hidden') continue;
            const t = (el.textContent || '').trim();
            if (t.length < 2) continue;
            if (isClipped(el)) {
              overflows.push((el.className || el.tagName) + ': ' + t.slice(0, 50));
              if (overflows.length >= 8) break;
            }
          }

          const svgGap = 8;
          const svgOverlaps = [];
          const radar = root.querySelector('.pub-landing__radar-svg');
          if (radar) {
            const outer = radar.querySelector('.pub-landing__radar-outer');
            const texts = [...radar.querySelectorAll('text')].filter(t => t.getBoundingClientRect().width > 0);
            const outerR = outer?.getBoundingClientRect();
            const radCol = radar.closest('.pub-landing__passport-rad')?.getBoundingClientRect();
            const hit = (ra, rb) => !(ra.right <= rb.left + svgGap || rb.right <= ra.left + svgGap || ra.bottom <= rb.top + svgGap || rb.bottom <= ra.top + svgGap);
            for (let i = 0; i < texts.length; i++) {
              for (let j = i + 1; j < texts.length; j++) {
                const ri = texts[i].getBoundingClientRect();
                const rj = texts[j].getBoundingClientRect();
                if (hit(ri, rj)) {
                  svgOverlaps.push({ a: texts[i].textContent?.trim(), b: texts[j].textContent?.trim() });
                  if (svgOverlaps.length >= 6) break;
                }
              }
              if (svgOverlaps.length >= 6) break;
            }
            if (outerR) {
              for (const t of texts) {
                const tr = t.getBoundingClientRect();
                if (hit(tr, outerR)) {
                  svgOverlaps.push({ a: t.textContent?.trim(), b: 'radar-outer-ring' });
                  if (svgOverlaps.length >= 6) break;
                }
              }
            }
            if (radCol) {
              for (const t of texts) {
                const tr = t.getBoundingClientRect();
                if (tr.left < radCol.left + svgGap - 1 || tr.right > radCol.right - svgGap + 1
                    || tr.top < radCol.top + svgGap - 1 || tr.bottom > radCol.bottom - svgGap + 1) {
                  svgOverlaps.push({ a: t.textContent?.trim(), b: 'radar-column-edge' });
                  if (svgOverlaps.length >= 6) break;
                }
              }
            }
          }

          return JSON.stringify({ overlaps, overflows, svgOverlaps });
        }
        """;

    private sealed class LandingComboOverlapWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;
        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["Landing:ForceVariant"] = "on",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmployersSwitch>();
                services.AddSingleton<IEmployersSwitch>(new AlwaysOnEmployersSwitch());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler()) { BaseAddress = new Uri("http://api.test/") }));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var testHost = builder.Build();
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
            });
            _kestrel = builder.Build();
            _kestrel.Start();
            var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
            testHost.Start();
            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class StubFlags : IFeatureFlags
        {
            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: true));

            public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(false);

            public void Invalidate()
            {
            }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                if (path.Contains("feature", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("platform", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"employersEnabled":false,"candidatePassportEnabled":true}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("landing-stats", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"jobsWithin30Min":38,"medianCommuteMin":14}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("landing-price", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"priceEuro":9.95}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("site/legal", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"tradeName":"Jobsy","kvk":""}""", Encoding.UTF8, "application/json")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"code":"not_found"}""", Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
