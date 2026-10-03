using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.School;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class SchoolClassFormBunitTests : BunitContext
{
    public SchoolClassFormBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task Switching_kind_resets_year_and_changes_fields()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var level = SchoolLevel.Havo;
        var year = 2;
        var cut = Render<SchoolClassForm>(ps => ps
            .Add(p => p.ClassName, "2B")
            .Add(p => p.Level, level)
            .Add(p => p.LevelChanged, EventCallback.Factory.Create<SchoolLevel>(this, v => level = v))
            .Add(p => p.Year, year)
            .Add(p => p.YearChanged, EventCallback.Factory.Create<int>(this, v => year = v))
            .Add(p => p.ShowPupilCount, true)
            .Add(p => p.PupilCount, 28));

        Assert.Contains("Middelbare school", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Vragenlijst: Middelbare school", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("100 vragen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pauze-eiland", cut.Markup, StringComparison.Ordinal);

        var primary = cut.Find("input[value='primary']");
        await primary.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "primary" });
        cut.Render(ps => ps
            .Add(p => p.ClassName, "7A")
            .Add(p => p.Level, level)
            .Add(p => p.LevelChanged, EventCallback.Factory.Create<SchoolLevel>(this, v => level = v))
            .Add(p => p.Year, year)
            .Add(p => p.YearChanged, EventCallback.Factory.Create<int>(this, v => year = v))
            .Add(p => p.ShowPupilCount, true)
            .Add(p => p.PupilCount, 28));

        Assert.Equal(SchoolLevel.Groep78, level);
        Assert.Equal(7, year);
        Assert.Contains("Vragenlijst: Groep 7/8", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("3 puzzelpauzes", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Locked_mode_disables_kind_cards_and_shows_warning()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var cut = Render<SchoolClassForm>(ps => ps
            .Add(p => p.ClassName, "2B")
            .Add(p => p.Level, SchoolLevel.Havo)
            .Add(p => p.Year, 2)
            .Add(p => p.LevelLocked, true));

        Assert.Contains("Soort klas ligt vast", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("role=\"note\"", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(cut.Find("fieldset.sch-kind").GetAttribute("disabled"));
        Assert.True(cut.Find("input[value='primary']").HasAttribute("disabled"));
        Assert.True(cut.Find("input[value='secondary']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Radios_have_accessible_names()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var cut = Render<SchoolClassForm>(ps => ps
            .Add(p => p.ClassName, "2B")
            .Add(p => p.Level, SchoolLevel.Havo)
            .Add(p => p.Year, 2));

        var groups = cut.FindAll("[role='radiogroup']");
        Assert.NotEmpty(groups);
        Assert.Contains(groups, g => !string.IsNullOrWhiteSpace(g.GetAttribute("aria-label")));
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
