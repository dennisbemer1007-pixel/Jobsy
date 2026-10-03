using Bunit;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class LeerlingCheerToastTests : BunitContext
{
    public LeerlingCheerToastTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Toast_is_polite_live_region_when_shown()
    {
        var cut = Render<LeerlingCheerToast>(ps => ps
            .Add(p => p.Text, "Goed bezig!")
            .Add(p => p.Show, true)
            .Add(p => p.VisibleMs, 60_000));

        var toast = cut.Find("[data-testid=ll-cheer-toast]");
        Assert.Equal("status", toast.GetAttribute("role"));
        Assert.Equal("polite", toast.GetAttribute("aria-live"));
        Assert.Contains("Goed bezig!", toast.TextContent, StringComparison.Ordinal);
        Assert.Contains("ll-cheer-toast", toast.GetAttribute("class") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void Hidden_toast_is_not_in_the_dom()
    {
        var cut = Render<LeerlingCheerToast>(ps => ps
            .Add(p => p.Text, "Goed bezig!")
            .Add(p => p.Show, false));

        Assert.Empty(cut.FindAll("[data-testid=ll-cheer-toast]"));
    }

    [Fact]
    public async Task Toast_hides_after_visible_ms()
    {
        var hidden = 0;
        var cut = Render<LeerlingCheerToast>(ps => ps
            .Add(p => p.Text, "Goed bezig!")
            .Add(p => p.Show, true)
            .Add(p => p.VisibleMs, 40)
            .Add(p => p.OnHidden, EventCallback.Factory.Create(this, () => hidden++)));

        Assert.Single(cut.FindAll("[data-testid=ll-cheer-toast]"));
        await Task.Delay(200);
        Assert.Equal(1, hidden);
    }

    [Fact]
    public void Reis_wires_mobile_toast_and_desktop_bubble()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var reis = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leerling", "LeerlingReis.razor"));
        Assert.Contains("<LeerlingCheerToast", reis, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"ll-cheer-desktop\"", reis, StringComparison.Ordinal);
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css"));
        Assert.Contains(".ll-cheer-toast", css, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 899px)", css, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", css, StringComparison.Ordinal);
        Assert.Contains(".ll-cheer-toast { animation: none; }", css, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
