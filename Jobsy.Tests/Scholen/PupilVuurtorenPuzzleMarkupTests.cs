using System.Text.RegularExpressions;

namespace Jobsy.Tests.Scholen;

public sealed class PupilVuurtorenPuzzleMarkupTests
{
    private static string PuzzleRazorSource()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Pages",
            "Leerling",
            "LeerlingPuzzel.razor");
        path = Path.GetFullPath(path);
        Assert.True(File.Exists(path), $"Missing {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Vuurtoren_lamps_use_play_mode_only_disabled_binding()
    {
        var src = PuzzleRazorSource();
        var lampBlock = Regex.Match(
            src,
            @"ll-puzzle__lamps[\s\S]*?ll-puzzle__status",
            RegexOptions.Singleline);
        Assert.True(lampBlock.Success, "Expected vuurtoren lamp block in LeerlingPuzzel.razor");

        var block = lampBlock.Value;
        Assert.Contains("disabled=\"@(_lampPlayMode)\"", block, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@_busy", block, StringComparison.Ordinal);
        Assert.DoesNotContain("False ||", block, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Leerling.Puzzle.Vuurtoren.Lamp", block, StringComparison.Ordinal);
    }
}
