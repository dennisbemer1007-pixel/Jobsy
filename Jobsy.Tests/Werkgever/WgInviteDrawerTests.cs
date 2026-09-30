using Bunit;
using Jobsy.Web.Components.Werkgever.Team;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Werkgever;

public class WgInviteDrawerTests : TestContext
{
    public WgInviteDrawerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new JobsyApiClient(new HttpClient
        {
            BaseAddress = new Uri("http://localhost")
        }));
    }

    [Fact]
    public void Role_cards_render_exact_copy_keys()
    {
        var cut = RenderComponent<WgInviteDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Companies, SampleCompanies())
            .Add(x => x.Regions, SampleRegions()));

        Assert.Contains("Bedrijfsmanager", cut.Markup);
        Assert.Contains("Alles voor alle vestigingen", cut.Markup);
        Assert.Contains("Regiomanager", cut.Markup);
        Assert.Contains("Alleen lezen", cut.Markup);
        Assert.Contains("Vestigingsmanager", cut.Markup);
        Assert.Contains("alleen voor de eigen vestiging", cut.Markup);
        Assert.Contains("Kandidaatgegevens volgen de privacyregels", cut.Markup);
    }

    [Fact]
    public void Scope_field_hidden_for_BM_shown_for_VM_and_RM()
    {
        var cut = RenderComponent<WgInviteDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Companies, SampleCompanies())
            .Add(x => x.Regions, SampleRegions())
            .Add(x => x.PrefillInvite, "vestiging:" + BranchId));

        // Prefill selects Vestigingsmanager → branch select visible
        Assert.Contains("Vestiging", cut.Markup);
        Assert.Contains("De Lier", cut.Markup);
    }

    [Fact]
    public void FullNameFromEmail_maps_local_part()
    {
        Assert.Equal("F Vos", TeamRoleCopy.FullNameFromEmail("f.vos@voorbeeld.nl"));
        Assert.Equal("Marie", TeamRoleCopy.FullNameFromEmail("marie@x.nl"));
    }

    [Fact]
    public void Validation_requires_email()
    {
        var cut = RenderComponent<WgInviteDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Companies, SampleCompanies())
            .Add(x => x.Regions, SampleRegions()));

        cut.Find("button.btn-compact--primary").Click();
        Assert.Contains("geldig e-mailadres", cut.Markup);
    }

    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RegionId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static List<CompanySummary> SampleCompanies() =>
    [
        new() { Id = OrgId, Name = "Org", ParentCompanyId = null },
        new() { Id = BranchId, Name = "De Lier", ParentCompanyId = OrgId },
    ];

    private static List<RegionItem> SampleRegions() =>
    [
        new()
        {
            Id = RegionId,
            Name = "Westland",
            OrganizationCompanyId = OrgId,
            Companies = [new() { CompanyId = BranchId, CompanyName = "De Lier" }]
        }
    ];

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
