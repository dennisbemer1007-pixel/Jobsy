using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Guards static auth/signup shells: styled inputs/providers and cookie banner when undecided.
/// </summary>
[Collection("PlaywrightSmoke")]
public class StaticAuthVisualPlaywrightTests
{
    [Theory]
    [InlineData("/login", false)]
    [InlineData("/account-maken", true)]
    public async Task Static_auth_pages_have_styled_controls_and_cookie_banner(string path, bool expectSignupCss)
    {
        var baseUrl = await RequireBaseUrlAsync();
        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(390, 844));
        await ClearCookieConsentAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });

        Assert.False(await page.EvaluateAsync<bool>(
            "() => !!document.querySelector('script[src*=\"blazor.web.js\"]')"));

        var hasSignupCss = await page.EvaluateAsync<bool>(
            "() => [...document.querySelectorAll('link[rel=stylesheet]')].some(l => l.href.includes('signup-static.css'))");
        Assert.Equal(expectSignupCss, hasSignupCss);

        Assert.True(await page.Locator(".cookie-consent").CountAsync() > 0, "Cookie banner must show when consent unknown.");

        if (path.Contains("account-maken", StringComparison.Ordinal))
        {
            var provider = page.Locator(".provider-btn").First;
            if (await provider.CountAsync() > 0)
            {
                var minH = await provider.EvaluateAsync<double>(
                    "el => parseFloat(getComputedStyle(el).minHeight) || 0");
                Assert.True(minH >= 40, $"provider-btn min-height too small ({minH}px).");
            }

            var input = page.Locator(".login-form input[name=email], .pub-signup__form input[name=email]").First;
            await input.WaitForAsync(new() { Timeout = 10_000 });
            var inputWidth = await input.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
            Assert.True(inputWidth >= 240, $"Signup email input too narrow ({inputWidth}px).");
        }
        else
        {
            var input = page.Locator("#login-email");
            var inputWidth = await input.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
            Assert.True(inputWidth >= 240, $"Login email input too narrow ({inputWidth}px).");
        }
    }

    [Fact]
    public async Task Account_maken_code_step_renders_code_field_after_email_submit()
    {
        var baseUrl = await RequireBaseUrlAsync();
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_CAPTURE_EMAIL_CODES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var email = $"visual.{Guid.NewGuid():N}@example.test";
        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(390, 844));
        await ClearCookieConsentAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.Locator("input[name=email]").First.FillAsync(email);
        await page.Locator("button.au-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**", new() { Timeout = 60_000 });

        Assert.True(await page.Locator("input[name=code]").CountAsync() > 0);
        var body = await page.InnerTextAsync("body");
        Assert.DoesNotContain("Stuur mijn code", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", body, StringComparison.OrdinalIgnoreCase);

        var codeInput = page.Locator("input[name=code]").First;
        var w = await codeInput.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(w >= 240, $"Code input too narrow ({w}px).");
    }

    private static async Task ClearCookieConsentAsync(IBrowserContext context)
    {
        await context.AddInitScriptAsync(
            """
            () => {
              try { localStorage.removeItem('Jobsy.CookieConsent'); } catch (e) {}
              document.documentElement.classList.remove('cookie-consent-known');
            }
            """);
    }

    private static async Task<string> RequireBaseUrlAsync()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unset or unreachable.");
        }

        return baseUrl;
    }
}
