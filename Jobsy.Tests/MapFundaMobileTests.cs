namespace Jobsy.Tests;

/// <summary>
/// Source guards for the Funda-style mobile banenkaart (k01–k04).
/// </summary>
public class MapFundaMobileTests
{
    [Fact]
    public void MapBottomSheet_exposes_three_snap_points_and_history_back()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Map", "MapBottomSheet.razor"));
        Assert.Contains("enum Snap", razor);
        Assert.Contains("Collapsed = 0", razor);
        Assert.Contains("Card = 1", razor);
        Assert.Contains("Half = 2", razor);
        Assert.Contains("Full = 3", razor);
        Assert.Contains("OnBrowserBack", razor);
        Assert.Contains("mapBottomSheet.pushHistory", razor);
        Assert.Contains("mapBottomSheet.setSnap", razor);
        Assert.Contains("OnJsSnapChanged", razor);
        Assert.Contains("aria-label", razor);

        var sheetJs = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "mapBottomSheet.js"));
        Assert.Contains("translate3d", sheetJs);
        Assert.Contains("prefers-reduced-motion", sheetJs);
        Assert.Contains("pointerdown", sheetJs);
        Assert.Contains("setPointerCapture", sheetJs);
    }

    [Fact]
    public void VacancyCompactCard_has_skeleton_and_compact_fields()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Map", "VacancyCompactCard.razor"));
        Assert.Contains("vac-compact--skeleton", razor);
        Assert.Contains("vac-compact__title", razor);
        Assert.Contains("vac-compact__chip--travel", razor);
        Assert.Contains("vac-compact__chip--match", razor);
        Assert.Contains("% match", razor);
        Assert.Contains("vac-compact__wage", razor);
        Assert.Contains("vac-compact__save", razor);
        Assert.Contains("loading=\"lazy\"", razor);
        Assert.Contains("role=\"button\"", razor);
        Assert.Contains("min-height", File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css")));
    }

    [Fact]
    public void Discovery_mobile_chrome_is_funda_fullscreen_map()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("jobsy-chrome__float", discovery);
        Assert.Contains("MapBottomSheet", discovery);
        Assert.Contains("vacatures in dit gebied", discovery);
        Assert.Contains("Veeg omhoog voor de lijst", discovery);
        Assert.Contains("vacatures in beeld", discovery);
        Assert.Contains("Kortste reistijd", discovery);
        Assert.Contains("fundaMobile", discovery);
        Assert.Contains("OpenMobileListSheetAsync", discovery);
        Assert.Contains("_wideViewport", discovery);
        Assert.Contains("highlight-carousel--map", discovery);
        Assert.Contains("Voorbeelddata", discovery);
        Assert.Contains("ShowSampleDataBadge", discovery);
        Assert.Contains("alle banen", discovery);
        Assert.Contains("map-sheet-card-host", discovery);
        // Card/cluster hosts stay mounted for instant skeleton (no Blazor wipe race).
        Assert.Contains("Hosts stay mounted", discovery);
        // Carousel only on wide (desktop) map pane.
        Assert.Contains("&& _wideViewport", discovery);
    }

    [Fact]
    public void JobMap_funda_mobile_uses_sheet_not_html_markers()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("function openMobileCard", js);
        Assert.Contains("function openMobileCluster", js);
        Assert.Contains("function applyCalmMobileStyle", js);
        Assert.Contains("function setSheetPadding", js);
        Assert.Contains("getViewportPinIds", js);
        Assert.Contains("publishViewportCount", js);
        Assert.Contains("setTimeout(publishViewportCount, 200)", js);
        Assert.Contains("#f54a1b", js);
        Assert.Contains("jobsy-pins-halo", js);
        Assert.Contains("compactCardSkeletonHtml", js);
        Assert.Contains("paintCardIntoHost", js);
        Assert.Contains("% match", js);
        Assert.Contains("fetchVacancyCards", js);
        Assert.Contains("setFeatureState", js);
        Assert.Contains("dimmed", js);
        Assert.Contains("clusterOn", js);
        Assert.DoesNotContain("L.marker", js);
        // Pin layers are native GeoJSON — no HTML Marker for vacancy pins.
        Assert.Contains("PIN_LAYER_UNCLUSTERED", js);
        Assert.Contains("jobsy-pins", js);
    }

    [Fact]
    public void Compact_card_css_meets_44px_tap_targets()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(".vac-compact__save {\n    flex-shrink: 0;\n    width: 44px;\n    height: 44px;", css);
        Assert.Contains(".map-sheet__panel", css);
        Assert.Contains("jobsy-discovery.show-map .jobsy-chrome__header", css);
        Assert.Contains("background: var(--brand)", css);
        Assert.Contains("jobsy-discovery.show-map .highlight-carousel--map", css);
        Assert.Contains("display: none !important", css);
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
