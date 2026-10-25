using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class LandingWhyLobsyPlaywrightTests
{
    private const string BaseUrl = "http://127.0.0.1:5201";

    [Fact]
    public async Task Landing_why_lobsy_below_reiskaart_at_1440_and_390()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_LANDING_WHY_LOBSY") ?? "").Trim() != "1")
        {
            return;
        }

        if (!await KbE2e.IsReachableAsync(BaseUrl))
        {
            throw new InvalidOperationException($"Stack not reachable at {BaseUrl}");
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var outputDir = "/cursor/stores/self/artifacts/landing-why-lobsy";
        Directory.CreateDirectory(outputDir);

        foreach (var (width, height, tag, lang, snippet) in new[]
                 {
                     (1440, 900, "1440", "nl-NL", "Waarom een kreeft?"),
                     (390, 844, "390", "nl-NL", "oude schild"),
                     (1440, 900, "1440-en", "en-GB", "Why a lobster?"),
                     (390, 844, "390-en", "en-GB", "letting go"),
                 })
        {
            var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                Locale = lang,
            });
            var page = await context.NewPageAsync();
            var path = lang.StartsWith("en", StringComparison.Ordinal) ? "/?lang=en" : "/";
            await page.GotoAsync($"{BaseUrl}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });

            var stack = page.Locator("[data-testid='landing-reiskaart-stack']");
            await stack.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

            var why = stack.Locator(".pub-landing__why-lobsy");
            await why.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var link = why.Locator("a.pub-landing__why-lobsy-link[href='/wie-zijn-wij']");
            Assert.Equal(1, await link.CountAsync());

            var text = await why.InnerTextAsync();
            Assert.Contains(snippet, text, StringComparison.Ordinal);
            Assert.DoesNotContain("%", text, StringComparison.Ordinal);

            var reisBox = await stack.Locator("[data-testid='kreeft-reiskaart']").BoundingBoxAsync();
            var whyBox = await why.BoundingBoxAsync();
            Assert.NotNull(reisBox);
            Assert.NotNull(whyBox);
            Assert.True(whyBox!.Y >= reisBox!.Y + reisBox.Height - 2, $"Why copy should sit below reiskaart at {tag}");

            await page.ScreenshotAsync(new() { Path = Path.Combine(outputDir, $"landing-why-lobsy-{tag}.png") });
            await context.CloseAsync();
        }
    }
}
