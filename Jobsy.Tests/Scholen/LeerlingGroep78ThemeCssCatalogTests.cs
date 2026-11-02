using Jobsy.Web.Hosting;

namespace Jobsy.Tests.Scholen;

public class LeerlingGroep78ThemeCssCatalogTests
{
    public static IEnumerable<object[]> ThemedRoutes =>
    [
        ["/leerling/wereld"],
        ["/leerling/start"],
        ["/leerling/reis"],
        ["/leerling/puzzel/schelpenrij"],
        ["/leerling/eiland"],
        ["/leerling/vakken"],
        ["/leerling/dit-ben-jij"],
        ["/leerling/paspoort"],
        ["/leerling/droombaan"],
    ];

    [Theory]
    [MemberData(nameof(ThemedRoutes))]
    public void RouteStylesheets_includes_g78_theme_css(string path)
    {
        Assert.True(PageAssetCatalog.IncludesLeerlingGroep78ThemeStyles(path));
        var sheets = PageAssetCatalog.RouteStylesheets(path);
        Assert.Contains(
            sheets,
            s => s.Contains("leerling-werelden-kies.css", StringComparison.Ordinal));
    }

    [Fact]
    public void Pupil_ssr_paspoort_includes_g78_passport_stylesheet_with_leading_slash()
    {
        var sheets = PageAssetCatalog.StylesheetsForPupilSsr("/leerling/paspoort");
        Assert.Contains(
            sheets,
            s => s.Contains("leerling-paspoort-g78.css", StringComparison.Ordinal)
                 && s.StartsWith("/css/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/leerling")]
    [InlineData("/leerling/stop")]
    [InlineData("/leerling/vo-paspoort")]
    public void Login_and_vo_routes_skip_g78_theme_css(string path)
    {
        Assert.False(PageAssetCatalog.IncludesLeerlingGroep78ThemeStyles(path));
        Assert.Empty(PageAssetCatalog.LeerlingGroep78ThemeStylesheets(path));
    }

    [Theory]
    [MemberData(nameof(ThemedRoutes))]
    public void StylesheetsFor_school_area_includes_g78_theme_on_journey_routes(string path)
    {
        var areas = PageAssetCatalog.ResolveAreas(path);
        var sheets = PageAssetCatalog.StylesheetsFor(areas, path);
        Assert.Contains(
            sheets,
            s => s.Contains("leerling-werelden-kies.css", StringComparison.Ordinal));
    }

    [Fact]
    public void Leerling_journey_layouts_publish_page_route_styles_head()
    {
        var root = FindRepoRoot();
        foreach (var file in new[]
                 {
                     "Jobsy.Web/Components/Layout/LeerlingJourneyLayout.razor",
                     "Jobsy.Web/Components/Layout/LeerlingLayout.razor",
                     "Jobsy.Web/Components/Layout/LeerlingGroep78ShellLayout.razor",
                 })
        {
            var src = File.ReadAllText(Path.Combine(root, file));
            Assert.Contains("<PageRouteStylesHead />", src, StringComparison.Ordinal);
            Assert.Contains("data-leerling-g78-theme-css", src, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("/leerling/vakken?page=2")]
    public void RouteStylesheets_includes_g78_theme_css_with_query(string path)
    {
        Assert.True(PageAssetCatalog.IncludesLeerlingGroep78ThemeStyles(path));
        var sheets = PageAssetCatalog.RouteStylesheets(path);
        Assert.Contains(
            sheets,
            s => s.Contains("leerling-werelden-kies.css", StringComparison.Ordinal));
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
