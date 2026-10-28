using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class SollicitatiesPlainPreviewPlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly ITestOutputHelper _output = output;
    private IllustratedPagesWebFactory? _web;
    private string? _baseUrl;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
        await ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _web?.Dispose();
        await ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Plain_sollicitaties_previews_have_no_large_empty_band()
    {
        if (_baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        await PlaywrightCookieConsent.AcceptAsync(context);

        foreach (var (path, slug, width, height) in new[]
                 {
                     ("/candidate/applications?_plainSkin=1", "sollicitaties", 1440, 900),
                     ("/candidate/applications?_plainSkin=1", "sollicitaties", 390, 844),
                     ("/candidate/liked?_plainSkin=1", "bewaard", 1440, 900),
                     ("/candidate/liked?_plainSkin=1", "bewaard", 390, 844),
                 })
        {
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            await page.SetViewportSizeAsync(width, height);
            await page.GotoAsync(_baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString(path));
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.WaitForSelectorAsync("[data-plain-app]", new() { Timeout = 60_000 });

            var gap = await page.EvaluateAsync<VerticalGapMetrics>(
                """
                () => {
                  const header = document.querySelector('.app-header, .pub-header, header.top');
                  const headerBottom = header ? header.getBoundingClientRect().bottom : 0;
                  const main = document.querySelector('[data-plain-app]') || document.querySelector('.app-main__content');
                  const firstBlock = main?.querySelector('.sollicitaties-intro, .sollicitaties-segment, .sollicitaties-tabs-row, .sollicitaties-empty, .kb-saved__toolbar, .candidate-job-list-tabs');
                  const firstTop = firstBlock ? firstBlock.getBoundingClientRect().top : headerBottom;
                  const gapUnderHeader = Math.max(0, Math.round(firstTop - headerBottom));
                  const bands = [];
                  const nodes = Array.from(document.querySelectorAll('.app-main *'));
                  for (const el of nodes) {
                    const r = el.getBoundingClientRect();
                    if (r.height < 24 || r.width < 40) continue;
                    const cs = getComputedStyle(el);
                    if (cs.display === 'none' || cs.visibility === 'hidden') continue;
                    const text = (el.innerText || '').trim();
                    const hasMedia = el.querySelector('img, svg, canvas, video, button, a, input, select, textarea');
                    if (text.length > 0 || hasMedia) continue;
                    if (el.classList.contains('sollicitaties-hero-strip') || el.classList.contains('kreeft-header-strip')) {
                      bands.push({ h: Math.round(r.height), cls: el.className });
                    }
                  }
                  bands.sort((a, b) => b.h - a.h);
                  const largest = bands[0]?.h ?? 0;
                  const under = firstBlock ? (firstBlock.className || firstBlock.tagName) : 'none';
                  return { gapUnderHeader, largestEmptyBand: largest, underHeader: under };
                }
                """);

            Assert.True(gap.LargestEmptyBand < 8, $"{slug}@{width}: lege strook {gap.LargestEmptyBand}px");
            Assert.True(gap.GapUnderHeader < 48, $"{slug}@{width}: gap onder header {gap.GapUnderHeader}px");
            _output.WriteLine(
                $"preview-{slug}-{width}: onder header={gap.UnderHeader}, gap={gap.GapUnderHeader}px, grootste lege strook={gap.LargestEmptyBand}px");

            Directory.CreateDirectory("/opt/cursor/artifacts");
            var file = $"/opt/cursor/artifacts/preview-{slug}-{width}.png";
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
            await page.CloseAsync();
        }
    }

    private sealed class VerticalGapMetrics
    {
        public int GapUnderHeader { get; set; }
        public int LargestEmptyBand { get; set; }
        public string? UnderHeader { get; set; }
    }
}
