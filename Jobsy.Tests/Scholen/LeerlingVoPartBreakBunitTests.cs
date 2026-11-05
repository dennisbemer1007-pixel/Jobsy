using Bunit;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

public class LeerlingVoPartBreakBunitTests : JobsyBunitContext

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
        Assert.Contains("action=\"/leerling/pauze\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/leerling/reis\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stop_ssr_deel1_uses_vo_part1_body_key()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var stop = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilStopSsrHtml.cs"));
        var frame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingSsrFrame.cs"));
        Assert.Contains("Leerling.Vo.Stop.Part1Body", stop, StringComparison.Ordinal);
        Assert.Contains("Leerling.Stop.AskTitle", stop, StringComparison.Ordinal);
        Assert.Contains("/leerling/pauze", frame, StringComparison.Ordinal);
        _ = culture["Leerling.Stop.Title"];
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
