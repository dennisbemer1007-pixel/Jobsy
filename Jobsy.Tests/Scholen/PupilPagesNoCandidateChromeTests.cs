using Bunit;
using Jobsy.Core.Scholen;
using Jobsy.Web.Components.Leerling.Scene;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class PupilPagesNoCandidateChromeTests : TestContext
{
    public PupilPagesNoCandidateChromeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void LeerlingLayout_css_block_exists_without_candidate_chrome_in_source()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "LeerlingLayout.razor"));
        Assert.Contains("ll-shell", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("BottomNav", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("TrainingOffers", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("banenkaart", layout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ll-shell", File.ReadAllText(
            Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css")), StringComparison.Ordinal);
    }

    [Fact]
    public void Answer_labels_are_five_likert_values()
    {
        Assert.Equal(5, PupilWorldCatalog.AnswerLabels.Count);
        Assert.Equal(1, PupilWorldCatalog.AnswerLabels[0].Value);
        Assert.Equal(5, PupilWorldCatalog.AnswerLabels[4].Value);
    }

    [Fact]
    public void LeerlingLobster_renders_svg_with_plates()
    {
        var cut = RenderComponent<LeerlingLobster>(ps => ps
            .Add(p => p.PlatesShed, 2)
            .Add(p => p.Size, 80));
        Assert.Contains("ll-lob", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("mascot-256", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
