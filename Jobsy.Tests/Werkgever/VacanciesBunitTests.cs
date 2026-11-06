using Bunit;
using Jobsy.Web.Components.Ui.Enterprise;
using Jobsy.Web.Components.Werkgever.Shell;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Werkgever;

public class VacanciesBunitTests : BunitContext
{
    public VacanciesBunitTests()
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
    public void EntTabs_vacatures_shows_counts_and_omits_verloopt_when_caller_filters()
    {
        var tabs = new List<EntTabs.Tab>
        {
            new("alle", "Alle (3)"),
            new("actief", "Actief (2)"),
            new("wacht", "Wacht op goedkeuring (1)"),
            new("concept", "Concept (0)"),
            new("gesloten", "Gesloten (0)"),
        };
        var cut = Render<EntTabs>(p => p
            .Add(x => x.Tabs, tabs)
            .Add(x => x.ActiveKey, "alle")
            .Add(x => x.BasePath, "/werkgever/vacatures")
            .Add(x => x.ExtraQuery, "q=oogst"));
        Assert.Contains("Alle (3)", cut.Markup);
        Assert.Contains("Wacht op goedkeuring (1)", cut.Markup);
        Assert.DoesNotContain("verloopt", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("q=oogst", cut.Markup);
    }

    [Fact]
    public void Verloopt_tab_present_when_end_date_exists()
    {
        // Guard: EndDate exists on Vacancy → tab is part of the catalog.
        Assert.NotNull(typeof(Jobsy.Core.Entities.Vacancy).GetProperty(nameof(Jobsy.Core.Entities.Vacancy.EndDate)));
        var tabs = new List<EntTabs.Tab>
        {
            new("alle", "Alle (1)"),
            new("verloopt", "Verloopt binnenkort (1)"),
        };
        var cut = Render<EntTabs>(p => p
            .Add(x => x.Tabs, tabs)
            .Add(x => x.ActiveKey, "verloopt")
            .Add(x => x.BasePath, "/werkgever/vacatures"));
        Assert.Contains("Verloopt binnenkort", cut.Markup);
    }

    [Fact]
    public void Bm_pending_row_shows_Goedkeuren_label_in_strings()
    {
        Assert.Equal("Goedkeuren", UiStrings.Get("VacancyAction.Approve", "nl"));
        Assert.Equal("Wacht op bedrijfsmanager", UiStrings.Get("WgVac.Status.PendingVm", "nl"));
    }

    [Fact]
    public void Rm_WgAction_locks_write_primary()
    {
        var scope = Services.GetRequiredService<EmployerScopeState>();
        scope.Initialize(
            EmployerRole.Regiomanager,
            [new EmployerScopeOption(EmployerScopeKind.Region, Guid.NewGuid(), "Westland")],
            new Dictionary<string, IReadOnlyList<Guid>>());
        var cut = Render<WgAction>(p => p
            .Add(x => x.RequiresWrite, true)
            .Add(x => x.Kind, WgAction.WgActionKind.Primary)
            .AddChildContent("Vacature plaatsen"));
        Assert.Contains("is-locked", cut.Markup);
        Assert.Contains("disabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntBulkBar_shows_cost_labels_from_caller()
    {
        var cut = Render<EntBulkBar>(p => p
            .Add(x => x.SelectedCount, 2)
            .AddChildContent(b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "type", "button");
                b.AddAttribute(2, "class", "btn-compact");
                b.AddContent(3, "Verlengen · 2 tokens");
                b.CloseElement();
                b.OpenElement(4, "button");
                b.AddAttribute(5, "type", "button");
                b.AddAttribute(6, "class", "btn-compact");
                b.AddContent(7, "Uitlichten · 4 tokens");
                b.CloseElement();
            }));
        Assert.Contains("Verlengen · 2 tokens", cut.Markup);
        Assert.Contains("Uitlichten · 4 tokens", cut.Markup);
        Assert.Contains("2 geselecteerd", cut.Markup);
    }

    [Fact]
    public void Confirm_copy_shows_balance_after()
    {
        var body = string.Format(
            UiStrings.Get("WgVac.Confirm.ApproveBody", "nl"),
            "3", "412", "409");
        Assert.Contains("Saldo na: 409", body);
        Assert.Contains("Saldo nu: 412", body);
    }

    [Fact]
    public void Bulk_partial_failure_summary()
    {
        var summary = string.Format(UiStrings.Get("WgVac.Bulk.Summary", "nl"), 2, 1);
        Assert.Equal("2 gelukt, 1 niet gelukt", summary);
    }

    [Fact]
    public void Terminology_dialogs_have_no_PushBom_or_Highlight_jargon_in_nl()
    {
        Assert.DoesNotContain("PushBom", UiStrings.Get("PushBom.Title", "nl"), StringComparison.Ordinal);
        Assert.DoesNotContain("Highlight", UiStrings.Get("Employer.Highlight", "nl"), StringComparison.Ordinal);
        Assert.DoesNotContain("PushBom", UiStrings.Get("VacancyAction.PushBom", "nl"), StringComparison.Ordinal);
        Assert.Equal("Uitlichten", UiStrings.Get("Employer.Highlight", "nl"));
        Assert.Equal("Zichtbaarheid & kosten", UiStrings.Get("Employer.PublishOptionsTitle", "nl"));
        Assert.Contains("Pushbericht", UiStrings.Get("PushBom.Title", "nl"), StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity())));
    }
}
