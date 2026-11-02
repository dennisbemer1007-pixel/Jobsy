using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public sealed class PupilGroep78RouteSsrHtmlTests : BunitContext
{
    public PupilGroep78RouteSsrHtmlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Theory]
    [InlineData(PupilJourneyTheme.ZeeExpeditie)]
    [InlineData(PupilJourneyTheme.Eilandtuin)]
    [InlineData(PupilJourneyTheme.Sterrenkust)]
    public void Route_overlay_svg_transform_attributes_are_well_formed(string themeKey)
    {
        var culture = Services.GetRequiredService<CultureState>();
        culture.InitializeFromLanguage("nl");
        var def = new PupilQuestionSetRegistry().Get(PupilQuestionSet.Groep78);
        var html = PupilGroep78RouteSsrHtml.BuildBody(def, culture, themeKey);

        var svg = Regex.Match(html, @"<svg[^>]*class=""rk-overlay""[^>]*>(.*?)</svg>", RegexOptions.Singleline);
        Assert.True(svg.Success, "rk-overlay svg missing");

        foreach (Match m in Regex.Matches(svg.Groups[1].Value, @"transform=""([^""]*)"""))
        {
            var value = m.Groups[1].Value;
            Assert.True(value.EndsWith(")", StringComparison.Ordinal), $"Malformed SVG transform (missing ')'): {value}");
            Assert.False(Regex.IsMatch(value, "[0-9],[0-9]"), $"Decimal-comma in SVG transform: {value}");
        }

        Assert.Equal(3, Regex.Matches(svg.Groups[1].Value, @"transform=""rotate\(45 \d+ \d+\)""").Count);
        Assert.DoesNotMatch(@"transform=""rotate\(-45", svg.Groups[1].Value);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
