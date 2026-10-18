using System.Diagnostics;
using System.Globalization;

namespace Jobsy.Tests;

public sealed class ReferenceVerificationHeroAssetTests
{
    [Fact]
    public void Ref_full_hero_assets_exist_and_seam_check_passes()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var full = Path.Combine(repoRoot, "Jobsy.Web/wwwroot/images/candidate/ref-full.jpg");
        Assert.True(File.Exists(full), full);
        Assert.True(new FileInfo(full).Length > 50_000);

        var seamOut = RunScript(repoRoot, "tools/scripts/check_ref_hero_desktop_seam.py");
        Assert.Contains("Desktop seam check", seamOut, StringComparison.Ordinal);
        var jump = double.Parse(
            seamOut.Split("jump")[1].Split('(')[0].Trim(),
            CultureInfo.InvariantCulture);
        Assert.True(jump <= 6.0, seamOut);
    }

    private static string RunScript(string repoRoot, string relativeScript)
    {
        var script = Path.Combine(repoRoot, relativeScript);
        Assert.True(File.Exists(script), script);
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "python3",
            ArgumentList = { script },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = repoRoot,
        });
        Assert.NotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(30_000);
        Assert.True(process.ExitCode == 0, stderr + stdout);
        return stdout;
    }
}
