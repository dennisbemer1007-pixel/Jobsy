using System.Text.Json;
using Jobsy.Core.Scholen;
using Jobsy.Web.Scholen;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78ContrastV2RenderedDomTests
{
    [Theory]
    [InlineData("toward-a")]
    [InlineData("toward-b")]
    [InlineData("toward-mixed")]
    public async Task Rendered_dit_ben_jij_has_no_overlapping_story_and_tile_sentences(string profile)
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var repoRoot = G78ContrastScreenshotHarness.RepoRoot();
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
        var page = await browser.NewPageAsync();
        await G78ContrastScreenshotHarness.PrepareDitBenJijPageAsync(page, repoRoot, dto, culture);

        var payload = await page.EvaluateAsync<string>(
            """
            () => {
              const story = [...document.querySelectorAll('p.story-body')]
                .map(p => (p.textContent || '').trim())
                .filter(t => t.length > 0);
              const tiles = [...document.querySelectorAll('.rtile-txt')]
                .map(p => (p.textContent || '').trim())
                .filter(t => t.length > 0);
              return JSON.stringify({ story, tiles });
            }
            """);

        using var doc = JsonDocument.Parse(payload);
        var story = doc.RootElement.GetProperty("story").EnumerateArray().Select(e => e.GetString() ?? "").ToList();
        var tiles = doc.RootElement.GetProperty("tiles").EnumerateArray().Select(e => e.GetString() ?? "").ToList();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in story.Concat(tiles))
        {
            var polished = PupilG78PageTextDedupe.FinalizeRenderedText(block);
            foreach (var sentence in PupilG78PageTextDedupe.SplitRenderedSentences(polished))
            {
                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                if (norm.Length < 12)
                {
                    continue;
                }

                Assert.True(seen.Add(norm), $"[{profile}] Duplicate on rendered page: {sentence}");
            }
        }
    }
}
