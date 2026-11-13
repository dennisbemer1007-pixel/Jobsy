using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78ContrastQuestionScreenshotTests
{
    [Theory]
    [InlineData(9001, 1440, 900, "desktop", false)]
    [InlineData(9001, 390, 844, "mobile", false)]
    [InlineData(9001, 1440, 900, "desktop", true)]
    [InlineData(9001, 390, 844, "mobile", true)]
    [InlineData(9016, 1440, 900, "desktop", false)]
    [InlineData(9016, 390, 844, "mobile", false)]
    public async Task Contrast_reis_full_page_screenshot(int itemId, int width, int height, string label, bool selected)
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var repoRoot = G78ContrastScreenshotHarness.RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-contrast-v2", "reis", itemId.ToString());
        Directory.CreateDirectory(artifactDir);

        var culture = G78ContrastScreenshotHarness.NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.GetGroep78(PupilG78ScoringVersions.V2);
        var state = G78ContrastScreenshotHarness.ReisStateForItem(itemId);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            DeviceScaleFactor = width < 768 ? 2 : 1,
        });

        await G78ContrastScreenshotHarness.PrepareGroep78ReisPageAsync(
            page,
            repoRoot,
            state,
            def,
            culture,
            selected ? 3 : null);

        await G78ContrastScreenshotHarness.AssertReisStylesAsync(page);

        var suffix = selected ? $"{label}-selected" : label;
        var path = Path.Combine(artifactDir, $"question-{itemId}-{suffix}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        Assert.True(File.Exists(path));
    }
}
