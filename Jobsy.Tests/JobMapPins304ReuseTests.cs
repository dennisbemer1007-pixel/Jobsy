namespace Jobsy.Tests;

/// <summary>
/// Source + node contract for FIX 2 (pins 304 cache + boot-map reuse).
/// </summary>
public class JobMapPins304ReuseTests
{
    [Fact]
    public void Node_js_pins_304_reuse_guard_passes()
    {
        var root = FindRepoRoot();
        var script = Path.Combine(root, "Jobsy.Tests", "js", "jobMap-pins-304-reuse.test.mjs");
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
