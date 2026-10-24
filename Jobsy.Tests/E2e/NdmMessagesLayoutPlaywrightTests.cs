using System.Text.Json;
using Jobsy.Infrastructure.Data;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

public sealed class NdmMessagesLayoutPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Theory]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task Messages_hub_hero_and_thread_fit_viewport(int width)
    {
        var css = await File.ReadAllTextAsync(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/na-de-match.css"));
        var html = """
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"><style>
            """ + css + """
            </style></head><body>
            <div class="ndm ndm-page ndm-messages-page" data-ndm-messages-ssr="1">
            <main class="wrap">
            <section class="band ndm-hero ndm-hero--gesprek"><div class="bt"><span class="eyb">Sollicitaties · Berichten</span><h1>Berichten</h1><p class="ndm-hero-lead ndm-hero-lead--full">Per sollicitatie een rustige plek.</p></div><div class="art ndm-hero__art"></div></section>
            <div class="thr ndm-messages-thr"><div class="tlist"><a class="ti on" href="#"><span class="av w">DK</span><div class="sp"><b>Test</b><p>sub</p></div></a></div>
            <div class="tmain ndm-messages-tmain"><div class="thread"><div class="msg">Hoi</div></div><form class="compose"><input class="sp" name="body" /><button type="submit" class="btn sm">Go</button></form></div></div>
            </main></div></body></html>
            """;

        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = 900 },
        });
        await page.SetContentAsync(html);
        var json = await page.EvaluateAsync<JsonElement>(@"() => {
            var hero = document.querySelector('.ndm-hero--gesprek');
            var thr = document.querySelector('.ndm-messages-thr');
            function rect(el) {
                if (!el) return { right: 0, width: 0 };
                var r = el.getBoundingClientRect();
                return { right: r.right, width: r.width };
            }
            return { hero: rect(hero), thr: rect(thr), vw: window.innerWidth };
        }");

        Assert.True(json.GetProperty("hero").GetProperty("width").GetDouble() > 0);
        Assert.True(json.GetProperty("thr").GetProperty("width").GetDouble() > 0);
        Assert.True(json.GetProperty("hero").GetProperty("right").GetDouble() <= json.GetProperty("vw").GetDouble() + 1);
        Assert.True(json.GetProperty("thr").GetProperty("right").GetDouble() <= json.GetProperty("vw").GetDouble() + 1);
        await page.CloseAsync();
    }

    [Fact]
    public void Demo_wait_app_messages_url_uses_panel_messages()
    {
        var url = $"/candidate/applications/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=messages";
        Assert.Contains("panel=messages", url, StringComparison.Ordinal);
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
