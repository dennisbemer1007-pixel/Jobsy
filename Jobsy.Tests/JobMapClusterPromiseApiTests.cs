namespace Jobsy.Tests;

/// <summary>
/// Guards the MapLibre 5 promise-only cluster API. Callback-style
/// getClusterExpansionZoom / getClusterLeaves never fire (dead clusters on Acc).
/// </summary>
public class JobMapClusterPromiseApiTests
{
    [Fact]
    public void JobMap_js_uses_promise_cluster_api_not_callbacks()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));

        Assert.DoesNotContain(
            "getClusterExpansionZoom(clusterId, function",
            js,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "getClusterLeaves(clusterId, 100, 0, function",
            js,
            StringComparison.Ordinal);
        Assert.Contains("await source.getClusterExpansionZoom", js, StringComparison.Ordinal);
        Assert.Contains("await source.getClusterLeaves", js, StringComparison.Ordinal);
        Assert.Contains("async function onClusterClick", js, StringComparison.Ordinal);
        Assert.Contains("openLeavesPager", js, StringComparison.Ordinal);
        Assert.Contains("easeTo", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Node_js_cluster_promise_guard_passes()
    {
        var root = FindRepoRoot();
        var script = Path.Combine(root, "Jobsy.Tests", "js", "jobMap-cluster-promise-api.test.mjs");
        Assert.True(File.Exists(script), "Missing " + script);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "node",
            Arguments = "\"" + script + "\"",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        Assert.NotNull(proc);
        var stdout = proc!.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(30_000);
        Assert.True(proc.ExitCode == 0, "node test failed: " + stderr + stdout);
        Assert.Contains("ok", stdout, StringComparison.OrdinalIgnoreCase);
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
