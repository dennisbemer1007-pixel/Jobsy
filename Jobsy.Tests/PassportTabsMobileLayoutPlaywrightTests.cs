using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public class PassportTabsMobileLayoutPlaywrightTests
{
    [Fact]
    public async Task Mobile_390_passport_tabs_do_not_overlap_cards_and_show_scroll_fade()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildPassportHtml(), new() { WaitUntil = WaitUntilState.Load });

        var unstuck = await AuditTabsAsync(page, scrollMain: false);
        Assert.True(unstuck.Ok, unstuck.Reason ?? "audit failed");

        await page.EvaluateAsync("""
            () => {
              const root = document.scrollingElement || document.documentElement;
              root.scrollTop = 420;
              window.scrollTo(0, 420);
            }
            """);
        await page.WaitForTimeoutAsync(200);

        var stuck = await AuditTabsAsync(page, scrollMain: true);
        Assert.True(stuck.Ok, stuck.Reason ?? "audit failed");

        var css = await File.ReadAllTextAsync(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/kompas-illustrated.css"));
        Assert.Contains(".passport-page--illustrated .passport-tabs-shell::after", css, StringComparison.Ordinal);
    }

    private static async Task<(bool Ok, string? Reason)> AuditTabsAsync(IPage page, bool scrollMain)
    {
        var json = await page.EvaluateAsync<JsonElement>("""
            (scrollMain) => {
              const shell = document.querySelector('.passport-tabs-shell');
              const tabs = document.querySelector('.passport-tabs');
              if (!shell || !tabs) return { ok: false, reason: 'missing-shell-or-tabs' };

              const shellBox = shell.getBoundingClientRect();
              let node = shell.previousElementSibling;
              while (node) {
                if (node.matches?.('.passport-overview, .passport-stat, .passport-layout__card')) {
                  const c = node.getBoundingClientRect();
                  if (c.height >= 8 && c.width >= 8) {
                    const overlap = !(shellBox.top >= c.bottom - 1);
                    if (overlap) return { ok: false, reason: 'tabs-overlap-preceding-card' };
                  }
                }
                node = node.previousElementSibling;
              }

              const tabButtons = [...tabs.querySelectorAll('.passport-tab')];
              const tabsRect = tabs.getBoundingClientRect();
              for (const btn of tabButtons) {
                const b = btn.getBoundingClientRect();
                if (b.right > tabsRect.right + 2 && b.left < tabsRect.right - 4) {
                  const scrollable = tabs.scrollWidth > tabs.clientWidth + 2;
                  if (!scrollable) {
                    return { ok: false, reason: 'tab-clipped-without-scroll' };
                  }
                }
              }

              const pos = getComputedStyle(shell).position;
              if (scrollMain && pos !== 'sticky') {
                return { ok: false, reason: 'shell-not-sticky' };
              }

              if (scrollMain) {
                const header = document.querySelector('.app-header');
                if (header) {
                  const h = header.getBoundingClientRect();
                  if (h.bottom <= 4) {
                    return { ok: false, reason: 'header-scrolled-away' };
                  }
                  if (Math.abs(shellBox.top - h.bottom) > 4) {
                    return { ok: false, reason: 'tabs-not-below-header' };
                  }
                }
              }

              const backdrop = getComputedStyle(shell, '::before').backgroundColor;
              const bg = backdrop && backdrop !== 'rgba(0, 0, 0, 0)' ? backdrop : getComputedStyle(shell).backgroundColor;
              if (!bg || bg === 'rgba(0, 0, 0, 0)' || bg === 'transparent') {
                return { ok: false, reason: 'shell-bg-transparent' };
              }

              return { ok: true, reason: 'ok' };
            }
            """, scrollMain);
        return (json.GetProperty("ok").GetBoolean(), json.GetProperty("reason").GetString());
    }

    private static string BuildPassportHtml()
    {
        var root = FindRepoRoot();
        var appCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var passportCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        var kompasCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kompas-illustrated.css"));
        return $$"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
            :root { --app-header-height: 56px; --bg: #f5f2ee; --surface: #fffcfa; --border: #ddd5cc; --brand: #0f2d5c; --z-sticky: 20; --space-1: 0.25rem; --space-2: 0.5rem; --space-3: 0.75rem; --space-4: 1rem; --bottom-nav-h: 64px; }
            body { margin: 0; background: var(--bg); font-family: system-ui, sans-serif; }
            {{appCss}}
            {{passportCss}}
            {{kompasCss}}
            </style></head><body>
            <div class="app-shell has-bottom-nav">
            <header class="app-header" style="min-height:var(--app-header-height);background:#0f2d5c;z-index:50"></header>
            <main class="app-main" style="min-height:120vh;padding:1rem">
            <div class="panel-page passport-page passport-page--illustrated">
            <div class="passport-layout"><div class="passport-layout__main">
            <section class="passport-overview" style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:1rem;margin-bottom:1rem">
            <button type="button" style="display:block;width:100%;margin-top:0.5rem;padding:0.75rem;background:var(--brand);color:#fff;border:0;border-radius:8px">Naar de tests</button>
            </section>
            <div class="passport-tabs-shell">
            <div class="passport-tabs" role="tablist">
            <button type="button" class="passport-tab passport-tab--active" id="passport-tab-dna">Wie ik ben</button>
            <button type="button" class="passport-tab" id="passport-tab-tests">Mijn tests <span class="passport-tabs__pill">4</span></button>
            <button type="button" class="passport-tab" id="passport-tab-fit">Past bij?</button>
            <button type="button" class="passport-tab" id="passport-tab-career">Carrière</button>
            <button type="button" class="passport-tab" id="passport-tab-proof">Bewijzen</button>
            </div></div>
            <article class="passport-panel" style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:1rem;margin-top:1rem;min-height:400px">Panel</article>
            </div></div></div></main>
            <nav class="bottom-nav" style="position:fixed;bottom:0;left:0;right:0;height:var(--bottom-nav-h);background:#fff;border-top:1px solid var(--border)"></nav>
            </div></body></html>
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
