using Jobsy.Web.Features;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class PublicNavCatalogTests
{
    [Fact]
    public void Header_ON_matches_variant_matrix_and_hides_unavailable()
    {
        var items = PublicNavCatalog.Header(LandingVariant.On);
        Assert.Contains(items, i => i.Href == PublicRoutes.HowItWorks && i.IsAvailable);
        Assert.Contains(items, i => i.Href == PublicRoutes.Banenkaart && i.IsAvailable);
        Assert.Contains(items, i => i.Href == PublicRoutes.Employers && !i.IsAvailable);
        Assert.Contains(items, i => i.Href == PublicRoutes.Schools && !i.IsAvailable);
        Assert.Contains(items, i => i.Href == PublicRoutes.Partner && i.IsAvailable);

        var rendered = items.Where(i => i.IsAvailable).Select(i => i.Href).ToArray();
        Assert.Contains(PublicRoutes.Banenkaart, rendered);
        Assert.DoesNotContain(PublicRoutes.Employers, rendered);
        Assert.DoesNotContain(PublicRoutes.Schools, rendered);
    }

    [Fact]
    public void Header_OFF_has_anchors_and_never_employer_paths()
    {
        var items = PublicNavCatalog.Header(LandingVariant.Zw);
        Assert.Contains(items, i => i.Href == PublicRoutes.PassportAnchor);
        Assert.Contains(items, i => i.Href == PublicRoutes.DiscoveryAnchor);
        Assert.Contains(items, i => i.Href == PublicRoutes.HowItWorks);

        var allHrefs = items.Select(i => i.Href).ToArray();
        Assert.DoesNotContain(PublicRoutes.Banenkaart, allHrefs);
        Assert.DoesNotContain(PublicRoutes.Employers, allHrefs);
        Assert.DoesNotContain(PublicRoutes.Partner, allHrefs);
        Assert.DoesNotContain(PublicRoutes.CompanyRegister, allHrefs);
        Assert.DoesNotContain("/westland", allHrefs);
        Assert.DoesNotContain("/banen", allHrefs);
    }

    [Fact]
    public void Footer_OFF_never_contains_employer_only_links()
    {
        var hrefs = PublicNavCatalog.Footer(LandingVariant.Zw)
            .SelectMany(c => c.Items)
            .Select(i => i.Href)
            .ToArray();

        Assert.DoesNotContain(PublicRoutes.Banenkaart, hrefs);
        Assert.DoesNotContain(PublicRoutes.Employers, hrefs);
        Assert.DoesNotContain(PublicRoutes.CompanyRegister, hrefs);
        Assert.DoesNotContain(PublicRoutes.Partner, hrefs);
        Assert.DoesNotContain("/westland", hrefs);
    }
}
