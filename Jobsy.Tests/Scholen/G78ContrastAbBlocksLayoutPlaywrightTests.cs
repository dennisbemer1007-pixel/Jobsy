using System.Globalization;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// G78 contrast (g78-2): A/B pole blocks keep equal bounding boxes per question (2px) at 390 / 768 / 1440.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class G78ContrastAbBlocksLayoutPlaywrightTests
{
    [Theory]
    [InlineData(9001, 390, 844)]
    [InlineData(9001, 768, 1024)]
    [InlineData(9001, 1440, 900)]
    [InlineData(9016, 390, 844)]
    [InlineData(9035, 768, 1024)]
    [InlineData(9055, 1440, 900)]
    public async Task Contrast_ab_blocks_equal_size_per_question_ssr(int itemId, int width, int height)
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var repoRoot = G78ContrastScreenshotHarness.RepoRoot();
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

        await G78ContrastScreenshotHarness.PrepareGroep78ReisPageAsync(page, repoRoot, state, def, culture);
        await G78ContrastAbBlockLayoutAssert.AssertEqualAsync(page, $"{itemId}@{width}");

        if (itemId is 9001 or 9016)
        {
            var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-contrast-ab");
            Directory.CreateDirectory(artifactDir);
            var tag = width <= 400 ? "390" : width < 1200 ? "768" : "1440";
            await page.Locator(".ll-g78-contrast").ScreenshotAsync(new LocatorScreenshotOptions
            {
                Path = Path.Combine(artifactDir, $"after-contrast-blocks-{itemId}-{tag}.png"),
            });
        }
    }

    [Fact]
    public async Task Contrast_ab_blocks_equal_on_short_and_long_text_items()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var repoRoot = G78ContrastScreenshotHarness.RepoRoot();
        var culture = G78ContrastScreenshotHarness.NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.GetGroep78(PupilG78ScoringVersions.V2);
        var ids = new[] { 9001, 9012, 9028, 9041, 9059 };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            DeviceScaleFactor = 2,
        });

        foreach (var itemId in ids)
        {
            var state = G78ContrastScreenshotHarness.ReisStateForItem(itemId);
            await G78ContrastScreenshotHarness.PrepareGroep78ReisPageAsync(page, repoRoot, state, def, culture);
            await G78ContrastAbBlockLayoutAssert.AssertEqualAsync(page, itemId.ToString(CultureInfo.InvariantCulture));
        }
    }
}
