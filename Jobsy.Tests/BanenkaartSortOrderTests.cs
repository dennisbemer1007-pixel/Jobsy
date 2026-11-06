using Jobsy.Web.Banenkaart;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public sealed class BanenkaartSortOrderTests
{
    [Fact]
    public void ResolveEffectiveSort_defaults_to_match_for_candidate_with_fit()
    {
        var sort = BanenkaartSsrQuery.ResolveEffectiveSort(
            sortFromState: "distance",
            sortExplicitInUrl: false,
            isCandidate: true,
            showFitFilters: true,
            hasOrigin: true);

        Assert.Equal("match", sort);
    }

    [Fact]
    public void OrderVacancies_sorts_by_match_percent_desc_then_title()
    {
        var items = new List<VacancyListItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Bravo", FitPercent = 40 },
            new() { Id = Guid.NewGuid(), Title = "Alpha", FitPercent = 90 },
            new() { Id = Guid.NewGuid(), Title = "Charlie", FitPercent = 90 },
        };

        var ordered = BanenkaartSsrQuery.OrderVacancies(items, "match", showFitFilters: true, hasOrigin: true);

        Assert.Equal("Alpha", ordered[0].Title);
        Assert.Equal("Charlie", ordered[1].Title);
        Assert.Equal("Bravo", ordered[2].Title);
    }

    [Fact]
    public void Banenkaart_catalog_merges_boot_fit_sort_when_url_has_origin()
    {
        var root = FindRepoRoot();
        var catalog = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-catalog.js"));
        Assert.Contains("mergeUrlFiltersWithBoot", catalog, StringComparison.Ordinal);
        Assert.Contains("kb-sheet-sort", catalog, StringComparison.Ordinal);

        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Banenkaart", "BanenkaartSsrShells.cs"));
        Assert.Contains("kb-sheet-sort", shells, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
