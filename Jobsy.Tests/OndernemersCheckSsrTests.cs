namespace Jobsy.Tests;

public sealed class OndernemersCheckSsrTests
{
    [Fact]
    public void Ondernemerscheck_skips_blazor_runtime()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/OndernemersCheck.razor"));
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServerRenderMode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Uwv_client_endpoints_register_ondernemer_save()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Program.cs"));
        Assert.Contains("MapUwvClientEndpoints", program, StringComparison.Ordinal);
        var endpoints = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Hosting/UwvClientEndpoints.cs"));
        Assert.Contains("/api/uwv-client/ondernemer", endpoints, StringComparison.Ordinal);
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
