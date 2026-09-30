using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Werkgever;

public class RoleNavCatalogEmployerEmptyTests
{
    [Fact]
    public void Employer_catalogs_are_empty()
    {
        Assert.Empty(RoleNavCatalog.Enterprise);
        Assert.Empty(RoleNavCatalog.Regional);
        Assert.Empty(RoleNavCatalog.Branch);
        Assert.Empty(RoleNavCatalog.Intermediary);
    }

    [Fact]
    public void Candidate_admin_sales_ambassadeur_unchanged_snapshot()
    {
        Assert.Equal(5, RoleNavCatalog.Candidate.Length);
        Assert.Equal(7, RoleNavCatalog.Admin.Length);
        Assert.Equal(5, RoleNavCatalog.SalesManager.Length);
        Assert.Equal(4, RoleNavCatalog.Ambassadeur.Length);
        Assert.Equal("/candidate/liked", RoleNavCatalog.Candidate[1].Href);
        Assert.Equal("/admin/vacancies", RoleNavCatalog.Admin[2].Href);
    }

    [Fact]
    public void ForUser_employer_returns_empty()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, JobsyRoles.EnterpriseManager)
        ], "test"));
        Assert.Empty(RoleNavCatalog.ForUser(user));
    }
}
