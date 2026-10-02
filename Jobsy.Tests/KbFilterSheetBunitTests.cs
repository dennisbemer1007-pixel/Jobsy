namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 03: filter sheet sections, pending apply, Wis alles, travel sync.
/// Source/markup guards (full VacancyDiscovery circuit is heavy for bUnit).
/// </summary>
public class KbFilterSheetBunitTests
{
    [Fact]
    public void Candidate_filter_sections_render_in_mockup_order()
    {
        var discovery = ReadDiscovery();
        var bodyStart = discovery.IndexOf("class=\"filter-sheet__body\"", StringComparison.Ordinal);
        Assert.True(bodyStart > 0);
        var footerStart = discovery.IndexOf("class=\"filter-sheet__footer\"", StringComparison.Ordinal);
        Assert.True(footerStart > bodyStart);
        var body = discovery[bodyStart..footerStart];

        string[] expected =
        [
            "data-filter-section=\"keyword\"",
            "data-filter-section=\"travel\"",
            "data-filter-section=\"match\"",
            "data-filter-section=\"hours\"",
            "data-filter-section=\"category\"",
            "data-filter-section=\"branch\"",
            "data-filter-section=\"wage\"",
            "data-filter-section=\"age\"",
            "data-filter-section=\"distance\"",
            "data-filter-section=\"sort\"",
        ];

        var last = -1;
        foreach (var marker in expected)
        {
            var idx = body.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(idx >= 0, $"Missing section marker {marker}");
            Assert.True(idx > last, $"Section order wrong for {marker}");
            last = idx;
        }

        var myVac = body.IndexOf("data-filter-section=\"my-vacancies\"", StringComparison.Ordinal);
        if (myVac >= 0)
        {
            Assert.True(myVac < body.IndexOf("data-filter-section=\"keyword\"", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Match_section_is_gated_for_candidates_with_fit_filters()
    {
        var discovery = ReadDiscovery();
        var matchIdx = discovery.IndexOf("data-filter-section=\"match\"", StringComparison.Ordinal);
        Assert.True(matchIdx > 0);
        var gate = discovery.LastIndexOf("@if (_isCandidate && ShowFitFilters)", matchIdx, StringComparison.Ordinal);
        Assert.True(gate > 0 && matchIdx - gate < 400);
        Assert.Contains("data-filter-section=\"my-vacancies\"", discovery, StringComparison.Ordinal);
        Assert.Contains("_canShowMyVacancies", discovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Wis_alles_clears_filters_but_not_address_helpers()
    {
        var discovery = ReadDiscovery();
        Assert.Contains("Kb.Filter.ClearAll", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-filter-clear-all\"", discovery, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"ClearAllFiltersAsync\"", discovery, StringComparison.Ordinal);

        var clearMethod = discovery.IndexOf("private async Task ClearAllFiltersAsync()", StringComparison.Ordinal);
        Assert.True(clearMethod > 0);
        var snippet = discovery.Substring(clearMethod, 900);
        Assert.DoesNotContain("_origin = null", snippet, StringComparison.Ordinal);
        Assert.DoesNotContain("_addressQuery =", snippet, StringComparison.Ordinal);
        Assert.Contains("_selectedWorkTypes.Clear()", snippet, StringComparison.Ordinal);
        Assert.Contains("_minMatchPercent = 0", snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void Annuleren_restores_snapshot_and_apply_commits_pending()
    {
        var discovery = ReadDiscovery();
        Assert.Contains("RestoreFilterSnapshot()", discovery, StringComparison.Ordinal);
        Assert.Contains("CaptureFilterSnapshot()", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-filter-cancel\"", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-filter-apply\"", discovery, StringComparison.Ordinal);
        Assert.Contains("ApplyFiltersLabel", discovery, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.ShowCount", discovery, StringComparison.Ordinal);
        Assert.Contains("SchedulePendingCountRefresh", discovery, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(300", discovery, StringComparison.Ordinal);

        // Pending while sheet open — do not reload the live list until Toon.
        var reload = discovery.IndexOf("private async Task ReloadFiltersIfAppliedAsync()", StringComparison.Ordinal);
        Assert.True(reload > 0);
        var reloadBody = discovery.Substring(reload, 350);
        Assert.Contains("if (_filtersOpen)", reloadBody, StringComparison.Ordinal);
        Assert.Contains("SchedulePendingCountRefresh()", reloadBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Travel_preset_chips_and_slider_share_max_travel_minutes()
    {
        var discovery = ReadDiscovery();
        Assert.Contains("SetSheetTravelMinutesAsync", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-sheet-travel-slider", discovery, StringComparison.Ordinal);
        Assert.Contains("OnTravelSliderInput", discovery, StringComparison.Ordinal);
        Assert.Contains("TravelChipMinutes", discovery, StringComparison.Ordinal);
        Assert.Contains("filter-sheet__modes", discovery, StringComparison.Ordinal);
        Assert.Contains("SetSheetTransportAsync", discovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Footer_uses_toon_banen_not_toepassen_and_header_has_wis_alles()
    {
        var discovery = ReadDiscovery();
        var footerIdx = discovery.IndexOf("class=\"filter-sheet__footer\"", StringComparison.Ordinal);
        Assert.True(footerIdx > 0);
        var footer = discovery.Substring(footerIdx, 500);
        Assert.Contains("ApplyFiltersLabel", footer, StringComparison.Ordinal);
        Assert.DoesNotContain("Discovery.Apply", footer, StringComparison.Ordinal);

        var headerIdx = discovery.IndexOf("class=\"filter-sheet__header\"", StringComparison.Ordinal);
        var header = discovery.Substring(headerIdx, 600);
        Assert.Contains("Kb.Filter.ClearAll", header, StringComparison.Ordinal);
        Assert.Contains("filter-sheet__close", header, StringComparison.Ordinal);
        Assert.DoesNotContain("filter-sheet__mascot", header, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_eighty_label_aligns_with_gte_threshold()
    {
        var discovery = ReadDiscovery();
        Assert.Contains("pct >= _minMatchPercent", discovery, StringComparison.Ordinal);
        var strings = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Localization", "UiStringsKandidaatBanen.cs"));
        Assert.Contains("Kb.Filter.Match80", strings, StringComparison.Ordinal);
        Assert.Contains("Vanaf 80%", strings, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Alleen >80%", strings, StringComparison.Ordinal);
    }

    [Fact]
    public void Feature_css_has_sticky_sheet_controls_at_forty_four_px()
    {
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "kandidaat-banen.css"));
        Assert.Contains(".filter-sheet__close", css, StringComparison.Ordinal);
        Assert.Contains("width: 44px", css, StringComparison.Ordinal);
        Assert.Contains("height: 44px", css, StringComparison.Ordinal);
        Assert.Contains(".filter-sheet__seg-btn", css, StringComparison.Ordinal);
        Assert.Contains(".filter-sheet__modes", css, StringComparison.Ordinal);
        Assert.Contains(".filter-sheet__chip", css, StringComparison.Ordinal);
    }

    private static string ReadDiscovery() =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));

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
