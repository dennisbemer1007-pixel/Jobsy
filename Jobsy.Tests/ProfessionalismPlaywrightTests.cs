using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real-browser checks for Bedrijfsgegevens, security.txt, the acceptatie noindex switch,
/// the friendly 404 and the one age rule. Soft-skips when JOBSY_E2E_BASE_URL is unset.
/// The CI stack is Development and does not set Seo__NoIndex, so the noindex check
/// asserts the live switch: on when robots.txt disallows everything, off otherwise.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ProfessionalismPlaywrightTests
{
    private const string Password = "Jobsy123!";

    [Fact]
    public async Task Admin_save_is_shown_on_privacy_and_a_cleared_field_disappears()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await LoginAdminAsync(page, baseUrl);

        var settings = baseUrl + "/admin/instellingen/algemeen";
        await page.GotoAsync(settings, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var legal = page.Locator("[data-testid=company-legal-name]");
        await legal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var originalLegal = await legal.InputValueAsync();
        var originalKvk = await page.Locator("[data-testid=company-kvk]").InputValueAsync();
        var originalStreet = await page.Locator("[data-testid=company-street]").InputValueAsync();
        var marker = "Proefbedrijf " + Guid.NewGuid().ToString("N")[..6];
        const string kvk = "10203040";

        try
        {
            await legal.FillAsync(marker);
            await page.Locator("[data-testid=company-street]").FillAsync("Teststraat 9");
            await page.Locator("[data-testid=company-kvk]").FillAsync(kvk);
            await page.Locator("[data-testid=company-save]").ClickAsync();
            await Assertions.Expect(page.GetByText("Bedrijfsgegevens opgeslagen.")).ToBeVisibleAsync(new() { Timeout = 20_000 });

            await page.GotoAsync(baseUrl + "/privacy", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            var card = page.Locator(".pp-identity");
            await Assertions.Expect(card).ToContainTextAsync(marker);
            await Assertions.Expect(card).ToContainTextAsync(kvk);
            await Assertions.Expect(card).ToContainTextAsync("Teststraat 9");

            await page.GotoAsync(settings, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.Locator("[data-testid=company-kvk]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.Locator("[data-testid=company-kvk]").FillAsync("");
            await page.Locator("[data-testid=company-save]").ClickAsync();
            await Assertions.Expect(page.GetByText("Bedrijfsgegevens opgeslagen.")).ToBeVisibleAsync(new() { Timeout = 20_000 });

            await page.GotoAsync(baseUrl + "/privacy", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await Assertions.Expect(page.Locator(".pp-identity")).ToContainTextAsync(marker);
            Assert.DoesNotContain(kvk, await page.Locator(".pp-identity").InnerTextAsync(), StringComparison.Ordinal);
        }
        finally
        {
            await page.GotoAsync(settings, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.Locator("[data-testid=company-legal-name]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.Locator("[data-testid=company-legal-name]").FillAsync(originalLegal);
            await page.Locator("[data-testid=company-street]").FillAsync(originalStreet);
            await page.Locator("[data-testid=company-kvk]").FillAsync(originalKvk);
            await page.Locator("[data-testid=company-save]").ClickAsync();
            await Assertions.Expect(page.GetByText("Bedrijfsgegevens opgeslagen.")).ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
    }

    [Fact]
    public async Task Security_txt_answers_plain_text()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        using var http = new HttpClient();
        var response = await http.GetAsync(baseUrl + "/.well-known/security.txt");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Expires:", body, StringComparison.Ordinal);
        Assert.Contains("Preferred-Languages: nl, en", body, StringComparison.Ordinal);
        var expires = Regex.Match(body, @"Expires: (\S+)").Groups[1].Value;
        Assert.True(DateTime.Parse(expires, null, System.Globalization.DateTimeStyles.AdjustToUniversal) > DateTime.UtcNow);
    }

    [Fact]
    public async Task Noindex_follows_the_acceptatie_switch()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        using var http = new HttpClient();
        var robots = await http.GetAsync(baseUrl + "/robots.txt");
        var robotsBody = await robots.Content.ReadAsStringAsync();
        var switchedOn = robotsBody.Contains("Disallow: /", StringComparison.Ordinal)
                         && !robotsBody.Contains("Sitemap:", StringComparison.Ordinal);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(baseUrl + "/privacy", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        Assert.NotNull(response);
        var html = await page.ContentAsync();

        if (switchedOn)
        {
            Assert.Contains("noindex, nofollow", response!.Headers["x-robots-tag"], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("noindex, nofollow", html, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            var header = response!.Headers.TryGetValue("x-robots-tag", out var tag) ? tag : "";
            Assert.DoesNotContain("noindex", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("index,follow", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Unknown_url_shows_the_friendly_404()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync(baseUrl + "/deze-pagina-bestaat-niet", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });

        Assert.Equal(404, response!.Status);
        Assert.Contains("text/html", response.Headers["content-type"], StringComparison.OrdinalIgnoreCase);
        var text = await page.Locator("body").InnerTextAsync();
        Assert.Contains("Deze pagina bestaat niet", text, StringComparison.Ordinal);
        Assert.True(await page.Locator("a[href='/']").CountAsync() > 0);
        Assert.True(await page.Locator("a[href='/ontdek']").CountAsync() > 0);
        Assert.True(await page.Locator("a[href='/melden']").CountAsync() > 0);
        Assert.Contains("noindex", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Age_rule_is_visible_on_home_signup_and_privacy()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var home = await page.Locator("body").InnerTextAsync();
        Assert.Contains("13", home, StringComparison.Ordinal);
        Assert.Contains("16", home, StringComparison.Ordinal);

        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var signup = await page.Locator("body").InnerTextAsync();
        Assert.Contains("13", signup, StringComparison.Ordinal);
        Assert.Contains("16", signup, StringComparison.Ordinal);

        await page.GotoAsync(baseUrl + "/privacy", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var privacy = await page.Locator("#jonger").InnerTextAsync();
        Assert.Contains("13", privacy, StringComparison.Ordinal);
        Assert.Contains("16", privacy, StringComparison.Ordinal);
        Assert.Contains("18", privacy, StringComparison.Ordinal);
    }

    private static async Task LoginAdminAsync(IPage page, string baseUrl)
    {
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? Password;
        await page.GotoAsync(baseUrl + "/login?returnUrl=" + Uri.EscapeDataString("/admin"), new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.FillAsync("input[name='email']", "admin@jobsy.local");
        await page.FillAsync("input[name='password']", password);
        var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
        await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Task.WhenAll(
            page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 }),
            submit.ClickAsync());
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        if (baseUrl.Contains("lobsy.nl", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.Contains("acceptatie", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This suite must not run against production.");
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await http.GetAsync(baseUrl + "/privacy");
            return response.StatusCode == HttpStatusCode.OK ? baseUrl : null;
        }
        catch
        {
            return null;
        }
    }
}
