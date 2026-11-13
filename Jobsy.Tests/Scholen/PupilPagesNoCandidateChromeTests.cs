using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Components.Leerling.Scene;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class PupilPagesNoCandidateChromeTests : BunitContext
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
    public void Answer_labels_are_five_likert_values_for_every_set()
    {
        var registry = new PupilQuestionSetRegistry();
        foreach (var def in registry.All)
        {
            Assert.Equal(5, def.AnswerLabels.Count);
            Assert.Equal(Enumerable.Range(1, 5), def.AnswerLabels.Select(l => l.Value));
        }
    }

    [Fact]
    public void Reis_uses_answer_scale_component_without_aria_pressed()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var reisDir = Path.Combine(root, "Jobsy.Web", "Scholen");
        var leerling = File.ReadAllText(Path.Combine(reisDir, "PupilLeerlingSsrParts.cs"));
        var groep78 = File.ReadAllText(Path.Combine(reisDir, "PupilGroep78SsrParts.cs"));
        Assert.Contains("data-ll-answer-scale", leerling, StringComparison.Ordinal);
        Assert.Contains("data-ll-answer-scale", groep78, StringComparison.Ordinal);
        Assert.Contains("role=\\\"radio\\\"", leerling + groep78, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-pressed", SliceMethod(leerling, "AnswerScaleForm"), StringComparison.Ordinal);
        Assert.DoesNotContain("aria-pressed", SliceMethod(groep78, "Groep78AnswerScaleForm"), StringComparison.Ordinal);
    }

    private static string SliceMethod(string source, string methodName)
    {
        var start = source.IndexOf($"string {methodName}", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {methodName} in SSR parts.");
        var next = source.IndexOf("\n    public static", start + 1, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
    }

    [Fact]
    public void LeerlingLobster_renders_svg_with_plates()
    {
        var cut = Render<LeerlingLobster>(ps => ps
            .Add(p => p.PlatesShed, 2)
            .Add(p => p.Size, 80));
        Assert.Contains("ll-lob", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("lobsy-256.webp", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
