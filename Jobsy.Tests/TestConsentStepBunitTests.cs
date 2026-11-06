using System.Security.Claims;
using Bunit;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class TestConsentStepBunitTests : BunitContext
{
    public TestConsentStepBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Consent_checkbox_sets_aria_checked_and_notifies_parent()
    {
        var draft = new OnboardingProfileDraft();
        var notified = false;
        var cut = Render<TestConsentStep>(p => p
            .Add(c => c.Draft, draft)
            .Add(c => c.ConsentChanged, (bool value) => notified = value));

        var toggle = cut.Find("[role=switch]");
        Assert.Equal("false", toggle.GetAttribute("aria-checked"));

        cut.Find("input[type=checkbox]").Change(true);

        Assert.True(draft.ConsentRequested);
        Assert.True(notified);
        Assert.Equal("true", cut.Find("[role=switch]").GetAttribute("aria-checked"));
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
