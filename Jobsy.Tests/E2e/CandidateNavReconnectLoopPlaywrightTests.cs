using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Guards against #826-style synchronous MutationObserver ↔ Blazor reconnect class fights.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateNavReconnectLoopPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Reconnect_modal_class_oscillation_completes_without_main_thread_hang()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        var elapsedMs = await page.EvaluateAsync<double>(
            """
            () => {
              const m = document.getElementById('components-reconnect-modal');
              if (!m) throw new Error('no modal');
              const start = performance.now();
              for (let i = 0; i < 240; i++) {
                m.classList.add('components-reconnect-retrying');
                m.classList.remove('components-reconnect-retrying');
                m.classList.add('components-reconnect-show');
                m.classList.remove('components-reconnect-show');
                m.classList.add('components-reconnect-failed');
                m.classList.remove('components-reconnect-failed');
              }
              return performance.now() - start;
            }
            """);

        Assert.True(elapsedMs < 3000, $"Reconnect class oscillation took {elapsedMs:F0}ms (likely observer loop).");

        var titleTask = page.TitleAsync();
        var titleOk = await Task.WhenAny(titleTask, Task.Delay(3000)) == titleTask;
        Assert.True(titleOk, "page.Title() hung after reconnect class oscillation.");
        _ = await titleTask;
    }
}
