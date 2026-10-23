namespace Jobsy.Tests;

public sealed class BanenkaartSsrUiBootGuardTests
{
    [Fact]
    public void Banenkaart_ssr_closes_filters_and_defers_map_boot_until_ui_ready()
    {
        var root = FindRepoRoot();
        var ssr = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-ssr.js"));
        var boot = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-maps-boot.js"));

        Assert.Contains("applyInitialUiState", ssr, StringComparison.Ordinal);
        Assert.Contains("jobsy-bk-ssr-ui-ready", ssr, StringComparison.Ordinal);
        Assert.DoesNotContain("if (state.filtersOpen)", ssr, StringComparison.Ordinal);
        Assert.Contains("scheduleWarmBoot", boot, StringComparison.Ordinal);
        Assert.Contains("jobsy-bk-ssr-ui-ready", boot, StringComparison.Ordinal);
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
