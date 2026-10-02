using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
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
        Assert.Empty(RoleNavCatalog.Admin);
        Assert.Empty(RoleNavCatalog.SalesManager);
        Assert.Equal(4, RoleNavCatalog.Ambassadeur.Length);
        Assert.Equal("/candidate/liked", RoleNavCatalog.Candidate[1].Href);
        Assert.Equal("/banenkaart", RoleNavCatalog.Candidate[0].Href);

        var passportOn = RoleNavCatalog.CandidateItems(new FeatureFlagSnapshot(true, true));
        Assert.Equal("Nav.Search", passportOn[2].TitleKey);
        Assert.Equal("/banenkaart", passportOn[2].Href);
        Assert.Equal("/candidate/applications", passportOn[3].Href);
        Assert.Equal("/carriere", passportOn[4].Href);
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
