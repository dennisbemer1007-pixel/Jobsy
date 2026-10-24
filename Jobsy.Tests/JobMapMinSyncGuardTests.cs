namespace Jobsy.Tests;

/// <summary>
/// jobMap.min.js must match terser(jobMap.js) and asset-versions sha256 (production serves min only).
/// </summary>
public sealed class JobMapMinSyncGuardTests
{
    [Fact]
    public void Node_jobMap_min_sync_guard_passes()
    {
        var root = FindRepoRoot();
        var script = Path.Combine(root, "Jobsy.Tests", "js", "jobMap-min-sync.test.mjs");
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
        proc.WaitForExit(120_000);
        Assert.True(proc.ExitCode == 0, "node test failed: " + stderr + stdout);
        Assert.Contains("jobMap-min-sync: ok", stdout, StringComparison.Ordinal);
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
