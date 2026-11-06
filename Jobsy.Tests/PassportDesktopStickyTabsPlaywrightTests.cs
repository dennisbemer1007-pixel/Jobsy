using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public class PassportDesktopStickyTabsPlaywrightTests
{
    [Fact]
    public async Task Desktop_1280_sticky_tabs_flush_under_header_with_opaque_band()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildPassportHtml(), new() { WaitUntil = WaitUntilState.Load });

        await page.EvaluateAsync("""
            () => {
              window.scrollTo(0, 420);
            }
            """);
        await page.WaitForTimeoutAsync(250);

        var json = await page.EvaluateAsync<JsonElement>("""
            () => {
              const shell = document.querySelector('.passport-tabs-shell');
              const header = document.querySelector('.app-header');
              if (!shell || !header) return { ok: false, reason: 'missing' };
              const shellBox = shell.getBoundingClientRect();
              const headerBox = header.getBoundingClientRect();
              const gap = shellBox.top - headerBox.bottom;
              const cs = getComputedStyle(shell);
              const bg = cs.backgroundColor;
              const beforeBg = getComputedStyle(shell, '::before').backgroundColor;
              const opaque = (bg && bg !== 'rgba(0, 0, 0, 0)' && bg !== 'transparent')
                || (beforeBg && beforeBg !== 'rgba(0, 0, 0, 0)' && beforeBg !== 'transparent');
              let contentAbove = false;
              const probeY = headerBox.bottom + Math.max(gap * 0.5, 2);
              if (probeY < shellBox.top - 1) {
                const hit = document.elementFromPoint(200, probeY);
                if (hit && !shell.contains(hit)) {
                  const hb = hit.getBoundingClientRect();
                  if (hb.height > 6 && hb.width > 6) contentAbove = true;
                }
              }
              return {
                ok: Math.abs(gap) < 2 && opaque && !contentAbove,
                gap,
                opaque,
                contentAbove,
                stickyTop: parseFloat(cs.top) || 0
              };
            }
            """);

        Assert.True(json.GetProperty("ok").GetBoolean(), json.ToString());
    }

    private static string BuildPassportHtml()
    {
        var root = FindRepoRoot();
        var appCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var passportCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        var kompasCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kompas-illustrated.css"));
        return $$"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8">
            <style>
            :root { --app-header-height: 68px; --bg: #f5f2ee; --surface: #fffcfa; --border: #ddd5cc; --brand: #0f2d5c; --z-sticky: 20; --space-2: 0.5rem; --space-3: 0.75rem; }
            body { margin: 0; background: var(--bg); font-family: system-ui, sans-serif; }
            {{appCss}}
            {{passportCss}}
            {{kompasCss}}
            </style></head><body>
            <header class="app-header" style="position:sticky;top:0;height:var(--app-header-height);background:#0f2d5c;z-index:50"></header>
            <main style="min-height:120vh;padding:1rem">
            <div class="passport-page passport-page--illustrated">
            <section class="passport-overview" style="background:var(--surface);border:1px solid var(--border);padding:1rem;margin-bottom:1rem">
            <a href="#" class="passport-card__fill-link">Aanvullen</a>
            </section>
            <div class="passport-tabs-shell"><div class="passport-tabs">
            <button type="button" class="passport-tab passport-tab--active">Wie ik ben</button>
            <button type="button" class="passport-tab">Tests</button>
            </div></div>
            <article style="min-height:600px;margin-top:1rem">Panel</article>
            </div></main></body></html>
            """;
    }

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
