namespace Jobsy.Tests;

/// <summary>
/// Mirrors CI <c>code-quality.yml</c> — app.min.css is committed by hand after running tools/css/build.sh (Render does not rebuild it).
/// </summary>
public sealed class AppMinCssSyncTests
{
    [Fact]
    public void App_min_css_matches_tools_css_build_output()
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, "tools", "css", "build.sh");
        var committed = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "app.min.css");
        Assert.True(File.Exists(script), "Missing tools/css/build.sh");
        Assert.True(File.Exists(committed), "Missing Jobsy.Web/wwwroot/css/app.min.css");

        var before = File.ReadAllBytes(committed);
        var exit = RunShell(script, repoRoot);
        Assert.Equal(0, exit);

        var after = File.ReadAllBytes(committed);
        Assert.True(before.AsSpan().SequenceEqual(after), """
            app.min.css is out of sync with app.css. Run tools/css/build.sh and commit Jobsy.Web/wwwroot/css/app.min.css
            (CI code-quality.yml enforces the same check; Render serves the committed file).
            """);
    }

    private static int RunShell(string script, string workingDirectory)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash", script)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start build.sh");
        process.WaitForExit(120_000);
        if (process.ExitCode != 0)
        {
            var err = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"build.sh failed: {err}");
        }

        return process.ExitCode;
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
