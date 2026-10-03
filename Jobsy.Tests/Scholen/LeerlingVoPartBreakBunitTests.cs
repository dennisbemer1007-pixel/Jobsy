using Bunit;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Components.Pages.Leerling;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class LeerlingVoPartBreakBunitTests : BunitContext
{
    public LeerlingVoPartBreakBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task Part_break_panel_renders_continue_and_stop_part1()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var cut = Render<LeerlingVoPartBreak>();
        Assert.Contains("role=\"status\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Deel 1 is klaar!", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Verder met deel 2", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Stoppen voor nu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("name=\"part\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("value=\"1\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("action=\"/leerling/stop\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/leerling/reis\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stop_page_deel1_shows_vuurtoren_copy()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/leerling/stop?done=deel1");
        var deel1 = Render<LeerlingStop>();
        Assert.Contains("vuurtoren", deel1.Markup, StringComparison.OrdinalIgnoreCase);

        nav.NavigateTo("/leerling/stop?done=1");
        var other = Render<LeerlingStop>();
        Assert.DoesNotContain("vuurtoren", other.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dezelfde code", other.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
