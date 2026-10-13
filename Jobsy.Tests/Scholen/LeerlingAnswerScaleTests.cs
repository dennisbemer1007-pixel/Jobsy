using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class LeerlingAnswerScaleTests : BunitContext
{
    private readonly PupilQuestionSetRegistry _registry = new();

    public LeerlingAnswerScaleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task G78_renders_dot_scale_without_vo_boxes()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var def = _registry.Get(PupilQuestionSet.Groep78);

        var cut = RenderScale(def, selected: 3);

        Assert.DoesNotContain("ll-answer--vo", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ll-answer__box", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(5, cut.FindAll(".ll-answer__dot").Count);
        Assert.Contains("Nee", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Niet echt", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Soms", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Best wel", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Ja!", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Vo_renders_box_scale_without_dots_or_emoji()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var def = _registry.Get(PupilQuestionSet.Vo);

        var cut = RenderScale(def, selected: 1);

        Assert.Equal(5, cut.FindAll(".ll-answer--vo").Count);
        Assert.Equal(5, cut.FindAll(".ll-answer__box").Count);
        Assert.Empty(cut.FindAll(".ll-answer__dot"));
        Assert.Contains("Klopt niet", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klopt meestal niet", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klopt deels", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klopt meestal", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klopt helemaal", cut.Markup, StringComparison.Ordinal);
        Assert.False(ContainsEmoji(cut.Markup), "VO scale must not use emoji or faces.");
    }

    [Fact]
    public async Task Radio_semantics_have_one_tab_stop_and_checked_option()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var def = _registry.Get(PupilQuestionSet.Groep78);

        var cut = RenderScale(def, selected: 2);

        var radios = cut.FindAll("[role=radio]");
        Assert.Equal(5, radios.Count);
        Assert.Equal(1, radios.Count(r => r.GetAttribute("tabindex") == "0"));
        Assert.Equal("true", radios[1].GetAttribute("aria-checked"));
        Assert.Equal(1, radios.Count(r => r.GetAttribute("aria-checked") == "true"));
        Assert.Contains("2 van 5", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Arrow_home_end_and_digit_keys_move_selection_once()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var def = _registry.Get(PupilQuestionSet.Vo);
        var chosen = new List<int>();
        int? selected = null;
        IRenderedComponent<LeerlingAnswerScale>? cut = null;

        void Bind()
        {
            cut = Render<LeerlingAnswerScale>(ps => ps
                .Add(p => p.Labels, def.AnswerLabels)
                .Add(p => p.Set, def.Set)
                .Add(p => p.Selected, selected)
                .Add(p => p.GroupLabel, "Jouw antwoord")
                .Add(p => p.OnSelect, EventCallback.Factory.Create<int>(this, v =>
                {
                    chosen.Add(v);
                    selected = v;
                })));
        }

        async Task PressAsync(string key)
        {
            Bind();
            await cut!.Find("[role=radiogroup]").KeyDownAsync(new KeyboardEventArgs { Key = key });
            Bind();
        }

        await PressAsync("ArrowRight");
        Assert.Equal(new[] { 2 }, chosen);
        await PressAsync("ArrowLeft");
        Assert.Equal(new[] { 2, 1 }, chosen);
        await PressAsync("End");
        Assert.Equal(5, selected);
        await PressAsync("Home");
        Assert.Equal(1, selected);
        chosen.Clear();
        await PressAsync("4");

        Assert.Equal(new[] { 4 }, chosen);
        Assert.Equal(4, selected);
        var radios = cut!.FindAll("[role=radio]");
        Assert.Equal("true", radios[3].GetAttribute("aria-checked"));
        Assert.Equal("0", radios[3].GetAttribute("tabindex"));
        Assert.Equal(1, radios.Count(r => r.GetAttribute("tabindex") == "0"));
    }

    private IRenderedComponent<LeerlingAnswerScale> RenderScale(PupilQuestionSetDef def, int? selected)
        => Render<LeerlingAnswerScale>(ps => ps
            .Add(p => p.Labels, def.AnswerLabels)
            .Add(p => p.Set, def.Set)
            .Add(p => p.Selected, selected)
            .Add(p => p.GroupLabel, "Jouw antwoord"));

    private static bool ContainsEmoji(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var v = rune.Value;
            if (v is (>= 0x1F300 and <= 0x1FAFF)
                or (>= 0x2600 and <= 0x27BF)
                or (>= 0x1F1E6 and <= 0x1F1FF)
                or 0xFE0F
                or 0x200D)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
