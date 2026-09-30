using Bunit;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class KbHiddenModeBunitTests : TestContext
{
    public KbHiddenModeBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void TravelTime_bureau_without_region_uses_simple_fallback_label()
    {
        var cut = RenderComponent<KbTravelTime>(p => p
            .Add(x => x.Minutes, 14)
            .Add(x => x.Transport, "Fiets")
            .Add(x => x.ToBureau, true));

        Assert.Contains("kb-travel--bureau", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Reistijd tot het bureau", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("werklocatie in de regio", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BadgeRow_still_works_without_engagement_badges_dep_b_absent()
    {
        // Dep B ABSENT: no engagement chips — fit + Staat lager only, within D7 budget.
        var badges = new List<KbBadgeItem>
        {
            new("fit", "fit", IsFitPill: true),
            new("rank", "Staat lager: nachtdienst", "rank-lower"),
        };
        var fit = new KbFitView(true, 76, Jobsy.Core.Rules.KbFitBand.Strong);
        var cut = RenderComponent<KbBadgeRow>(p => p
            .Add(x => x.Badges, badges)
            .Add(x => x.MaxVisible, 2)
            .Add(x => x.Fit, fit));

        Assert.Contains("76% past bij jou", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Staat lager: nachtdienst", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-badges__more", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
