using Bunit;
using Jobsy.Core.Interfaces;
using Jobsy.Web.Components.Werkgever.Tokens;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Werkgever;

public class TokensBunitTests : BunitContext
{
    public TokensBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new EmployerScopeState());
    }

    [Fact]
    public void Costs_panel_renders_from_mocked_costs_not_hardcoded()
    {
        var costs = new List<TokenSpendCostItem>
        {
            new() { Reason = "Publish", CostTokens = 3 },
            new() { Reason = "PushBom", CostTokens = 5 },
        };

        var cut = Render<WgTokenCostsPanel>(p => p
            .Add(x => x.Costs, costs)
            .Add(x => x.StartOpen, true));

        var markup = cut.Markup;
        Assert.Contains("3", markup);
        Assert.Contains("5", markup);
        Assert.DoesNotContain("9,50", markup);
        Assert.Contains("Vacature publiceren", markup);
        Assert.Contains("Pushbericht naar kandidaten", markup);
    }

    [Fact]
    public void Usage_table_shows_allocate_when_allowed_and_impact_ready()
    {
        var rows = new List<WerkgeverTokenBranchUsageDto>
        {
            new(Guid.NewGuid(), "Naaldwijk", "Westland", 40, 12, 28)
        };

        var cut = Render<WgTokenUsageTable>(p => p
            .Add(x => x.Rows, rows)
            .Add(x => x.Take, 5)
            .Add(x => x.CanAllocate, true));

        Assert.Contains("Naaldwijk", cut.Markup);
        Assert.Contains("Verdelen", cut.Markup);
        Assert.Contains("28", cut.Markup);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity())));
    }
}
