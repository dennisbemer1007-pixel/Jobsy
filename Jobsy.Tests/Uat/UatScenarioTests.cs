using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Uat;

/// <summary>
/// One xUnit test per UAT grid row (UAT-0001…). Filter: <c>Suite=Uat999</c>.
/// </summary>
[Trait("Suite", "Uat999")]
public sealed class UatScenarioTests
{
    [Fact]
    public void Catalog_has_one_script_per_csv_row()
    {
        Assert.True(UatCatalog.All.Count >= 700, $"Expected the full UAT grid, got {UatCatalog.All.Count}.");
        Assert.Equal(UatCatalog.All.Count, UatCatalog.All.Select(s => s.Id).Distinct().Count());
        Assert.Contains(UatCatalog.All, s => s.Role == "Gast");
        Assert.Contains(UatCatalog.All, s => s.Role == "Kandidaat");
        Assert.Contains(UatCatalog.All, s => s.Role == "Filiaalmanager");
        Assert.Contains(UatCatalog.All, s => s.Role == "Regiomanager");
        Assert.Contains(UatCatalog.All, s => s.Role == "Bedrijfsmanager");
        Assert.Contains(UatCatalog.All, s => s.Role == "Intermediair");
        Assert.Contains(UatCatalog.All, s => s.Role == "Salesmanager");
        Assert.Contains(UatCatalog.All, s => s.Role == "Ambassadeur");
        Assert.Contains(UatCatalog.All, s => s.Role == "Admin");
    }

    [Fact]
    public void Every_bottom_nav_href_has_a_page()
    {
        var routes = RazorRouteIndex.Load();
        string[] roles =
        [
            JobsyRoles.Candidate,
            JobsyRoles.BranchManager,
            JobsyRoles.RegionalManager,
            JobsyRoles.EnterpriseManager,
            JobsyRoles.Intermediary,
            JobsyRoles.SalesManager,
            JobsyRoles.Ambassadeur,
            JobsyRoles.Admin
        ];

        foreach (var role in roles)
        {
            var items = RoleNavCatalog.ForUser(Principal(role));
            Assert.NotEmpty(items);
            foreach (var item in items)
            {
                Assert.True(
                    routes.Find(item.Href) is not null,
                    $"{role} nav href {item.Href} has no @page.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(UatCatalog.MemberData), MemberType = typeof(UatCatalog))]
    public void Script(string id, string role, string scenario)
    {
        _ = role;
        Assert.False(string.IsNullOrWhiteSpace(scenario));
        UatScriptRunner.Execute(UatCatalog.Get(id));
    }

    private static System.Security.Claims.ClaimsPrincipal Principal(string role)
    {
        var id = new System.Security.Claims.ClaimsIdentity("uat");
        id.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        return new System.Security.Claims.ClaimsPrincipal(id);
    }
}
