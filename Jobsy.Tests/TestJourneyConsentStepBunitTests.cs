using System.Security.Claims;
using Bunit;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class TestJourneyConsentStepBunitTests : BunitContext
{
    public TestJourneyConsentStepBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Flag_off_renders_classic_test_consent_step()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ITestJourneyIllustrationGate, TestJourneyIllustrationGate>();

        var cut = Render<TestConsentStep>(p => p
            .Add(c => c.Draft, new OnboardingProfileDraft())
            .Add(c => c.ConsentChanged, (bool _) => { }));

        Assert.Contains("Nu even over jou", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"test-journey-consent\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Flag_on_renders_journey_headline_and_station_checks()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TestJourneyIllustrationEnabled"] = "true" })
            .Build());
        Services.AddSingleton<ITestJourneyIllustrationGate, TestJourneyIllustrationGate>();

        var cut = Render<TestJourneyConsentStep>(p => p
            .Add(c => c.CompletedSteps, new[] { 7, 8 })
            .Add(c => c.NextStep, 9));

        Assert.Contains("data-testid=\"test-journey-consent\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("In ±6 minuten ontdek je wat bij je past", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("ob-journey__item--done", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("ob-journey__item--next", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("ob-journey__stations--grid", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ob-journey__badge", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("~1,5 min", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Journey_consent_footer_gates_parent_until_checked()
    {
        var draft = new OnboardingProfileDraft();
        var cut = Render<TestConsentFooterFields>(p => p
            .Add(c => c.Draft, draft)
            .Add(c => c.ConsentChanged, (bool value) => draft.ConsentRequested = value));

        Assert.False(draft.ConsentRequested);
        cut.Find("input[type=checkbox]").Change(true);
        Assert.True(draft.ConsentRequested);
    }

    [Theory]
    [InlineData("nl", "Start met test 1")]
    [InlineData("en", "Start with test 1")]
    public void Journey_cta_strings_localized(string lang, string expectedStart)
    {
        Assert.Equal(expectedStart, UiStrings.Get("Onboarding.Journey.CtaStart", lang));
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
