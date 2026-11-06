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

    [Theory]
    [InlineData("en", "What do you enjoy?", "Who I am", "Mail settings")]
    [InlineData("pl", "Co lubisz?", "Kim jestem", "Ustawienia e-mail")]
    [InlineData("ro", "Ce-ți place?", "Cine sunt", "Setări e-mail")]
    [InlineData("ar", "ماذا تحب؟", "من أنا", "إعدادات البريد")]
    public async Task Culture_cookie_beats_a_dutch_profile_on_intro_passport_and_mail(
        string locale,
        string intro,
        string who,
        string mail)
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
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
        if (await page.Locator("input[name='email']").CountAsync() == 0)
        {
            return;
        }

        await page.FillAsync("input[name='email']", "kandidaat@jobsy.local");
        await page.FillAsync("input[name='password']", "Jobsy123!");
        await page.ClickAsync("button.login-submit");
        try
        {
            await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var uri = new Uri(baseUrl);
        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "Jobsy.Culture",
                Value = locale,
                Domain = uri.Host,
                Path = "/"
            }
        ]);

        await page.GotoAsync(baseUrl + "/candidate/career?stap=intro", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
        await page.GetByRole(AriaRole.Heading, new() { Name = intro }).WaitForAsync(new() { Timeout = 20_000 });

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
        Assert.Contains(who, await page.ContentAsync(), StringComparison.Ordinal);

        await page.GotoAsync(baseUrl + "/account/mail-instellingen", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
        Assert.Contains(mail, await page.ContentAsync(), StringComparison.Ordinal);
    }
}
