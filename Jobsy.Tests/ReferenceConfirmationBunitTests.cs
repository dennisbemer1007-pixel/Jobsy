using Bunit;
using Bunit.TestDoubles;
using Jobsy.Web.Components.Pages.Public;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class ReferenceConfirmationBunitTests : BunitContext
{
    public ReferenceConfirmationBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuth()));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
    }

    [Fact]
    public void Referee_form_shows_five_questions_decline_and_misuse_in_dutch()
    {
        var cut = Render<RefereeQuestions>(parameters => parameters
            .Add(p => p.Token, new string('a', 64))
            .Add(p => p.CandidateName, "Sam")
            .Add(p => p.RoleTitle, "vakkenvuller"));

        var markup = cut.Markup;
        Assert.Contains("Klopt het dat Sam bij jullie werkte als vakkenvuller?", markup, StringComparison.Ordinal);
        Assert.Contains("Van wanneer tot wanneer ongeveer?", markup, StringComparison.Ordinal);
        Assert.Contains("Wat deed Sam goed?", markup, StringComparison.Ordinal);
        Assert.Contains("Zou je opnieuw met Sam willen werken?", markup, StringComparison.Ordinal);
        Assert.Contains("Wil je nog iets kwijt?", markup, StringComparison.Ordinal);
        Assert.Contains("Nee, ik doe niet mee", markup, StringComparison.Ordinal);
        Assert.Contains("Melding van misbruik", markup, StringComparison.Ordinal);
        Assert.Contains("Lobsy vraagt dit omdat deze persoon jou noemde.", markup, StringComparison.Ordinal);
        Assert.Contains("data-referee-form", markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
