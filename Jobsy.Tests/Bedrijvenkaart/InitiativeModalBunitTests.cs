using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.Bedrijvenkaart;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Bedrijvenkaart;

public class InitiativeModalBunitTests : BunitContext
{
    public InitiativeModalBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost") }));
    }

    [Fact]
    public void Knock_panel_shows_motivation_and_send_when_open()
    {
        var cut = Render<InitiativeKnockPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.CompanyName, "Bakkerij De Korenaar")
            .Add(c => c.RootCompanyId, Guid.NewGuid())
            .Add(c => c.Teams, new List<PublicInitiativeTeamOptionDto>
            {
                new(Guid.NewGuid(), "Bakkerij", "Brood", null, InitiativeMatchLabel.Good, true)
            })
            .Add(c => c.SelectedTeamId, Guid.NewGuid()));

        Assert.Contains("Klop aan bij Bakkerij De Korenaar", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Verstuur bericht", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("textarea", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bk-side-panel__textarea", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Knock_panel_team_change_uses_translated_label()
    {
        var teamId = Guid.NewGuid();
        var cut = Render<InitiativeKnockPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.CompanyName, "Bakkerij")
            .Add(c => c.RootCompanyId, Guid.NewGuid())
            .Add(c => c.Teams, new List<PublicInitiativeTeamOptionDto>
            {
                new(teamId, "Bakkerij", "Brood", null, InitiativeMatchLabel.Good, true),
                new(Guid.NewGuid(), "Winkel", "Shop", null, InitiativeMatchLabel.Good, true)
            })
            .Add(c => c.SelectedTeamId, teamId));

        Assert.Contains("Wijzigen", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Initiative.Panel.ChangeTeam", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Sent_dialog_shows_three_step_timeline()
    {
        var cut = Render<InitiativeSentDialog>(p => p.Add(c => c.IsOpen, true));
        Assert.Contains("Bericht verstuurd", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("bk-sent-stepper", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Verstuurd", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Bekeken", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Reactie", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Week_limit_dialog_shows_saved_body()
    {
        var cut = Render<InitiativeWeekLimitDialog>(p => p.Add(c => c.IsOpen, true));
        Assert.Contains("3 initiatieven deze week", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Maandag", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
