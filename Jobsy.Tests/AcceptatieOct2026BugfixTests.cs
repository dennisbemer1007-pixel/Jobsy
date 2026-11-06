using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Guards and Playwright checks for acceptatie bugs (scroll, cookie enhanced nav, header menu close).
/// </summary>
public class AcceptatieOct2026BugfixTests
{
    [Fact]
    public void App_main_default_does_not_trap_wheel_scroll()
    {
        var css = ReadCss();
        var mainBlock = ExtractRuleBlock(css, ".app-main {");
        Assert.Contains("overflow-y: visible", mainBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("overflow-y: auto", mainBlock, StringComparison.Ordinal);
        Assert.Contains(".app-shell.app-shell--questionnaire .app-main", css, StringComparison.Ordinal);
        Assert.Contains("overscroll-behavior-y: contain", css, StringComparison.Ordinal);
        Assert.Contains("overflow-x: clip", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Cookie_consent_js_reapplies_known_class_after_enhanced_navigation()
    {
        var js = ReadJs();
        Assert.Contains("enhancedload", js, StringComparison.Ordinal);
        Assert.Contains("consentStored", js, StringComparison.Ordinal);
        Assert.Contains("document.cookie.match", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Header_menu_close_buttons_use_data_menu_close()
    {
        var js = ReadJs();
        Assert.Contains("[data-menu-close]", js, StringComparison.Ordinal);
        var pageHelp = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Components", "Layout", "PageHelp.razor"));
        var bell = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Components", "Layout", "NotificationBell.razor"));
        Assert.Contains("data-menu-close", pageHelp, StringComparison.Ordinal);
        Assert.Contains("data-menu-close", bell, StringComparison.Ordinal);
        var css = ReadCss();
        Assert.Contains("min-width: 44px", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Interactive_cookie_banner_exposes_data_consent_for_early_clicks()
    {
        var banner = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Components", "CookieConsentBanner.razor"));
        Assert.Contains("data-consent=\"necessary\"", banner, StringComparison.Ordinal);
        Assert.Contains("data-consent=\"analytics\"", banner, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chromium_wheel_scrolls_document_on_typical_app_shell_fixture()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1366, Height = 768 }
        });
        var page = await context.NewPageAsync();
        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css");
        await page.RouteAsync("**/fixture/app.css", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/css",
                Path = cssPath
            });
        });

        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta charset="utf-8" />
              <link rel="stylesheet" href="/fixture/app.css" />
            </head>
            <body>
              <div class="app-shell has-bottom-nav">
                <header class="app-header" style="min-height:56px">Header</header>
                <main class="app-main">
                  <div style="height:2400px;padding:1rem">Long content</div>
                </main>
                <nav class="bottom-nav" style="min-height:64px">Nav</nav>
              </div>
            </body>
            </html>
            """);

        var main = page.Locator("main.app-main");
        var box = await main.BoundingBoxAsync();
        Assert.NotNull(box);
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
        for (var i = 0; i < 8; i++)
        {
            await page.Mouse.WheelAsync(0, 180);
        }

        var scrollY = await page.EvaluateAsync<double>("() => window.scrollY");
        Assert.True(scrollY > 40, $"Expected document scroll after wheel, scrollY={scrollY}");
    }

    [Fact]
    public async Task Header_menu_close_clears_data_header_menu_attribute()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("""
            <div class="page-help" data-menu-id="help">
              <button type="button" data-menu-trigger>i</button>
              <button type="button" class="page-help__close" data-menu-close>×</button>
            </div>
            """);
        var jsPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js");
        await page.AddScriptTagAsync(new() { Path = jsPath });

        await page.Locator("[data-menu-trigger]").ClickAsync();
        Assert.Equal("help", await page.EvaluateAsync<string?>("() => document.documentElement.getAttribute('data-header-menu')"));

        await page.Locator("[data-menu-close]").ClickAsync();
        Assert.Null(await page.EvaluateAsync<string?>("() => document.documentElement.getAttribute('data-header-menu')"));
    }

    [Fact]
    public async Task Enhancedload_restores_cookie_consent_known_class()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.RouteAsync("https://jobsy.test/**", route => route.FulfillAsync(new()
        {
            Status = 200,
            ContentType = "text/html",
            Body = "<!DOCTYPE html><html><body><div class=\"cookie-consent\"></div></body></html>"
        }));
        await page.GotoAsync("https://jobsy.test/");
        var jsPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js");
        await page.AddScriptTagAsync(new() { Path = jsPath });
        await page.EvaluateAsync("""
            () => {
              localStorage.setItem('Jobsy.CookieConsent', 'necessary');
              document.documentElement.classList.remove('cookie-consent-known');
            }
            """);
        await page.EvaluateAsync("() => document.dispatchEvent(new Event('enhancedload'))");
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('cookie-consent-known')"));
    }

    private static string ExtractRuleBlock(string css, string selectorPrefix)
    {
        var start = css.IndexOf(selectorPrefix, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing CSS block starting with {selectorPrefix}");
        var brace = css.IndexOf('{', start);
        var depth = 0;
        for (var i = brace; i < css.Length; i++)
        {
            if (css[i] == '{') depth++;
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return css.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException("Unclosed CSS block.");
    }

    private static string ReadCss()
        => File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));

    private static string ReadJs()
        => File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));

    private static string RepoRoot()
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
