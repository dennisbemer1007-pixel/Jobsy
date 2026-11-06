using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
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

        var model = new SchoolClassFormSsrHtml.FormModel
        {
            ClassName = "2B",
            Kind = SchoolClassFormSsrHtml.KindVo,
            Level = SchoolLevel.Havo,
            Year = 2,
            ShowPupilCount = true,
            PupilCount = 28,
        };
        var voMarkup = RenderFormHtml(model, culture);
        Assert.Contains("Middelbare school", voMarkup, StringComparison.Ordinal);
        Assert.Contains("Vragenlijst: Middelbare school", voMarkup, StringComparison.Ordinal);
        Assert.Contains("100 vragen", voMarkup, StringComparison.Ordinal);
        Assert.Contains("Pauze-eiland", voMarkup, StringComparison.Ordinal);

        model.Kind = SchoolClassFormSsrHtml.KindPrimary;
        model.Level = SchoolLevel.Groep78;
        model.Year = 7;
        var primaryMarkup = RenderFormHtml(model, culture);
        Assert.Contains("Vragenlijst: Groep 7/8", primaryMarkup, StringComparison.Ordinal);
        Assert.Contains("60 vragen", primaryMarkup, StringComparison.Ordinal);
        Assert.Contains("1 pauze-eiland", primaryMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", primaryMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Locked_mode_disables_kind_cards_and_shows_warning()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var model = new SchoolClassFormSsrHtml.FormModel
        {
            ClassName = "2B",
            Level = SchoolLevel.Havo,
            Year = 2,
            LevelLocked = true,
        };
        var markup = RenderFormHtml(model, culture);
        Assert.Contains("Soort klas ligt vast", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"note\"", markup, StringComparison.Ordinal);
        Assert.Contains("<fieldset class=\"sch-kind\" disabled>", markup, StringComparison.Ordinal);
        Assert.Contains("input type=\"radio\" name=\"kind\" value=\"primary\"", markup, StringComparison.Ordinal);
        Assert.Contains(" disabled", markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Radios_have_accessible_names()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var markup = RenderFormHtml(new SchoolClassFormSsrHtml.FormModel
        {
            ClassName = "2B",
            Level = SchoolLevel.Havo,
            Year = 2,
        }, culture);
        Assert.Contains("role=\"radiogroup\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=", markup, StringComparison.Ordinal);
    }

    private static string RenderFormHtml(SchoolClassFormSsrHtml.FormModel model, CultureState culture)
        => SchoolClassFormSsrHtml.Build(
            model,
            null,
            "/school/klassen",
            "/school/klassen",
            "",
            culture);

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
