using System.Net;
using Jobsy.Tests.E2e;
using Jobsy.Web.Localization;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Journey zone/step labels on landing and onboarding sidebar (1440 + 390, NL + EN).
/// JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 dotnet test --filter Journey_step_names
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class JourneyStepNamesPlaywrightTests
{
    private static readonly (int Width, int Height)[] Viewports = [(1440, 900), (390, 844)];

    [Fact]
    public async Task Journey_step_names_on_landing_and_onboarding_sidebar()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
            });
            var page = await context.NewPageAsync();

            foreach (var lang in new[] { "nl", "en" })
            {
                await page.GotoAsync(
                    $"{baseUrl}/?lang={lang}#ontdekkingsreis",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
                await DismissCookiesAsync(page);

                var strandHeading = UiStrings.Get("Discovery.Journey.Strand", lang);
                await Assertions.Expect(page.Locator(".pub-landing__journey-step h3").First)
                    .ToHaveTextAsync(strandHeading);

                var lichtHeading = UiStrings.Get("Discovery.Journey.Licht", lang);
                await Assertions.Expect(page.Locator(".pub-landing__journey-step h3").Nth(4))
                    .ToHaveTextAsync(lichtHeading);
            }
        }

        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var authContext = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
        });
        var authPage = await authContext.NewPageAsync();
        if (!await KbE2e.TryLoginAsync(authPage, baseUrl, KbE2e.DefaultCompleteEmail, KbE2e.DefaultPassword))
        {
            return;
        }

        await authPage.GotoAsync(
            $"{baseUrl}/candidate/start?step=1&lang=nl",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await DismissCookiesAsync(authPage);

        await Assertions.Expect(authPage.Locator(".ob-wizard__side-item strong").First)
            .ToHaveTextAsync(UiStrings.Get("Onboarding.Phase.OverJou", "nl"));
        await Assertions.Expect(authPage.Locator(".ob-wizard__side-item strong").Nth(4))
            .ToHaveTextAsync(UiStrings.Get("Onboarding.Rustig.Phase.Kompas", "nl"));
    }

    private static async Task DismissCookiesAsync(IPage page)
    {
        var cookieAccept = page.GetByRole(AriaRole.Button, new() { Name = "Accepteer cookies" });
        if (await cookieAccept.CountAsync() > 0)
        {
            await cookieAccept.ClickAsync();
        }
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl + "/privacy");
            return response.StatusCode is HttpStatusCode.OK ? baseUrl : null;
        }
        catch
        {
            return null;
        }
    }
}
