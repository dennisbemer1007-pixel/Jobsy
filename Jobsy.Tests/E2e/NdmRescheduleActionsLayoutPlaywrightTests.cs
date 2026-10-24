using System.Text.Json;
using Jobsy.Tests.Applications;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

public sealed class NdmRescheduleActionsLayoutPlaywrightTests : IAsyncLifetime
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
    [InlineData(390, "reschedule")]
    [InlineData(390, "cancel")]
    [InlineData(1440, "reschedule")]
    [InlineData(1440, "cancel")]
    public async Task Action_row_shows_one_primary_button_and_nevermind(int width, string mode)
    {
        var html = NdmRescheduleShellFixtures.BuildHtml(mode);
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = 900 },
        });
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load });
        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Path = Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/ndm-reschedule.js"),
        });

        var json = await page.EvaluateAsync<JsonElement>(@"() => {
            function disp(sel) {
                var el = document.querySelector(sel);
                if (!el) return 'missing';
                return getComputedStyle(el).display;
            }
            function rect(sel) {
                var el = document.querySelector(sel);
                if (!el) return { width: 0, right: 0 };
                var r = el.getBoundingClientRect();
                return { width: r.width, right: r.right };
            }
            return {
                rescheduleDisplay: disp('[data-ndm-submit-reschedule]'),
                cancelDisplay: disp('[data-ndm-submit-cancel]'),
                neverDisplay: disp('[data-ndm-reschedule-nevermind]'),
                rescheduleRect: rect('[data-ndm-submit-reschedule]'),
                cancelRect: rect('[data-ndm-submit-cancel]'),
                neverRect: rect('[data-ndm-reschedule-nevermind]'),
                viewportWidth: window.innerWidth
            };
        }");

        var rescheduleDisplay = json.GetProperty("rescheduleDisplay").GetString();
        var cancelDisplay = json.GetProperty("cancelDisplay").GetString();
        var neverDisplay = json.GetProperty("neverDisplay").GetString();
        var viewportWidth = json.GetProperty("viewportWidth").GetDouble();

        if (mode == "reschedule")
        {
            Assert.Equal("none", cancelDisplay);
            Assert.NotEqual("none", rescheduleDisplay);
        }
        else
        {
            Assert.Equal("none", rescheduleDisplay);
            Assert.NotEqual("none", cancelDisplay);
        }

        Assert.NotEqual("none", neverDisplay);

        var primaryRect = mode == "reschedule"
            ? json.GetProperty("rescheduleRect")
            : json.GetProperty("cancelRect");
        var neverRect = json.GetProperty("neverRect");
        Assert.True(primaryRect.GetProperty("width").GetDouble() > 0);
        Assert.True(neverRect.GetProperty("width").GetDouble() > 0);
        Assert.True(primaryRect.GetProperty("right").GetDouble() <= viewportWidth + 1);
        Assert.True(neverRect.GetProperty("right").GetDouble() <= viewportWidth + 1);

        await page.CloseAsync();
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
