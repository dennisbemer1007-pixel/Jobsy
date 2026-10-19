using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public sealed class OnboardingPersonalStepInteractivityBunitTests : BunitContext
{
    public OnboardingPersonalStepInteractivityBunitTests()
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
    public void Work_status_chip_toggles_selection_on_click()
    {
        var draft = new OnboardingProfileDraft
        {
            FirstName = "Alex",
            LastName = "Jansen",
        };

        var cut = Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.Illustrated, true)
            .Add(p => p.ShowValidation, false));

        var chip = cut.Find(".work-status-chips .lobsy-choice-tile");
        Assert.Equal("false", chip.GetAttribute("aria-pressed"));

        chip.Click();
        Assert.Equal("true", chip.GetAttribute("aria-pressed"));
        Assert.Contains(WorkStatusCatalog.AllCodes[0], draft.WorkStatuses, StringComparer.OrdinalIgnoreCase);

        chip.Click();
        Assert.Equal("false", chip.GetAttribute("aria-pressed"));
        Assert.DoesNotContain(WorkStatusCatalog.AllCodes[0], draft.WorkStatuses);
    }

    [Fact]
    public void Illustrated_personal_step_renders_date_field_with_picker_button()
    {
        var draft = new OnboardingProfileDraft { FirstName = "A", LastName = "B" };
        var cut = Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.Illustrated, true));

        cut.Find("[data-testid=onboarding-birth-date] .jobsy-date-field__picker-btn");
        cut.Find("[data-testid=onboarding-birth-date] .jobsy-date-field__input--native");
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeGeocoder : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }
}
