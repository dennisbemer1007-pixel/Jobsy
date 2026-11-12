using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// Admin UI consistency: stylesheets per route and platform settings save mapping.
/// </summary>
public class AdminUiPolishTests
{
    [Theory]
    [InlineData("/admin/scholen")]
    [InlineData("/admin/scholen/rapportage")]
    public void Admin_scholen_routes_include_scholen_stylesheet(string path)
    {
        var areas = PageAssetCatalog.ResolveAreas(path);
        var sheets = PageAssetCatalog.StylesheetsFor(areas, path);
        Assert.Contains(
            WwwrootAssetCatalog.Url("css/features/admin.css"),
            sheets,
            StringComparer.Ordinal);
        Assert.Contains(
            WwwrootAssetCatalog.Url("css/features/scholen.css"),
            sheets,
            StringComparer.Ordinal);
    }

    [Fact]
    public void Admin_css_includes_beheer_filter_and_drawer_polish()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/admin.css"));
        Assert.Contains(".admin-main .filter-bar", css, StringComparison.Ordinal);
        Assert.Contains(".admin-main .admin-drawer__body label", css, StringComparison.Ordinal);
        Assert.Contains(".admin-main .sch-kpi-row", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_settings_to_patch_includes_coach_tone_preferences()
    {
        var editor = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Components/Admin/Sections/PlatformSettingsEditor.razor"));
        Assert.Contains("CandidateCoachTonePreferencesEnabled = u.CandidateCoachTonePreferencesEnabled", editor, StringComparison.Ordinal);
        Assert.Contains("catch (InvalidOperationException ex)", editor, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("PlatformSettingsCatalog.GeneralGroupKeys")]
    [InlineData("PlatformSettingsCatalog.FeaturesGroupKeys")]
    public void Settings_editor_wires_catalog_group_keys(string groupKeysSymbol)
    {
        var algemeen = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Admin/CompanySettingsAdmin.razor"));
        var functies = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Admin/SettingsAdmin.razor"));
        if (groupKeysSymbol.Contains("General", StringComparison.Ordinal))
        {
            Assert.Contains(groupKeysSymbol, algemeen, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains(groupKeysSymbol, functies, StringComparison.Ordinal);
        }
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
