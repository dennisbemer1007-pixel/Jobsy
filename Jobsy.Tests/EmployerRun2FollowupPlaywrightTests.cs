using Bunit;
using Jobsy.Web.Components.Admin.Sections;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.Components.Werkgever;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real Chromium check for the employer-run-2 confirm dialogs and the OpenForWork prompt.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EmployerRun2FollowupPlaywrightTests : BunitContext
{
    public EmployerRun2FollowupPlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new Run2Auth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new EmployerScopeState());
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }));
    }

    [Fact]
    public async Task Browser_shows_purge_confirm_delete_button_and_one_open_for_work_prompt()
    {
        var purge = Render<AdminVacancyPurgeConfirm>(p => p
            .Add(x => x.Item, new AdminVacancyItem
            {
                Id = Guid.NewGuid(),
                Title = "Kas",
                ApplicationCount = 4
            }));
        var deleteButton = Render<VacancyConfirmProceedButton>(p => p.Add(x => x.DeleteMode, true));
        var prompt = Render<OpenForWorkPrompt>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.SetContentAsync(Wrap(purge.Markup));
        var confirm = page.GetByTestId("admin-vacancy-delete-confirm");
        await Assertions.Expect(confirm).ToBeVisibleAsync();
        var confirmText = await confirm.InnerTextAsync();
        Assert.Contains("Vacature en 4 sollicitaties definitief verwijderen?", confirmText, StringComparison.Ordinal);
        Assert.Contains("Dit kun je niet ongedaan maken.", confirmText, StringComparison.Ordinal);
        var purgeButton = page.GetByTestId("admin-vacancy-delete-confirm-btn");
        await Assertions.Expect(purgeButton).ToHaveTextAsync("Verwijderen");
        Assert.Contains("btn-compact--danger", await purgeButton.GetAttributeAsync("class"));
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Annuleren" })).ToBeVisibleAsync();

        await page.SetContentAsync(Wrap(deleteButton.Markup));
        var proceed = page.GetByTestId("wg-vac-confirm-proceed");
        await Assertions.Expect(proceed).ToHaveTextAsync("Verwijderen");
        Assert.Contains("btn-compact--danger", await proceed.GetAttributeAsync("class"));

        await page.SetContentAsync(Wrap(prompt.Markup));
        var note = page.GetByTestId("open-for-work-prompt");
        await Assertions.Expect(note).ToBeVisibleAsync();
        Assert.Equal(1, await page.GetByTestId("open-for-work-prompt").CountAsync());
        var promptText = await note.InnerTextAsync();
        Assert.Contains("Beschikbaar voor werk", promptText, StringComparison.Ordinal);
        await Assertions.Expect(page.GetByRole(AriaRole.Button)).ToBeVisibleAsync();
    }

    private static string Wrap(string body)
        => "<!DOCTYPE html><html lang=\"nl\"><body>" + body + "</body></html>";

    private sealed class Run2Auth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")],
                    "test"))));
    }
}
