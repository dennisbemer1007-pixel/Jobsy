using Bunit;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class LandingWhyLobsyTests : BunitContext
{
    public LandingWhyLobsyTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Landing_reiskaart_markup_includes_why_lobsy_link_to_about()
    {
        Culture.InitializeFromLanguage("nl");
        var html = KreeftReiskaartMarkup.ForLandingHero(Culture);

        Assert.Contains("pub-landing__why-lobsy", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/wie-zijn-wij\"", html, StringComparison.Ordinal);
        Assert.Contains("oude schild los te laten", html, StringComparison.Ordinal);
        Assert.Contains("Waarom een kreeft?", html, StringComparison.Ordinal);
        Assert.DoesNotContain("—", html, StringComparison.Ordinal);
        Assert.DoesNotContain("%", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Landing_reiskaart_why_lobsy_english_copy()
    {
        Culture.InitializeFromLanguage("en");
        var html = KreeftReiskaartMarkup.ForLandingHero(Culture);

        Assert.Contains("letting go of its old shell", html, StringComparison.Ordinal);
        Assert.Contains("Why a lobster?", html, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new()));
    }
}
