using System.Text.RegularExpressions;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class PassportDayLinkScreenshotTests
{
    [Fact]
    public async Task Capture_passport_day_link_surfaces_at_390_and_1440()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("PASSPORT_DAY_SCREENSHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        var root = RepoRoot();
        Directory.CreateDirectory("/opt/cursor/artifacts");
        var culture = PassportDayLinkScreenshotHarness.CreateCulture();

        var scenes = new (string name, string html, IReadOnlyList<string> cssPaths)[]
        {
            (
                "candidate-fit-past-dit",
                PassportDayLinkScreenshotHarness.CandidatePassportDocument(
                    PassportDayLinkScreenshotHarness.CandidatePassportFitTabBody(culture)),
                PassportDayLinkScreenshotHarness.StylesheetPaths(
                    root,
                    PageAssetCatalog.StylesheetsFor(PageAssetCatalog.Area.CandidateApp | PageAssetCatalog.Area.CandidatePublic, "/candidate/paspoort"))),
            (
                "g78-dit-ben-jij-jobs",
                PassportDayLinkScreenshotHarness.PupilShellDocument(
                    PassportDayLinkScreenshotHarness.DitBenJijResultBody(culture)),
                PassportDayLinkScreenshotHarness.StylesheetPaths(
                    root,
                    PageAssetCatalog.StylesheetsForPupilSsr("/leerling/dit-ben-jij"),
                    "css/features/leerling-paspoort-g78.css")),
            (
                "g78-paspoort-job-day",
                PassportDayLinkScreenshotHarness.PupilShellDocument(
                    PassportDayLinkScreenshotHarness.G78PaspoortPageBody(culture, withDay: true)),
                PassportDayLinkScreenshotHarness.StylesheetPaths(root, PageAssetCatalog.StylesheetsForPupilSsr("/leerling/paspoort"))),
            (
                "g78-paspoort-job-no-day",
                PassportDayLinkScreenshotHarness.PupilShellDocument(
                    PassportDayLinkScreenshotHarness.G78PaspoortPageBody(culture, withDay: false)),
                PassportDayLinkScreenshotHarness.StylesheetPaths(root, PageAssetCatalog.StylesheetsForPupilSsr("/leerling/paspoort"))),
            (
                "pupil-day-page",
                PassportDayLinkScreenshotHarness.PupilShellDocument(
                    PassportDayLinkScreenshotHarness.PupilDayPage(culture)),
                PassportDayLinkScreenshotHarness.StylesheetPaths(root, PageAssetCatalog.StylesheetsForPupilSsr("/leerling/beroep/x/dag"))),
        };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (name, html, cssPaths) in scenes)
        {
            foreach (var (width, suffix) in new[] { (390, "390"), (1440, "1440") })
            {
                var page = await browser.NewPageAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = 900 },
                    DeviceScaleFactor = 2
                });
                await SetupWwwrootRoutingAsync(page, root);
                await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                foreach (var css in cssPaths)
                {
                    await page.AddStyleTagAsync(new() { Path = css });
                }

                await page.WaitForTimeoutAsync(200);

                if (name == "candidate-fit-past-dit")
                {
                    await page.EvaluateAsync(
                        """
                        () => {
                          for (const panel of document.querySelectorAll('[data-passport-panel]')) {
                            const on = panel.getAttribute('data-passport-panel') === 'fit';
                            panel.toggleAttribute('hidden', !on);
                          }
                          for (const tab of document.querySelectorAll('[data-passport-tab]')) {
                            const on = tab.getAttribute('data-passport-tab') === 'fit';
                            tab.classList.toggle('passport-tab--active', on);
                            tab.setAttribute('aria-selected', on ? 'true' : 'false');
                            tab.setAttribute('tabindex', on ? '0' : '-1');
                          }
                        }
                        """);
                }

                if (name is "g78-paspoort-job-day" or "g78-dit-ben-jij-jobs" or "candidate-fit-past-dit")
                {
                    var link = page.Locator("[data-testid=\"occupation-day-passport-link\"]");
                    await Assertions.Expect(link.First).ToBeVisibleAsync();
                    await Assertions.Expect(link.First).ToContainTextAsync("Een dag als kok", new() { IgnoreCase = false });
                }

                if (name == "g78-paspoort-job-no-day")
                {
                    Assert.Equal(0, await page.Locator("[data-testid=\"occupation-day-passport-link\"]").CountAsync());
                }

                if (name == "g78-dit-ben-jij-jobs")
                {
                    await Assertions.Expect(page.Locator(".ll-g78-result-jobs .chip").First).ToBeVisibleAsync();
                    await Assertions.Expect(page.Locator("[data-testid=\"occupation-day-passport-link\"]").First).ToContainTextAsync("Een dag als kok");
                }

                if (name == "candidate-fit-past-dit")
                {
                    await Assertions.Expect(page.Locator("[data-testid=\"passport-panel-fit-content\"]")).ToBeVisibleAsync();
                    await Assertions.Expect(page.Locator("#passport-panel-fit")).ToBeVisibleAsync();
                    await Assertions.Expect(page.Locator("#passport-tab-fit")).ToHaveClassAsync(new Regex("passport-tab--active"));
                }

                if (name == "pupil-day-page")
                {
                    await Assertions.Expect(page.Locator(".day-life__back a")).ToBeVisibleAsync();
                }

                await page.ScreenshotAsync(new()
                {
                    Path = $"/opt/cursor/artifacts/passport-day-{name}-{suffix}.png",
                    FullPage = true
                });
                await page.CloseAsync();
            }
        }
    }

    private static async Task SetupWwwrootRoutingAsync(IPage page, string repoRoot)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web/wwwroot");
        await page.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            if (!string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal))
            {
                await route.FallbackAsync();
                return;
            }

            var relative = uri.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(relative))
            {
                await route.FallbackAsync();
                return;
            }

            var file = Path.Combine(wwwroot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file))
            {
                await route.FulfillAsync(new() { Path = file });
                return;
            }

            var imgFallback = Path.Combine(wwwroot, "img", "kreeft-kijker.png");
            if (relative.Contains("kreeft", StringComparison.OrdinalIgnoreCase) && File.Exists(imgFallback))
            {
                await route.FulfillAsync(new() { Path = imgFallback });
                return;
            }

            await route.FallbackAsync();
        });
    }

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
