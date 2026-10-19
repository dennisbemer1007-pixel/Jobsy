using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile 390 layout for external vacancy detail and other candidate shells with the coach dock.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ExternalVacancyMobileLayoutPlaywrightTests
{
    public static IEnumerable<object[]> CandidateScrollFixtures =>
    [
        ["kb-ext", "external-vacancy"],
        ["passport-page--illustrated", "paspoort"],
        ["panel-page--cand-ill kb-saved", "zoeken-bewaard"],
        ["panel-page--cand-ill kb-apps", "sollicitaties"],
        ["panel-page--cand-ill career-page--illustrated", "carriere"],
        ["panel-page--cand-ill candidate-hub", "reis"],
    ];

    [Theory]
    [MemberData(nameof(CandidateScrollFixtures))]
    public async Task Mobile_390_scroll_slack_after_last_content_is_within_bottom_nav_plus_48(string pageClass, string artifactSlug)
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            Locale = "nl-NL",
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildShellHtml(pageClass), new() { WaitUntil = WaitUntilState.Load });

        var audit = await AuditScrollEndGapAsync(page);
        Assert.True(audit.Ok, audit.Reason ?? "scroll end gap audit failed");

        var outDir = "/opt/cursor/artifacts/oude-kreeft";
        Directory.CreateDirectory(outDir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"mobile-scroll-{artifactSlug}-390.png"),
            FullPage = true,
        });
    }

    [Fact]
    public async Task Mobile_390_external_vacancy_uses_equal_16px_gutters()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildShellHtml("kb-ext"), new() { WaitUntil = WaitUntilState.Load });

        var audit = await page.EvaluateAsync<JsonElement>("""
            () => {
              const row = document.querySelector('.kb-ext__header-row');
              const card = document.querySelector('.kb-ext__card');
              const header = document.querySelector('.app-header');
              const probe = row || card;
              if (!probe || !header) return { ok: false, reason: 'missing-content-or-header' };
              const p = probe.getBoundingClientRect();
              const h = header.getBoundingClientRect();
              const gutter = 16;
              const leftOk = Math.abs(p.left - gutter) <= 3;
              const rightOk = Math.abs(window.innerWidth - p.right - gutter) <= 3;
              const ok = leftOk && rightOk;
              return {
                ok,
                reason: ok ? 'ok' : `content-left-${Math.round(p.left)}-right-${Math.round(window.innerWidth - p.right)}`,
                headerLeft: Math.round(h.left),
                headerRight: Math.round(window.innerWidth - h.right),
              };
            }
            """);

        Assert.True(audit.GetProperty("ok").GetBoolean(), audit.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Mobile_390_external_vacancy_apply_cta_matches_card_width()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildShellHtml("kb-ext"), new() { WaitUntil = WaitUntilState.Load });

        var audit = await page.EvaluateAsync<JsonElement>("""
            () => {
              const card = document.querySelector('.kb-ext__card');
              const apply = document.querySelector('[data-testid="kb-ext-apply-cta"]');
              if (!card || !apply) return { ok: false, reason: 'missing-card-or-apply' };
              const c = card.getBoundingClientRect();
              const a = apply.getBoundingClientRect();
              const widthOk = Math.abs(a.width - c.width) <= 2;
              const leftOk = Math.abs(a.left - c.left) <= 2;
              return {
                ok: widthOk && leftOk,
                reason: widthOk && leftOk ? 'ok' : `apply-${Math.round(a.width)}x${Math.round(a.left)}-card-${Math.round(c.width)}x${Math.round(c.left)}`,
              };
            }
            """);

        Assert.True(audit.GetProperty("ok").GetBoolean(), audit.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Mobile_390_external_vacancy_coach_never_overlaps_main_while_scrolling()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildShellHtml("kb-ext"), new() { WaitUntil = WaitUntilState.Load });
        await LoadCoachGuardScriptAsync(page);

        var audit = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const root = document.scrollingElement || document.documentElement;
              const maxY = Math.max(0, root.scrollHeight - window.innerHeight);
              const step = 40;

              function auditStep(scrollY) {
                const dock = document.querySelector('.lobsy-coach-dock');
                const coach = document.querySelector('#lobsy-coach-btn');
                if (!coach || !dock) return { ok: true, reason: 'no-coach' };
                const dockStyle = getComputedStyle(dock);
                if (
                  dock.classList.contains('lobsy-coach-dock--suppressed')
                  || dockStyle.visibility === 'hidden'
                  || parseFloat(dockStyle.opacity || '1') < 0.05
                ) {
                  return { ok: true, reason: 'coach-suppressed' };
                }
                const c = coach.getBoundingClientRect();
                if (c.width < 8 || c.height < 8) return { ok: true, reason: 'coach-collapsed' };

                function intersects(a, b) {
                  return a.left < b.right - 1 && a.right > b.left + 1 && a.top < b.bottom - 1 && a.bottom > b.top + 1;
                }

                const nodes = document.querySelectorAll(
                  'main a, main button, main p, main td, main th, main h1, main h2, main h3, main li, main .kb-ext__title',
                );
                for (const node of nodes) {
                  if (!node || dock.contains(node)) continue;
                  const style = getComputedStyle(node);
                  if (style.display === 'none' || style.visibility === 'hidden') continue;
                  const box = node.getBoundingClientRect();
                  if (box.width < 4 || box.height < 4) continue;
                  if (box.bottom <= 0 || box.top >= window.innerHeight) continue;
                  if (intersects(c, box)) {
                    return {
                      ok: false,
                      reason: `overlap-y${Math.round(scrollY)}-${(node.textContent || '').trim().slice(0, 20)}`,
                    };
                  }
                }
                return { ok: true, reason: 'ok' };
              }

              for (let y = 0; y <= maxY + step; y += step) {
                root.scrollTop = y;
                window.scrollTo(0, y);
                if (window.jobsyCoachDock?.bindKbExtCoachGuard) {
                  window.jobsyCoachDock.bindKbExtCoachGuard();
                }
                await new Promise((r) => setTimeout(r, 30));
                const stepAudit = auditStep(y);
                if (!stepAudit.ok) {
                  return { ok: false, reason: stepAudit.reason, scrollY: y, maxY };
                }
              }
              return { ok: true, reason: 'ok', maxY };
            }
            """);

        Assert.True(audit.GetProperty("ok").GetBoolean(), audit.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Mobile_390_external_vacancy_coach_does_not_overlap_facts_text()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(BuildShellHtml("kb-ext"), new() { WaitUntil = WaitUntilState.Load });
        await LoadCoachGuardScriptAsync(page);
        await page.EvaluateAsync("""
            () => {
              const dock = document.querySelector('.lobsy-coach-dock');
              const btn = dock?.querySelector('#lobsy-coach-btn') || dock;
              if (!btn) return;
              const box = btn.getBoundingClientRect();
              const clear = Math.max(72, Math.ceil(window.innerWidth - box.left + 12));
              document.documentElement.style.setProperty('--lobsy-coach-clear-inline', clear + 'px');
            }
            """);

        await page.EvaluateAsync("""
            () => {
              const actions = document.querySelector('[data-testid="kb-ext-detail-actions"]');
              const root = document.scrollingElement || document.documentElement;
              if (actions) {
                const y = window.scrollY + actions.getBoundingClientRect().top - window.innerHeight * 0.55;
                root.scrollTop = Math.max(0, y);
                window.scrollTo(0, Math.max(0, y));
              } else {
                root.scrollTop = root.scrollHeight;
                window.scrollTo(0, document.body.scrollHeight);
              }
            }
            """);
        await page.WaitForTimeoutAsync(150);

        var audit = await AuditCoachTextOverlapAsync(page);
        Assert.True(audit.Ok, audit.Reason ?? "coach overlap audit failed");

        var suppressed = await page.EvaluateAsync<bool>("""
            () => {
              const dock = document.querySelector('.lobsy-coach-dock');
              return !!dock?.classList.contains('lobsy-coach-dock--suppressed');
            }
            """);
        Assert.True(suppressed, "coach should hide while action buttons are in view");

        Assert.False(await page.Locator(".lobsy-coach-dock__label").IsVisibleAsync());

        var outDir = "/opt/cursor/artifacts/oude-kreeft";
        Directory.CreateDirectory(outDir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, "external-vacancy-coach-390.png"),
            FullPage = false,
        });
    }

    private static async Task<(bool Ok, string? Reason)> AuditScrollEndGapAsync(IPage page)
    {
        await page.EvaluateAsync("""
            () => {
              const root = document.scrollingElement || document.documentElement;
              root.scrollTo(0, root.scrollHeight);
            }
            """);
        await page.WaitForTimeoutAsync(100);

        var json = await page.EvaluateAsync<JsonElement>("""
            () => {
              const root = document.scrollingElement || document.documentElement;
              const last =
                document.querySelector('[data-testid="kb-ext-detail-actions"]')
                || document.querySelector('.kb-ext__actions')
                || document.querySelector('.cand-panel-end');
              if (!last) return { ok: false, reason: 'missing-last-content' };

              const nav = document.querySelector('.bottom-nav');
              const navH = nav ? nav.getBoundingClientRect().height : 64;
              const maxSlack = navH + 24;

              const lastRect = last.getBoundingClientRect();
              const rootTop = root.getBoundingClientRect().top;
              const lastBottomDoc = lastRect.bottom - rootTop + root.scrollTop;
              const scrollSlack = root.scrollHeight - lastBottomDoc;

              const ok = scrollSlack <= maxSlack + 2;
              const reason = ok
                ? 'ok'
                : `scrollSlack-${Math.round(scrollSlack)}-maxSlack-${Math.round(maxSlack)}`;
              return { ok, reason, scrollSlack, maxSlack };
            }
            """);
        return (json.GetProperty("ok").GetBoolean(), json.GetProperty("reason").GetString());
    }

    private static async Task LoadCoachGuardScriptAsync(IPage page)
    {
        var root = FindRepoRoot();
        await page.AddScriptTagAsync(new() { Path = Path.Combine(root, "Jobsy.Web/wwwroot/js/app-core.js") });
        await page.EvaluateAsync("""
            () => {
              if (window.jobsyCoachDock?.bindKbExtCoachGuard) {
                window.jobsyCoachDock.bindKbExtCoachGuard();
              }
            }
            """);
    }

    private static async Task<(bool Ok, string? Reason)> AuditCoachTextOverlapAsync(IPage page)
    {
        var json = await page.EvaluateAsync<JsonElement>("""
            () => {
              const dock = document.querySelector('.lobsy-coach-dock');
              const coach = document.querySelector('#lobsy-coach-btn') || document.querySelector('.lobsy-coach-dock__btn');
              if (!coach || !dock) return { ok: true, reason: 'no-coach' };
              const dockStyle = getComputedStyle(dock);
              if (
                dock.classList.contains('lobsy-coach-dock--suppressed')
                || dockStyle.visibility === 'hidden'
                || parseFloat(dockStyle.opacity || '1') < 0.05
              ) {
                return { ok: true, reason: 'coach-suppressed' };
              }
              const c = coach.getBoundingClientRect();
              if (c.width < 8) return { ok: false, reason: 'coach-not-laid-out' };

              function intersects(a, b) {
                return a.left < b.right - 1 && a.right > b.left + 1 && a.top < b.bottom - 1 && a.bottom > b.top + 1;
              }

              const nodes = document.querySelectorAll('.kb-ext__facts th, .kb-ext__facts td, .kb-ext__title, .kb-ext__travel');
              for (const node of nodes) {
                const style = getComputedStyle(node);
                if (style.display === 'none' || style.visibility === 'hidden') continue;
                const box = node.getBoundingClientRect();
                if (box.width < 4 || box.height < 4) continue;
                if (intersects(c, box)) {
                  return { ok: false, reason: 'coach-overlaps-' + (node.textContent || '').trim().slice(0, 24) };
                }
              }
              return { ok: true, reason: 'ok' };
            }
            """);
        return (json.GetProperty("ok").GetBoolean(), json.GetProperty("reason").GetString());
    }

    private static string BuildShellHtml(string pageClass)
    {
        var root = FindRepoRoot();
        var appCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var extCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kb-external-vacancy.css"));
        var passportCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        var kompasCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kompas-illustrated.css"));
        var candCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/candidate-illustrated-app.css"));

        var body = pageClass.Contains("kb-ext", StringComparison.Ordinal)
            ? BuildExternalVacancyBody()
            : BuildIllustratedEndMarkerBody(pageClass);

        return $$"""
            <!DOCTYPE html><html lang="nl" class="cookie-consent-known"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
            :root { --app-header-height: 56px; --bg: #f5f2ee; --surface: #fffcfa; --border: #ddd5cc; --brand: #0f2d5c; --bottom-nav-h: 64px; --pearl: #f5f2ee; }
            html, body { min-height: auto !important; }
            body { margin: 0; background: var(--bg); font-family: system-ui, sans-serif; }
            .app-shell { padding-top: var(--app-header-height); box-sizing: border-box; }
            {{appCss}}
            {{extCss}}
            {{passportCss}}
            {{kompasCss}}
            {{candCss}}
            </style></head><body>
            <div class="app-shell has-bottom-nav" data-lobsy-coach style="--bottom-nav-h:64px">
            <header class="app-header" style="min-height:var(--app-header-height);background:var(--pearl-shine);border-bottom:1px solid var(--border);padding:0 1rem;display:flex;align-items:center">Lobsy</header>
            <main class="app-main"><div class="app-main__content">{{body}}</div></main>
            <nav class="bottom-nav" aria-label="Nav" style="position:fixed;bottom:0;left:0;right:0;height:var(--bottom-nav-h);background:#fff;border-top:1px solid var(--border);z-index:40"></nav>
            <div class="lobsy-coach-dock" data-lobsy-coach>
            <button type="button" id="lobsy-coach-btn" class="lobsy-coach-dock__btn lobsy-coach-dock__btn--warm" aria-label="Vraag het Lobsy">
            <span class="lobsy-coach-dock__medallion" aria-hidden="true"><img src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='56' height='56'/%3E" width="56" height="56" alt=""></span>
            <span class="lobsy-coach-dock__label">Vraag</span>
            </button></div></div></body></html>
            """;
    }

    private static string BuildExternalVacancyBody() => """
        <div class="kb-ext" data-testid="kb-ext-detail">
        <div class="kb-ext__header-row"><div class="kb-ext__header-copy"><h1 class="kb-ext__title">Bedrijfsleider</h1><p class="muted">Intertoys · Den Haag</p></div><button type="button" class="read-aloud read-aloud--icon" aria-label="Lees voor"><span class="read-aloud__icon" aria-hidden="true">▶</span></button></div>
        <section class="kb-ext__card"><h2 class="kb-ext__method-title">Wat ze vragen</h2><ul class="kb-ext__bullets"><li>MBO</li></ul></section>
        <p class="kb-ext__travel kb-ext__travel--hint muted" data-testid="kb-ext-travel-hint">Vul je adres in om reistijd te zien.</p>
        <section class="kb-ext__card"><h2 class="kb-ext__method-title">Feiten</h2>
        <table class="kb-ext__facts"><tbody>
        <tr><th scope="row">Start</th><td class="is-unknown">Dat weet ik niet</td></tr>
        <tr><th scope="row">Opleiding</th><td class="is-unknown">Dat weet ik niet</td></tr>
        </tbody></table></section>
        <section class="kb-ext__card"><h2 class="kb-ext__method-title">Match</h2><ul class="kb-ext__bullets"><li>Sterkte</li><li>Uitdaging</li></ul></section>
        <section class="kb-ext__card"><h2 class="kb-ext__method-title">Match</h2><ul class="kb-ext__bullets"><li>Leidinggeven</li><li>Retail</li><li>Klantcontact</li></ul></section>
        <section class="kb-ext__card"><p class="kb-ext__privacy">Privacy — alleen jij ziet deze match-analyse.</p></section>
        <p class="muted">Bron: intertoys.nl</p>
        <div class="kb-ext__actions" data-testid="kb-ext-detail-actions">
        <a class="kb-ext__btn" href="#" data-testid="kb-ext-apply-cta">Ik wil solliciteren</a>
        <div class="kb-ext__actions-row"><button type="button" class="kb-ext__btn kb-ext__btn--secondary" data-testid="kb-ext-save-toggle">Uit Bewaard</button><button type="button" class="kb-ext__btn kb-ext__btn--secondary" data-testid="kb-ext-not-interested">Niet interessant</button></div>
        </div></div>
        """;

    private static string BuildIllustratedEndMarkerBody(string pageClass) => $"""
        <div class="panel-page {pageClass}">
        <section style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:1rem;margin:1rem;min-height:720px">Inhoud</section>
        <p class="cand-panel-end" style="margin:1rem;padding:0 1rem 1rem">Einde panel</p>
        </div>
        """;

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
