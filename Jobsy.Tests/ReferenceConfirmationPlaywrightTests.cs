using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Public referee page at phone, desktop, and Arabic (right to left).
/// Soft-skips when JOBSY_E2E_BASE_URL is not set.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ReferenceConfirmationPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, "nl")]
    [InlineData(390, 844, "nl")]
    [InlineData(390, 844, "ar")]
    public async Task Invalid_link_page_fits_the_viewport(int width, int height, string language)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        var token = new string('a', 64);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = language == "ar" ? "ar" : "nl-NL",
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        var path = language == "ar"
            ? $"/taal/ar?returnUrl={Uri.EscapeDataString("/referentie/" + token)}"
            : "/referentie/" + token;
        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        if (language == "ar")
        {
            var dir = await page.EvalOnSelectorAsync<string>("html", "el => el.getAttribute('dir') || el.dir || ''");
            Assert.Equal("rtl", dir);
        }

        var text = await page.InnerTextAsync("body");
        Assert.Contains(language == "ar" ? "الرابط" : "link", text, StringComparison.OrdinalIgnoreCase);
        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);
    }
}
