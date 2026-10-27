namespace Jobsy.Tests;

public sealed class WerkgeverSollicitatiesRouteSplitTests
{
    [Fact]
    public void Entry_owns_root_sollicitaties_route_without_blazor_runtime()
    {
        var root = FindRepoRoot();
        var entry = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Werkgever/WerkgeverSollicitatiesEntry.razor"));
        var pipeline = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Werkgever/Applicants.razor"));

        Assert.Contains("@page \"/werkgever/sollicitaties\"", entry, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", entry, StringComparison.Ordinal);
        Assert.Contains("@page \"/werkgever/sollicitaties/pipeline\"", pipeline, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/werkgever/sollicitaties\"\n", pipeline, StringComparison.Ordinal);
    }

    [Fact]
    public void List_view_query_redirects_to_ssr_entry_from_pipeline()
    {
        var pipeline = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/Applicants.razor"));
        Assert.Contains("BuildListViewUrl", pipeline, StringComparison.Ordinal);
        Assert.Contains("forceLoad: true", pipeline, StringComparison.Ordinal);
        Assert.Contains("/werkgever/sollicitaties/pipeline", pipeline, StringComparison.Ordinal);
    }

    [Fact]
    public void Entry_loads_list_data_via_api_for_list_view()
    {
        var entry = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/WerkgeverSollicitatiesEntry.razor"));
        Assert.Contains("EmployerApplicationsListSsrData.LoadAsync", entry, StringComparison.Ordinal);
        Assert.Contains("EmployerApplicationsListSsrShells.BuildBody", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildRouteShell", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void List_ssr_shell_exposes_testid_and_view_toggle()
    {
        var shells = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Werkgever/EmployerApplicationsListSsrShells.cs"));
        Assert.Contains("wg-applications-list-ssr", shells, StringComparison.Ordinal);
        Assert.Contains("/werkgever/sollicitaties/pipeline", shells, StringComparison.Ordinal);
        Assert.Contains("view=lijst", shells, StringComparison.Ordinal);
        Assert.Contains("ent-data-table", shells, StringComparison.Ordinal);
        Assert.Contains("EmployerApplicationsListSsrData", File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Werkgever/EmployerApplicationsListSsrData.cs")), StringComparison.Ordinal);
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
