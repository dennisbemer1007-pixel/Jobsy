namespace Jobsy.Tests;

public class PageRouteStylesHeadTests
{
    [Fact]
    public void MainLayout_includes_page_route_styles_head_for_enhanced_navigation()
    {
        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("<PageRouteStylesHead />", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_route_styles_head_publishes_catalog_links_in_head_content()
    {
        var head = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "PageRouteStylesHead.razor"));
        Assert.Contains("<HeadContent>", head, StringComparison.Ordinal);
        Assert.Contains("PageAssetCatalog.StylesheetsFor", head, StringComparison.Ordinal);
        Assert.Contains("data-page-route-css", head, StringComparison.Ordinal);
        Assert.Contains("LocationChanged", head, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
