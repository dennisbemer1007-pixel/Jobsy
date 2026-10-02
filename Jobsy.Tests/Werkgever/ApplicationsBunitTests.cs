using Bunit;
using Jobsy.Web.Components.Werkgever.Applications;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Werkgever;

public class ApplicationsBunitTests : BunitContext
{
    public ApplicationsBunitTests()
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
    public void Drawer_Pending_shows_Kandidaat_hash_even_when_name_present()
    {
        var item = new EmployerApplicationItem
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Kas",
            CompanyName = "Naaldwijk",
            Status = "Pending",
            CandidateName = "Priya Sanders",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            MatchPercent = 92
        };

        var cut = Render<ApplicationCandidateDetail>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.Tab, "profiel"));

        Assert.Contains("Kandidaat #", cut.Markup);
        Assert.DoesNotContain("Priya", cut.Markup);
        Assert.Contains("Wat je ziet", cut.Markup);
        Assert.Contains("Anoniem profiel", cut.Markup);
    }

    [Fact]
    public void Drawer_Accepted_shows_name_and_locks_contact_row()
    {
        var item = new EmployerApplicationItem
        {
            Id = Guid.NewGuid(),
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Kas",
            CompanyName = "Naaldwijk",
            Status = "Accepted",
            CandidateName = "Priya Sanders",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            RespondedAt = DateTime.UtcNow.AddDays(-1),
            PiiRevealed = true,
            CvPdfAvailable = true,
            MatchPercent = 90
        };

        var cut = Render<ApplicationCandidateDetail>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.Tab, "profiel"));

        Assert.Contains("Priya Sanders", cut.Markup);
        Assert.Contains("is-locked", cut.Markup);
        Assert.Contains("E-mail en telefoon", cut.Markup);
    }

    [Fact]
    public void Footer_actions_per_stage_and_role()
    {
        var pending = new EmployerApplicationItem
        {
            Id = Guid.NewGuid(),
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Kas",
            CompanyName = "X",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var bm = Render<ApplicationCandidateActions>(p => p
            .Add(x => x.Item, pending)
            .Add(x => x.CanWrite, true));
        Assert.Contains("Accepteren", bm.Markup);
        Assert.Contains("Afwijzen", bm.Markup);

        var rm = Render<ApplicationCandidateActions>(p => p
            .Add(x => x.Item, pending)
            .Add(x => x.CanWrite, false));
        Assert.Contains("Reageren doet de vestigings- of bedrijfsmanager", rm.Markup);
        Assert.DoesNotContain("Accepteren", rm.Markup);
    }

    [Fact]
    public void Terminology_pages_have_no_Gematcht_or_Contact_opgenomen()
    {
        Assert.Equal("Uitgenodigd", UiStrings.Get("WgApp.Status.Invited", "nl"));
        Assert.Equal("Aangenomen", UiStrings.Get("WgApp.Status.Hired", "nl"));
        Assert.Equal("Aannemen", UiStrings.Get("WgApp.Action.Hire", "nl"));
        Assert.DoesNotContain("Gematcht", UiStrings.Get("WgApp.Col.Hired", "nl"));
        Assert.DoesNotContain("Contact opgenomen", UiStrings.Get("WgApp.Col.Invited", "nl"));
    }

    [Fact]
    public void List_sort_and_paging_constants()
    {
        Assert.Equal(25, ApplicationPipelineRules.ListPageSize);
        Assert.Equal(50, ApplicationPipelineRules.PipelineCardCap);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity())));
    }
}
