using Bunit;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class PersonalStepAddressSuggestionsBunitTests : BunitContext
{
    public PersonalStepAddressSuggestionsBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient, FakeGeocoder>();
    }

    [Fact]
    public void Prefilled_resolved_address_does_not_show_suggestion_panel()
    {
        var draft = new OnboardingProfileDraft
        {
            FirstName = "Alex",
            LastName = "Jansen",
            AddressQuery = "2671 GB Naaldwijk",
            HomeLat = 51.99,
            HomeLng = 4.21,
            PostcodeResolved = true,
            Postcode = "2671GB",
            City = "Naaldwijk",
        };

        var cut = Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.ShowValidation, false));

        Assert.Empty(cut.FindAll(".address-suggestions"));
    }

    [Fact]
    public async Task User_typed_query_opens_list_then_blur_and_escape_close_it()
    {
        var draft = new OnboardingProfileDraft
        {
            FirstName = "Alex",
            LastName = "Jansen",
        };

        var cut = Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.ShowValidation, false));

        var input = cut.Find("#onboarding-home-address");
        input.Input("Kerkstraat Naaldwijk");
        await cut.InvokeAsync(async () => await Task.Delay(450));

        cut.Find(".address-suggestions");
        Assert.NotEmpty(cut.FindAll(".address-suggestions__item"));

        input.Blur(new FocusEventArgs());
        await cut.InvokeAsync(async () => await Task.Delay(200));
        Assert.Empty(cut.FindAll(".address-suggestions"));

        var inputAgain = cut.Find("#onboarding-home-address");
        inputAgain.Focus(new FocusEventArgs());
        inputAgain.Input("Kerkstraat Naaldwijk");
        await cut.InvokeAsync(async () => await Task.Delay(450));
        cut.Find(".address-suggestions");

        cut.Find("#onboarding-home-address").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        await cut.InvokeAsync(() => Task.CompletedTask);
        Assert.Empty(cut.FindAll(".address-suggestions"));
    }

    [Fact]
    public async Task Selecting_suggestion_closes_list()
    {
        var draft = new OnboardingProfileDraft
        {
            FirstName = "Alex",
            LastName = "Jansen",
        };

        var cut = Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.ShowValidation, false));

        cut.Find("#onboarding-home-address").Input("Kerkstraat Naaldwijk");
        await cut.InvokeAsync(async () => await Task.Delay(450));

        cut.Find(".address-suggestions__item").Click();
        await cut.InvokeAsync(() => Task.CompletedTask);
        Assert.Empty(cut.FindAll(".address-suggestions"));
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeGeocoder : IGeocodingClient
    {
        public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            if ((query ?? "").Trim().Length < 3)
            {
                return [];
            }

            return
            [
                new AddressSuggestion("Kerkstraat, 2671 Naaldwijk", 51.994, 4.205),
            ];
        }

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("2671 GB Naaldwijk");
    }
}
