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
    public void LeerlingLayout_markup_has_no_candidate_chrome()
    {
        var cut = RenderComponent<LeerlingLayout>(ps => ps
            .Add(p => p.Body, builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "body");
                builder.CloseElement();
            }));

        var html = cut.Markup;
        Assert.Contains("ll-shell", html, StringComparison.Ordinal);
        Assert.DoesNotContain("bottom-nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TrainingOffers", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("banenkaart", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("partner", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RoleFitCheck", html, StringComparison.OrdinalIgnoreCase);
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
