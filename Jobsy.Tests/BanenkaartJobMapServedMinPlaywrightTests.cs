using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart must load wwwroot jobMap.min.js (not unrouted dev source) with PR #925 pin fixes.
/// </summary>
public sealed class BanenkaartJobMapServedMinPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IllustratedPagesWebFactory? _web;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
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
    public async Task Banenkaart_loads_committed_jobMap_min_js()
    {
        var root = FindRepoRoot();
        var expectedBytes = await File.ReadAllBytesAsync(
            Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.min.js"));

        await using var context = await _browser!.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            _baseUrl + "/banenkaart?originLat=52.0115&originLng=4.3571&maxMinutes=20&transport=Fiets",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });

        await page.WaitForFunctionAsync(
            """
            () => performance.getEntriesByType('resource')
              .some(e => e.name.includes('/js/jobMap.min.js'))
            """,
            null,
            new() { Timeout = 90_000 });

        var url = await page.EvaluateAsync<string>(
            """
            () => {
              const hit = performance.getEntriesByType('resource')
                .map(e => e.name)
                .find(u => u.includes('/js/jobMap.min.js'));
              return hit || '';
            }
            """);
        Assert.False(string.IsNullOrWhiteSpace(url));

        var served = await page.EvaluateAsync<byte[]>(
            """
            async (u) => {
              const res = await fetch(u, { cache: 'no-store' });
              const buf = await res.arrayBuffer();
              return [...new Uint8Array(buf)];
            }
            """,
            url!);

        Assert.Equal(expectedBytes.Length, served.Length);
        Assert.True(
            expectedBytes.AsSpan().SequenceEqual(served),
            "Served jobMap.min.js bytes must match repo wwwroot/js/jobMap.min.js (regenerate min + bump ?v=).");

        var text = Encoding.UTF8.GetString(served);
        var hasImageCount = CountOccurrences(text, "hasImage");
        Assert.True(hasImageCount >= 4, "Served min must include hasImage guard (PR #925), got " + hasImageCount);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
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
