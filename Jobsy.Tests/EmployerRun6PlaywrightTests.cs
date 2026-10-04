using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Static layout checks for employer run 6. No live site: the CSS under test is inlined.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EmployerRun6PlaywrightTests
{
    [Fact]
    public async Task Passport_stats_are_at_least_120px_at_1366()
    {
        await using var page = await OpenAsync(1366, 900, PassportHtml());
        var widths = await page.EvaluateAsync<double[]>(
            """
            () => [...document.querySelectorAll('.passport-stat')].map(el => el.getBoundingClientRect().width)
            """);
        Assert.Equal(4, widths.Length);
        Assert.All(widths, width => Assert.True(width >= 120, $"passport-stat is {width}px"));
    }

    [Fact]
    public async Task Map_controls_are_not_under_the_coach_at_390()
    {
        await using var page = await OpenAsync(390, 844, MapHtml());
        var covered = await page.EvaluateAsync<string[]>(
            """
            () => {
              const coach = document.querySelector('#lobsy-coach-btn');
              const c = coach.getBoundingClientRect();
              const nodes = document.querySelectorAll('.maplibregl-ctrl button, .job-map-style-switch__btn, .job-map-locate__btn');
              const hits = [];
              for (const node of nodes) {
                const r = node.getBoundingClientRect();
                if (r.width < 8 || r.height < 8) continue;
                const cx = r.left + r.width / 2;
                const cy = r.top + r.height / 2;
                const centreUnder = cx >= c.left && cx <= c.right && cy >= c.top && cy <= c.bottom;
                const intersects = c.left < r.right && c.right > r.left && c.top < r.bottom && c.bottom > r.top;
                if (centreUnder || intersects) {
                  hits.push((node.getAttribute('aria-label') || node.className) + ' ' + Math.round(r.x) + ',' + Math.round(r.y) + ' ' + Math.round(r.width) + 'x' + Math.round(r.height));
                }
              }
              return hits;
            }
            """);
        Assert.Empty(covered);
    }

    [Fact]
    public async Task Map_controls_do_not_cover_each_other_at_390()
    {
        await using var page = await OpenAsync(390, 844, MapHtml());
        var report = await page.EvaluateAsync<JsonElement>(
            """
            () => {
              const sel = '.maplibregl-ctrl-zoom-in, .maplibregl-ctrl-zoom-out, .job-map-style-switch__btn, .job-map-locate__btn, .maplibregl-ctrl-attrib-button';
              const coach = document.querySelector('#lobsy-coach-btn').getBoundingClientRect();
              const nodes = [...document.querySelectorAll(sel)].map(node => {
                const r = node.getBoundingClientRect();
                return {
                  name: node.getAttribute('aria-label') || node.className,
                  x: r.x, y: r.y, w: r.width, h: r.height,
                  right: r.right, bottom: r.bottom
                };
              }).filter(r => r.w >= 8 && r.h >= 8);
              const hits = [];
              for (let i = 0; i < nodes.length; i++) {
                const a = nodes[i];
                if (coach.left < a.right && coach.right > a.x && coach.top < a.bottom && coach.bottom > a.y) {
                  hits.push('coach ' + a.name);
                }
                for (let j = i + 1; j < nodes.length; j++) {
                  const b = nodes[j];
                  if (a.x < b.right && a.right > b.x && a.y < b.bottom && a.bottom > b.y) {
                    hits.push(a.name + ' x ' + b.name);
                  }
                }
              }
              const zoom = nodes.find(r => r.name === 'Zoom in');
              const style = nodes.find(r => r.name === '3D-kaart');
              const attrib = nodes.find(r => r.name === 'Bronvermelding');
              return {
                hits,
                align: zoom && style ? Math.abs(zoom.x - style.x) : 999,
                attribX: attrib ? attrib.x : -1
              };
            }
            """);
        var hits = report.GetProperty("hits").EnumerateArray().Select(h => h.GetString()).ToArray();
        Assert.Empty(hits);
        Assert.True(report.GetProperty("align").GetDouble() <= 1, "3D is not in line with zoom.");
        Assert.True(report.GetProperty("attribX").GetDouble() < 40, "Attribution is still on the zoom stack.");
    }

    [Fact]
    public async Task Employer_install_banner_later_is_clear_of_the_assistant_tab()
    {
        await using var page = await OpenAsync(390, 844, BannerHtml());
        var overlap = await page.EvaluateAsync<bool>(
            """
            () => {
              const later = document.querySelector('[data-testid=pwa-later]');
              const tab = document.querySelector('.lobsy-assistant-tab__btn');
              const a = later.getBoundingClientRect();
              const b = tab.getBoundingClientRect();
              return a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
            }
            """);
        Assert.False(overlap, "Later is covered by the Assistent tab.");
        var later = await page.Locator("[data-testid=pwa-later]").BoundingBoxAsync();
        Assert.NotNull(later);
        Assert.True(later!.Width >= 24 && later.Height >= 24);
    }

    private static async Task<IPage> OpenAsync(int width, int height, string html)
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            HasTouch = width < 768,
            IsMobile = width < 768
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(html);
        return page;
    }

    private static string PassportHtml()
    {
        var css = Read("Jobsy.Web/wwwroot/css/app.css") + Read("Jobsy.Web/wwwroot/css/features/mijn-paspoort.css");
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
               + "<style>" + css + "</style></head><body>"
               + "<div class=\"panel-page\"><div class=\"passport-layout\">"
               + "<aside class=\"passport-layout__card passport-layout__card--desktop\"><div class=\"passport-card\">Paspoort</div></aside>"
               + "<div class=\"passport-layout__main\"><section class=\"passport-overview\">"
               + "<p class=\"profile-address__hint\">We bewaren je antwoorden alleen voor jou.</p>"
               + "<div class=\"passport-overview__ring-block\"><div class=\"passport-dna-ring\"></div>"
               + "<div><h2 class=\"passport-overview__title\">Dit ben jij</h2>"
               + "<p class=\"passport-overview__stage\">Je bent net begonnen.</p></div></div>"
               + "<div class=\"passport-overview__stats\">"
               + Stat("Jouw sterkste stap") + Stat("Wat je kunt") + Stat("Waar je past") + Stat("Wat je belangrijk vindt")
               + "</div></section></div></div></div></body></html>";
    }

    private static string Stat(string label)
        => "<article class=\"passport-stat\"><p class=\"passport-stat__label\">" + label
           + "</p><p class=\"passport-stat__value\">Nog niet</p></article>";

    private static string MapHtml()
    {
        var css = Read("Jobsy.Web/wwwroot/lib/maplibre/maplibre-gl.css")
                  + Read("Jobsy.Web/wwwroot/css/app.css")
                  + Read("Jobsy.Web/wwwroot/css/features/banenkaart.css");
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><style>" + css
               + "</style></head><body>"
               + "<div class=\"app-shell has-bottom-nav\" style=\"--bottom-nav-h:64px\">"
               + "<div class=\"job-map maplibregl-map\" style=\"position:fixed;inset:0;\">"
               + "<div class=\"maplibregl-control-container\"><div class=\"maplibregl-ctrl-bottom-right\">"
               + "<div class=\"maplibregl-ctrl maplibregl-ctrl-attrib maplibregl-compact\">"
               + "<button type=\"button\" class=\"maplibregl-ctrl-attrib-button\" aria-label=\"Bronvermelding\"></button>"
               + "</div>"
               + "<div class=\"maplibregl-ctrl maplibregl-ctrl-group\">"
               + "<button type=\"button\" class=\"maplibregl-ctrl-zoom-in\" aria-label=\"Zoom in\"></button>"
               + "<button type=\"button\" class=\"maplibregl-ctrl-zoom-out\" aria-label=\"Zoom uit\"></button>"
               + "</div>"
               + "<div class=\"maplibregl-ctrl maplibregl-ctrl-group job-map-style-switch\">"
               + "<button type=\"button\" class=\"job-map-style-switch__btn\" aria-label=\"3D-kaart\">3D</button>"
               + "</div></div></div>"
               + "<div class=\"job-map-locate\"><button type=\"button\" class=\"job-map-locate__btn\" aria-label=\"Mijn plek\"></button></div>"
               + "</div>"
               + "<div class=\"lobsy-coach-dock\" data-lobsy-coach>"
               + "<button type=\"button\" id=\"lobsy-coach-btn\" class=\"lobsy-coach-dock__btn\" aria-label=\"Coach\"></button>"
               + "</div></div></body></html>";
    }

    private static string BannerHtml()
    {
        var css = Read("Jobsy.Web/wwwroot/css/app.css") + Read("Jobsy.Web/wwwroot/css/features/werkgever.css");
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><style>" + css
               + "</style></head><body>"
               + "<div class=\"wg-shell wg-has-bottom-nav\">"
               + "<main class=\"wg-main\" style=\"height:640px\"></main>"
               + "<div class=\"pwa-install-banner\" role=\"status\">"
               + "<p class=\"pwa-install-banner__text\">Zet Lobsy op je startscherm.</p>"
               + "<div class=\"pwa-install-banner__actions\">"
               + "<button type=\"button\" class=\"btn-compact\" data-testid=\"pwa-later\">Later</button>"
               + "</div></div>"
               + "<div class=\"lobsy-assistant-tab lobsy-assistant-tab--edge has-bottom-nav\">"
               + "<button type=\"button\" class=\"lobsy-assistant-tab__btn\">Assistent</button>"
               + "</div></div></body></html>";
    }

    private static string Read(string relative)
        => File.ReadAllText(Path.Combine(FindRoot(), relative));

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
