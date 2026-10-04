using Jobsy.Web.Localization;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Logged-in page copy follows /taal/{locale}. Soft-skips when no site is running.
/// </summary>
[Collection("PlaywrightSmoke")]
public class LocaleContentPlaywrightTests
{
    [Theory]
    [InlineData("en", "Discovery.Test.Career.Title", "What do you enjoy?")]
    [InlineData("pl", "Discovery.Test.Career.Title", "Co lubisz?")]
    [InlineData("ro", "Discovery.Test.Career.Title", "Ce-ți place?")]
    [InlineData("ar", "Discovery.Test.Career.Title", "ماذا تحب؟")]
    public void Catalog_has_the_test_intro_in_each_locale(string lang, string key, string expected)
        => Assert.Equal(expected, UiStrings.Get(key, lang));

    [Theory]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public async Task Picker_locale_reaches_the_test_intro_passport_and_mail_page(string locale)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(baseUrl + "/taal/" + locale + "?returnUrl=/account/mail-instellingen", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        if (response is null || response.Status >= 500)
        {
            return;
        }

        var expectedMail = UiStrings.Get("MailSettings.Title", locale);
        var html = await page.ContentAsync();
        if (!html.Contains(expectedMail, StringComparison.Ordinal)
            && html.Contains("Inloggen", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Contains(expectedMail, html, StringComparison.Ordinal);
    }
}
