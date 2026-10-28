namespace Jobsy.Tests;

/// <summary>PR #1332: banenkaart stays on acceptatie baseline; paspoort-only plain work on the branch.</summary>
public sealed class BanenkaartMatchesAcceptatieGuardTests
{
    private static readonly string[] BanenkaartPaths =
    [
        "Jobsy.Web/Banenkaart/BanenkaartSsrShells.cs",
        "Jobsy.Web/Banenkaart/BanenkaartSsrActiveFilters.cs",
        "Jobsy.Web/Banenkaart/BanenkaartSsrListHtml.cs",
        "Jobsy.Web/wwwroot/js/banenkaart-catalog.js",
        "Jobsy.Web/wwwroot/js/banenkaart-map-icons.js",
    ];

    [Fact]
    public void Banenkaart_sources_match_origin_acceptatie()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        foreach (var rel in BanenkaartPaths)
        {
            var path = Path.Combine(root, rel);
            Assert.True(File.Exists(path), $"Missing {rel}");
            var branch = File.ReadAllText(path);
            var acceptatie = RunGitShow($"origin/acceptatie:{rel}");
            if (acceptatie is null)
            {
                Assert.Fail($"File {rel} not on origin/acceptatie");
            }

            Assert.True(
                string.Equals(branch, acceptatie, StringComparison.Ordinal),
                $"Banenkaart file differs from acceptatie: {rel}");
        }

        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/candidate-plain-banenkaart.css")));
    }

    private static string? RunGitShow(string spec)
    {
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"show {spec}",
                WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        proc.Start();
        var stdout = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(5000);
        return proc.ExitCode == 0 ? stdout : null;
    }
}
