using System.Diagnostics;
using System.Globalization;

namespace Jobsy.Tests;

public sealed class ReferenceVerificationHeroAssetTests
{
    [Fact]
    public void Ref_full_hero_assets_exist_and_seam_check_passes()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var desktop = Path.Combine(repoRoot, "Jobsy.Web/wwwroot/images/candidate/ref-full.jpg");
        var mobile = Path.Combine(repoRoot, "Jobsy.Web/wwwroot/images/candidate/ref-full-mobile.jpg");
        Assert.True(File.Exists(desktop), desktop);
        Assert.True(new FileInfo(desktop).Length > 50_000);

        RunScript(repoRoot, "tools/scripts/build_ref_full_mobile.py");
        RunScript(repoRoot, "tools/scripts/build_ref_full_desktop.py");
        var desktopCrop = Path.Combine(repoRoot, "Jobsy.Web/wwwroot/images/candidate/ref-full-desktop.jpg");
        Assert.True(File.Exists(desktopCrop), desktopCrop);
        Assert.True(File.Exists(mobile), mobile);
        Assert.True(new FileInfo(mobile).Length > 30_000);

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
