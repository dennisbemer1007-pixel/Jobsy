using System.Diagnostics;
using System.Globalization;

namespace Jobsy.Tests;

public sealed class ReferenceVerificationHeroAssetTests
{
    [Fact]
    public void Ref_verif_hero_background_columns_stay_smooth()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var script = Path.Combine(repoRoot, "tools", "scripts", "build_ref_verif_hero.py");
        Assert.True(File.Exists(script), $"Hero build script missing at {script}.");

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

        Assert.Contains("jump", stdout, StringComparison.Ordinal);
        foreach (var part in stdout.Split("jump")[1..])
        {
            var token = part.Split(';')[0].Split(',')[0].Trim();
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var jump))
            {
                continue;
            }

            Assert.True(jump <= 6.0, $"Hero background jump {jump} exceeds 6. ({stdout})");
        }
    }
}
