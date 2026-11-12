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
        var css = PassportDayLinkScreenshotHarness.InlineStyles(
            root,
            "css/features/mijn-paspoort.css",
            "css/features/future-jobs.css",
            "css/features/occupation-day.css",
            "css/features/leerling-paspoort-g78.css");

        var scenes = new (string name, string html)[]
        {
            ("candidate-fit-past-dit", PassportDayLinkScreenshotHarness.Wrap(css, PassportDayLinkScreenshotHarness.CandidateFitPastDit(culture))),
            ("g78-paspoort-job-day", PassportDayLinkScreenshotHarness.Wrap(css, PassportDayLinkScreenshotHarness.G78PaspoortJobCard(culture, withDay: true))),
            ("g78-paspoort-job-no-day", PassportDayLinkScreenshotHarness.Wrap(css, PassportDayLinkScreenshotHarness.G78PaspoortJobCard(culture, withDay: false))),
            ("g78-dit-ben-jij-jobs", PassportDayLinkScreenshotHarness.Wrap(css, PassportDayLinkScreenshotHarness.DitBenJijJobs(culture, withDay: true))),
            ("pupil-day-page", PassportDayLinkScreenshotHarness.Wrap(css, PassportDayLinkScreenshotHarness.PupilDayPage(culture))),
        };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (name, html) in scenes)
        {
            foreach (var (width, suffix) in new[] { (390, "390"), (1440, "1440") })
            {
                var page = await browser.NewPageAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = 900 },
                    DeviceScaleFactor = 2
                });
                await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });
                if (name.Contains("day", StringComparison.Ordinal) && !name.Contains("no-day"))
                {
                    Assert.True(await page.Locator("[data-testid=\"occupation-day-passport-link\"]").CountAsync() > 0
                                || await page.Locator(".day-life__timeline").CountAsync() > 0);
                }

                if (name.Contains("no-day"))
                {
                    Assert.Equal(0, await page.Locator("[data-testid=\"occupation-day-passport-link\"]").CountAsync());
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
