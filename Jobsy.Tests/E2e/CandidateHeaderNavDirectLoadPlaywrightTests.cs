using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateHeaderNavDirectLoadPlaywrightTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots";

    private static readonly (string Path, string ActiveSuffix)[] CandidateNavRoutes =
    [
        ("/candidate/ontdekkingsreis", "/candidate/ontdekkingsreis"),
        ("/candidate/paspoort", "/candidate/paspoort"),
        ("/banenkaart", "/banenkaart"),
        ("/candidate/applications", "/candidate/applications"),
        ("/carriere", "/carriere"),
    ];

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ArtifactsDir);
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

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 900)]
    public async Task Logged_in_direct_load_no_loop_single_nav_correct_active(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 1024,
            IgnoreHTTPSErrors = true,
        });

        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");

        foreach (var (path, activeSuffix) in CandidateNavRoutes)
        {
            var redirects = 0;
            void OnResponse(object? _, IResponse response)
            {
                if (response.Status is >= 300 and < 400)
                {
                    redirects++;
                }
            }

            page.Response += OnResponse;

            await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(400);

            page.Response -= OnResponse;

            Assert.True(redirects < 4, $"Redirect loop suspected for {path} ({redirects} redirects).");
            Assert.Contains(path.TrimEnd('/'), page.Url, StringComparison.OrdinalIgnoreCase);

            if (width >= 1024)
            {
                await page.WaitForFunctionAsync(
                    "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                    null,
                    new() { Timeout = 60_000 });

                var navCount = await CountVisibleHeaderNavsAsync(page);
                Assert.Equal(1, navCount);

                var activeHref = await page.EvaluateAsync<string>(
                    """
                    () => {
                      const a = document.querySelector('[data-testid=candidate-header-nav] .candidate-header-nav__link.is-active');
                      return a ? (a.getAttribute('href') || '') : '';
                    }
                    """);
                Assert.True(
                    activeHref.Contains(activeSuffix, StringComparison.OrdinalIgnoreCase),
                    $"Expected active nav for {path}, got {activeHref}");
            }

            var slug = path.Trim('/').Replace('/', '-');
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, $"candidate-nav-logged-in-{slug}-{width}.png"),
                FullPage = false,
            });
        }
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 900)]
    public async Task Logged_out_direct_load_no_redirect_loop(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 1024,
            IgnoreHTTPSErrors = true,
        });

        await PlaywrightCookieConsent.AcceptAsync(context);

        foreach (var (path, _) in CandidateNavRoutes)
        {
            var page = await context.NewPageAsync();
            var redirects = 0;
            page.Response += (_, response) =>
            {
                if (response.Status is >= 300 and < 400)
                {
                    redirects++;
                }
            };

            await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(300);

            Assert.True(redirects < 5, $"Redirect loop for logged-out {path}: {redirects} redirects.");
            var url = page.Url;
            Assert.True(
                url.Contains("/login", StringComparison.OrdinalIgnoreCase)
                || url.Contains(path, StringComparison.OrdinalIgnoreCase),
                $"Unexpected logged-out landing for {path}: {url}");

            var slug = path.Trim('/').Replace('/', '-');
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, $"candidate-nav-logged-out-{slug}-{width}.png"),
                FullPage = false,
            });

            await page.CloseAsync();
        }
    }

    private static async Task<int> CountVisibleHeaderNavsAsync(IPage page)
        => await page.EvaluateAsync<int>(
            """
            () => Array.from(document.querySelectorAll('.app-header .candidate-header-nav'))
              .filter(n => {
                const style = getComputedStyle(n);
                const r = n.getBoundingClientRect();
                return style.display !== 'none' && r.height > 0 && r.width > 0;
              }).length
            """);
}
