using System.Security.Claims;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Web.Components.Landing;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class LandingHeroNoPercentTests : BunitContext
{
    private static readonly Regex MatchScorePercent = new(
        @"(\d{1,3}(?:[.,]\d+)?)\s*%\s*(match|past|fit|score|dopasow|potrivir)|>\s*\d{1,3}\s*%|<[^>]*>\d{1,3}%",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public LandingHeroNoPercentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Hero_radar_card_mock_fits_use_words_not_percent()
    {
        var cut = Render<LandingHeroRadarCard>(p => p.Add(x => x.Variant, LandingVariant.On));
        Assert.DoesNotMatch(MatchScorePercent, cut.Markup);
    }

    [Fact]
    public void Hero_passport_float_copy_has_no_percent()
    {
        var cut = Render<LandingHeroPassport>(p => p.Add(x => x.Variant, LandingVariant.Zw));
        Assert.DoesNotMatch(MatchScorePercent, cut.Markup);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
