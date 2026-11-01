using Jobsy.Web.Hosting;

namespace Jobsy.Tests.Scholen;

public sealed class LeerlingMboHboThemeCssCatalogTests
{
    [Theory]
    [InlineData("/leerling/mbo-hbo/uitleg")]
    [InlineData("/leerling/mbo-hbo/overzicht")]
    [InlineData("/leerling/mbo-hbo/test/interesse")]
    public void RouteStylesheets_includes_mbo_hbo_theme_css(string path)
    {
        Assert.True(PageAssetCatalog.IncludesLeerlingMboHboStyles(path));
        var sheets = PageAssetCatalog.RouteStylesheets(path);
        Assert.Contains(
            sheets,
            s => s.Contains("leerling-mbo-hbo.css", StringComparison.Ordinal));
    }
}
