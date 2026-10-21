using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>One-off diagnostic against Acceptatie — dumps hero DOM/styles for /carriere.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CareerHeroAccDiagnosticPlaywrightTests
{
    private const string AccUrl = "https://acceptatie.lobsy.nl";
    private const string Email = "onboarding.e2e@jobsy.local";
    private const string Password = "Jobsy123!";

    [Theory]
    [InlineData(1024)]
    [InlineData(1280)]
    [InlineData(1440)]
    [InlineData(1920)]
    public async Task Dump_carriere_hero_on_acceptatie(int width)
    {
        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "carriere-acc-diagnostic");
        Directory.CreateDirectory(outDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = 900 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(AccUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.FillAsync("input[name='email']", Email);
        await page.FillAsync("input[name='password']", Password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });

        await page.GotoAsync(AccUrl + "/carriere", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await page.WaitForTimeoutAsync(3000);

        var dump = await page.EvaluateAsync<JsonElement>("""
            () => {
              const pageRoot = document.querySelector('.career-page');
              const hero = document.querySelector('header.cand-hero, header.cand-compact-hero, .cand-hero');
              const copy = hero && hero.querySelector('.cand-hero__copy, .cand-compact-hero__copy');
              const h1 = copy && copy.querySelector('h1');
              const eyebrow = copy && copy.querySelector('.cand-hero__eyebrow');
              const sheets = Array.from(document.querySelectorAll('link[rel=stylesheet]'))
                .map(l => l.href)
                .filter(h => /carriere|candidate-hero|illustrated|app\.min/i.test(h));
              function box(el) {
                if (!el) return null;
                const r = el.getBoundingClientRect();
                return { left: r.left, top: r.top, width: r.width, height: r.height, right: r.right };
              }
              function style(el, props) {
                if (!el) return null;
                const cs = getComputedStyle(el);
                const o = {};
                props.forEach(p => { o[p] = cs[p]; });
                return o;
              }
              const props = ['position','left','right','marginLeft','marginRight','marginInline','paddingLeft','paddingInline','width','maxWidth','transform','overflow','display','gridTemplateColumns'];
              return {
                url: location.href,
                viewport: window.innerWidth,
                pageClasses: pageRoot ? pageRoot.className : null,
                heroTag: hero ? hero.tagName : null,
                heroClasses: hero ? hero.className : null,
                heroBox: box(hero),
                copyBox: box(copy),
                h1Box: box(h1),
                h1Text: h1 ? h1.textContent.trim().slice(0, 120) : null,
                eyebrowBox: box(eyebrow),
                eyebrowText: eyebrow ? eyebrow.textContent.trim() : null,
                copyStyle: style(copy, props),
                heroStyle: style(hero, props),
                stylesheets: sheets,
                headerNav: (() => {
                  const nav = document.querySelector('.app-shell-header nav, header.app-header nav, .header-nav');
                  return nav ? { classes: nav.className, box: box(nav), childCount: nav.children.length } : null;
                })()
              };
            }
            """);

        var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outDir, $"dump-{width}.json"), json);

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"before-{width}.png"),
            FullPage = false
        });
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
