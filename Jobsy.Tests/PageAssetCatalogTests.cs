namespace Jobsy.Tests;

public class PageAssetCatalogTests
{
    // Keep in sync with PageAssetCatalog bundle arrays (Stijl A: tokens + stijl-a in core; illustrated public bundles).
    private const int CoreCss = 5;
    private const int PublicCss = 7;
    private const int AppCss = 21;
    private const int WerkgeverCss = 6;
    private const int BeheerCss = 2;
    private const int SalesCss = 1;
    private const int SchoolCss = 1;

    [Theory]
    [InlineData("/", CoreCss)]
    [InlineData("/login", CoreCss)]
    [InlineData("/account-maken", CoreCss)]
    [InlineData("/banenkaart", CoreCss + PublicCss)]
    [InlineData("/candidate/paspoort", CoreCss + PublicCss + AppCss)]
    [InlineData("/register", CoreCss + WerkgeverCss)]
    [InlineData("/werkgever/vacatures", CoreCss + WerkgeverCss)]
    [InlineData("/werkgever/kandidatenkaart", CoreCss + WerkgeverCss)]
    [InlineData("/admin/users", CoreCss + BeheerCss)]
    [InlineData("/sales/dashboard", CoreCss + SalesCss)]
    [InlineData("/leerling", CoreCss + SchoolCss)]
    [InlineData("/leerling/vo-paspoort", CoreCss + SchoolCss)]
    public void Route_stylesheets_use_area_bundles_not_the_full_catalog(string path, int expectedMinLinks)
    {
        var areas = Jobsy.Web.Hosting.PageAssetCatalog.ResolveAreas(path);
        var sheets = Jobsy.Web.Hosting.PageAssetCatalog.StylesheetsFor(areas);
        Assert.True(sheets.Count >= expectedMinLinks, $"Expected at least {expectedMinLinks} sheets for {path}, got {sheets.Count}");
        var maxSheets = path is "/candidate/paspoort" ? 38 : 32;
        Assert.True(sheets.Count <= maxSheets, $"Too many stylesheets ({sheets.Count}) for {path}");
        Assert.Equal(1, sheets.Count(s => s.StartsWith("css/app.min.css", StringComparison.Ordinal)));
        Assert.DoesNotContain("css/features/admin.css", sheets, StringComparer.Ordinal);
        if (path is "/login" or "/account-maken")
        {
            Assert.DoesNotContain("css/features/banenkaart.css", sheets, StringComparer.Ordinal);
        }

        if (path is "/banenkaart")
        {
            Assert.Contains(sheets, s => s.StartsWith("css/features/banenkaart.css", StringComparison.Ordinal));
            Assert.DoesNotContain(sheets, s => s.StartsWith("css/features/admin.css", StringComparison.Ordinal));
            Assert.DoesNotContain(sheets, s => s.StartsWith("css/features/vacancy-wizard.css", StringComparison.Ordinal));
        }

        if (path is "/register")
        {
            Assert.Contains(sheets, s => s.StartsWith("css/features/werkgever-aanmelding.css", StringComparison.Ordinal));
        }

        if (path is "/werkgever/kandidatenkaart")
        {
            var kkSheets = Jobsy.Web.Hosting.PageAssetCatalog.StylesheetsFor(areas, path);
            Assert.Contains(kkSheets, s => s.StartsWith("css/features/kandidatenkaart.css", StringComparison.Ordinal));
        }

        if (path.StartsWith("/leerling", StringComparison.Ordinal))
        {
            Assert.Contains(sheets, s => s.StartsWith("css/features/scholen.css", StringComparison.Ordinal));
            Assert.DoesNotContain(sheets, s => s.StartsWith("css/features/admin.css", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void App_razor_links_each_stylesheet_at_most_once()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("PageStylesheets", app, StringComparison.Ordinal);
        Assert.DoesNotMatch("href=\"css/app.min.css", app);
    }

    [Theory]
    [InlineData("/login", false, false, false, false)]
    [InlineData("/banenkaart", false, false, false, false)]
    [InlineData("/", true, false, false, false)]
    [InlineData("/leerling", false, false, true, false)]
    [InlineData("/leerling/vo-paspoort", false, false, true, false)]
    [InlineData("/werkgever/vacatures/nieuw", false, false, false, true)]
    [InlineData("/account-maken", false, true, false, false)]
    public void Page_scripts_are_gated(
        string path,
        bool landing,
        bool gratisDna,
        bool leerling,
        bool vacancyWizard)
    {
        Assert.Equal(landing, Jobsy.Web.Hosting.PageAssetCatalog.IncludeLandingScript(path));
        Assert.Equal(gratisDna, Jobsy.Web.Hosting.PageAssetCatalog.IncludeGratisDnaScript(path));
        Assert.Equal(leerling, Jobsy.Web.Hosting.PageAssetCatalog.IncludeLeerlingLoginScript(path));
        Assert.Equal(vacancyWizard, Jobsy.Web.Hosting.PageAssetCatalog.IncludeVacancyWizardScript(path));
    }

    [Theory]
    [InlineData("/login", false)]
    [InlineData("/account-maken", true)]
    [InlineData("/account-maken/code", true)]
    public void Signup_static_css_gated(string path, bool expected)
    {
        Assert.Equal(expected, Jobsy.Web.Hosting.PageAssetCatalog.IncludeSignupStaticCss(path));
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
