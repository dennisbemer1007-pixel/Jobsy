using Bunit;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.Services;
using Jobsy.Web.Components.Werkgever.Applications;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real Chromium check: employer applicant detail has no match %, and a candidate
/// with OpenForWork off sees the enable prompt instead of a fit percent and no apply control.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EmployerRolesFixlistPlaywrightTests : BunitContext
{
    public EmployerRolesFixlistPlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FixlistAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new EmployerScopeState());
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }));
    }

    [Fact]
    public async Task Browser_hides_match_percent_and_blocks_apply_until_open_for_work()
    {
        var employer = Render<ApplicationCandidateDetail>(p => p
            .Add(x => x.Item, new EmployerApplicationItem
            {
                Id = Guid.NewGuid(),
                VacancyId = Guid.NewGuid(),
                VacancyTitle = "Kas",
                CompanyName = "Naaldwijk",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            })
            .Add(x => x.Tab, "profiel"));

        var candidate = Render<OpenForWorkPrompt>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.SetContentAsync(Wrap(employer.Markup));
        var employerText = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("%", employerText, StringComparison.Ordinal);
        Assert.DoesNotContain("match", employerText, StringComparison.OrdinalIgnoreCase);

        await page.SetContentAsync(Wrap(candidate.Markup));
        var prompt = page.GetByTestId("open-for-work-prompt");
        await Assertions.Expect(prompt).ToBeVisibleAsync();
        var promptText = await prompt.InnerTextAsync();
        Assert.Contains("Beschikbaar voor werk", promptText, StringComparison.Ordinal);
        Assert.DoesNotContain("% past bij jou", promptText, StringComparison.Ordinal);
        Assert.Equal(0, await page.Locator("a, button.btn--primary").CountAsync());
    }

    private static string Wrap(string body)
        => "<!DOCTYPE html><html lang=\"nl\"><body>" + body + "</body></html>";

    private sealed class FixlistAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "BranchManager")],
                    "test"))));
    }
}
