using Jobsy.Core.Scholen;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78ContrastV2ResultScreenshotTests
{
    [Fact]
    public void A_heavy_and_B_heavy_v2_profiles_produce_different_fact_stories()
    {
        var a = PupilG78ContrastV2Fixtures.AllTowardA();
        var b = PupilG78ContrastV2Fixtures.AllTowardB();
        var renderer = new PupilStoryRenderer();
        var storyA = renderer.Render(a.Result, a.Progress, "nl");
        var storyB = renderer.Render(b.Result, b.Progress, "nl");
        Assert.NotEqual(storyA.Body, storyB.Body);
        Assert.NotEqual(a.Result.HollandCode, b.Result.HollandCode);
        Assert.True(storyA.Tiles.Count >= 4);
        Assert.True(storyB.Tiles.Count >= 4);
    }

    [Theory]
    [InlineData("toward-a", 1440, 900, "desktop")]
    [InlineData("toward-a", 390, 844, "mobile")]
    [InlineData("toward-b", 1440, 900, "desktop")]
    [InlineData("toward-b", 390, 844, "mobile")]
    [InlineData("toward-mixed", 1440, 900, "desktop")]
    [InlineData("toward-mixed", 390, 844, "mobile")]
    public async Task Dit_ben_jij_v2_full_result_screenshot(string profile, int width, int height, string label)
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var repoRoot = G78ContrastScreenshotHarness.RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-contrast-v2", "result", profile);
        Directory.CreateDirectory(artifactDir);

        var fixture = profile switch
        {
            "toward-a" => PupilG78ContrastV2Fixtures.AllTowardA(),
            "toward-mixed" => PupilG78ContrastV2Fixtures.MixedToward(),
            _ => PupilG78ContrastV2Fixtures.AllTowardB(),
        };
        var culture = G78ContrastScreenshotHarness.NlCulture();
        var dto = G78ContrastScreenshotHarness.BuildResultDto(fixture);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            DeviceScaleFactor = width < 768 ? 2 : 1,
        });

        await G78ContrastScreenshotHarness.PrepareDitBenJijPageAsync(page, repoRoot, dto, culture);

        var path = Path.Combine(artifactDir, $"dit-ben-jij-{label}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        Assert.True(File.Exists(path));
    }
}
