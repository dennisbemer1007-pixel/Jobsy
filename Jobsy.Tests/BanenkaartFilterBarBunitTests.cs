using Jobsy.Web.KandidaatBanen;

namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 02: mobile filter bar markup + badge counting (travel excluded).
/// </summary>
public class BanenkaartFilterBarBunitTests
{
    [Fact]
    public void Mobile_filter_bar_has_search_filters_travel_match_and_view_toggle()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));

        Assert.Contains("class=\"kb-filter-bar\"", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-filter-search--bar", discovery, StringComparison.Ordinal);
        Assert.Contains("discovery-keyword-mobile", discovery, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.KeywordPlaceholder", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-filters-button\"", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-travel-chip\"", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-match-button\"", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-view-toggle\"", discovery, StringComparison.Ordinal);
        Assert.Contains("TravelChipModes", discovery, StringComparison.Ordinal);
        Assert.Contains("TravelChipMinutes", discovery, StringComparison.Ordinal);
        Assert.Contains("\"E-bike\"", discovery, StringComparison.Ordinal);

        // Mobile no longer exposes work-type / hours / wage / clear as separate chips.
        var mobileBarStart = discovery.IndexOf("class=\"kb-filter-bar\"", StringComparison.Ordinal);
        Assert.True(mobileBarStart > 0);
        var desktopStart = discovery.IndexOf("kb-filter-chips--desktop", StringComparison.Ordinal);
        Assert.True(desktopStart > mobileBarStart);
        var mobileSection = discovery[mobileBarStart..desktopStart];
        Assert.DoesNotContain("Kb.Filter.WorkType", mobileSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Kb.Filter.Hours", mobileSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Kb.Filter.Wage", mobileSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Kb.Filter.Clear", mobileSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Kb.Filter.More", mobileSection, StringComparison.Ordinal);

        // Desktop keeps chips and gains Filters + Match.
        var desktopSection = discovery[desktopStart..];
        Assert.Contains("Kb.Filter.WorkType", desktopSection, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-filters-button\"", desktopSection, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-match-button\"", desktopSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Travel_chip_presets_are_10_20_30_with_five_modes()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("private static readonly string[] TravelChipModes = [\"Fiets\", \"E-bike\", \"Auto\", \"OV\", \"Lopend\"]", discovery, StringComparison.Ordinal);
        Assert.Contains("private static readonly int[] TravelChipMinutes = [10, 20, 30]", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("TravelChipMinutes = [10, 20, 30, 45]", discovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Filters_badge_hidden_at_zero_and_shows_count_for_two_filters()
    {
        var defaults = new KbFilterDefaults();
        var zero = new KbFilterState(
            Transport: "Auto",
            MaxTravelMinutes: 30,
            RadiusKm: 15,
            AgeYears: null,
            MinHoursPerWeek: 0,
            MaxHoursPerWeek: 40,
            SearchQuery: null,
            WorkTypeCount: 0,
            CategoryCount: 0,
            HasMinWage: false,
            HasMaxWage: false,
            MyVacanciesOnly: false,
            MinMatchPercent: 0,
            HasOrigin: true);
        Assert.Equal(0, KbFilterBadge.Count(zero, defaults));

        var two = zero with { SearchQuery = "zorg", WorkTypeCount = 1 };
        Assert.Equal(2, KbFilterBadge.Count(two, defaults));

        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("@if (ActiveFilterCount > 0)", discovery, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-filters-badge\"", discovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Address_field_css_matches_fld_height_and_font()
    {
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "kandidaat-banen.css"));
        Assert.Contains(".kb-address__field input", css, StringComparison.Ordinal);
        Assert.Contains("height: 44px", css, StringComparison.Ordinal);
        Assert.Contains("font-size: 16px", css, StringComparison.Ordinal);
        Assert.Contains(".kb-address__geo", css, StringComparison.Ordinal);
        Assert.Contains("width: 44px", css, StringComparison.Ordinal);
        Assert.Contains(".kb-filter-bar", css, StringComparison.Ordinal);
        Assert.Contains(".kb-filters-button", css, StringComparison.Ordinal);
        Assert.Contains(".kb-match-button", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Address_field_markup_keeps_geo_and_clear_buttons()
    {
        var razor = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Web", "Components", "KandidaatBanen", "KbAddressField.razor"));
        Assert.Contains("kb-address__field", razor, StringComparison.Ordinal);
        Assert.Contains("kb-address__geo", razor, StringComparison.Ordinal);
        Assert.Contains("kb-address__clear", razor, StringComparison.Ordinal);
        Assert.Contains("role=\"combobox\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Ui_strings_include_keyword_placeholder_and_aria_labels()
    {
        var strings = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Web", "Localization", "UiStringsKandidaatBanen.cs"));
        Assert.Contains("Kb.Filter.KeywordPlaceholder", strings, StringComparison.Ordinal);
        Assert.Contains("Wat voor werk zoek je?", strings, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.FiltersAria", strings, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.TravelAria", strings, StringComparison.Ordinal);
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
