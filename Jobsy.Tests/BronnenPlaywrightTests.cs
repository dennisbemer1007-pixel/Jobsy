using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Sources page and the short occupation footnote. Soft-skips when JOBSY_E2E_BASE_URL is unset.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BronnenPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, "nl")]
    [InlineData(390, 844, "nl")]
    [InlineData(390, 844, "ar")]
    public async Task Bronnen_page_shows_the_licence_lines(int width, int height, string lang)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(
            $"{baseUrl}/taal/{lang}?returnUrl=%2Fbronnen",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        Assert.NotNull(response);
        Assert.True(response!.Ok);

        var body = await page.Locator("body").InnerTextAsync();
        Assert.Contains("This service uses the ESCO classification of the European Commission.", body, StringComparison.Ordinal);
        Assert.Contains("O*NET 31.0 Database", body, StringComparison.Ordinal);
        Assert.Contains("USDOL/ETA has not approved, endorsed, or tested these modifications.", body, StringComparison.Ordinal);
        Assert.Contains("CBS, Beroepenclassificatie BRC 2014 editie 2025 (CC BY 4.0).", body, StringComparison.Ordinal);
        Assert.Contains("https://doi.org/10.34894/DVQTOG", body, StringComparison.Ordinal);
        Assert.Contains("Arbeidsmarktinformatiesysteem tot 2030", body, StringComparison.Ordinal);
        Assert.Contains("should not be considered an official ILO adaptation", body, StringComparison.Ordinal);
        Assert.Contains("Vaardigheden bij een beroep komen uit dezelfde ESCO-lijst.", body, StringComparison.Ordinal);
        if (lang == "nl")
        {
            Assert.Contains(
                "Lobsy gebruikt ESCO v1.2.1 en heeft de koppeling voor een aantal beroepen aangepast.",
                body,
                StringComparison.Ordinal);
        }

        if (lang == "ar")
        {
            var dir = await page.Locator("html").GetAttributeAsync("dir");
            Assert.Equal("rtl", dir);
        }
    }
}
