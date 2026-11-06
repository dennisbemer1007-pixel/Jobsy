using Bunit;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Werkgever;
using Jobsy.Web.Components.Werkgever.Sections;
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
/// Chromium check: career-test tags stay off the talent card, the new-application
/// count is readable, and Annuleren comes before Publiceren.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EmployerRun4FollowupPlaywrightTests : BunitContext
{
    public EmployerRun4FollowupPlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new Run4Auth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new EmployerScopeState());
        Services.AddSingleton(new JobsyApiClient(new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:1/"),
            Timeout = TimeSpan.FromSeconds(2)
        }));
    }

    [Fact]
    public async Task Browser_hides_career_test_tags_and_separates_the_new_application_count()
    {
        var card = Render<TalentPoolResultCard>(p => p
            .Add(x => x.ShowRiasec, false)
            .Add(x => x.Card, new AnonymousTalentCard
            {
                RegionLabel = "Westland",
                MatchTags = ["Samenwerken", "Realistic", "Social", "Mensen helpen"],
                RiasecTags = ["Social", "Realistic"],
                HollandCode = "S",
                AvailabilitySummary = "Doordeweeks"
            }));
        var apps = Render<VacancyApplicationCount>(p => p
            .Add(x => x.Total, 1)
            .Add(x => x.NewCount, 1));
        var dialog = Render<PublishOptionsDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.Vacancy, new VacancyListItem
            {
                Id = Guid.NewGuid(),
                Title = "Oogst",
                CompanyName = "De Kas",
                CategoryHighlightAvailable = false,
                CategoryPushBomAvailable = false
            }));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(Wrap(card.Markup + apps.Markup + dialog.Markup));

        var tags = await page.GetByTestId("talent-tags").InnerTextAsync();
        Assert.Contains("Samenwerken", tags, StringComparison.Ordinal);
        Assert.DoesNotContain("Realistic", tags, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Social", tags, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mensen helpen", tags, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await page.GetByTestId("talent-holland").CountAsync());
        var cardText = await page.GetByTestId("talent-card").InnerTextAsync();
        Assert.DoesNotContain("Holland", cardText, StringComparison.OrdinalIgnoreCase);

        var cell = page.GetByTestId("wg-vac-apps");
        var cellText = await cell.InnerTextAsync();
        Assert.Contains("1 · 1 nieuw", cellText, StringComparison.Ordinal);
        Assert.Contains(" · ", cellText, StringComparison.Ordinal);
        var badge = page.Locator(".wg-vac__new");
        await Assertions.Expect(badge).ToHaveAttributeAsync("aria-label", "1 nieuwe sollicitatie");
        await Assertions.Expect(badge).ToHaveAttributeAsync("title", "1 nieuwe sollicitatie");

        var cancel = page.GetByTestId("publish-cancel");
        var confirm = page.GetByTestId("publish-confirm");
        await Assertions.Expect(cancel).ToHaveTextAsync("Annuleren");
        await Assertions.Expect(confirm).ToHaveTextAsync("Publiceren");
        var cancelBox = await cancel.BoundingBoxAsync();
        var confirmBox = await confirm.BoundingBoxAsync();
        Assert.NotNull(cancelBox);
        Assert.NotNull(confirmBox);
        Assert.True(cancelBox.X < confirmBox.X, "Annuleren must come before Publiceren.");
    }

    private static string Wrap(string body)
        => "<!DOCTYPE html><html lang=\"nl\"><body>" + body + "</body></html>";

    private sealed class Run4Auth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "BranchManager")],
                    "test"))));
    }
}
