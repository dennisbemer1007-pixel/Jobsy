using System.Text.RegularExpressions;

namespace Jobsy.Tests.Scholen;

public sealed class PupilVuurtorenPuzzleMarkupTests
{
    private static string PuzzleSsrSource()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Scholen",
            "PupilPuzzelSsrHtml.cs"));
        Assert.True(File.Exists(path), $"Missing {path}");
        return File.ReadAllText(path);
    }

    private static string LampPlaySource()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Scholen",
            "PupilGroep78MockHtml.cs"));
        Assert.True(File.Exists(path), $"Missing {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Vuurtoren_lamps_use_play_mode_markup_without_busy_disabled_binding()
    {
        var play = LampPlaySource();
        var lampBlock = Regex.Match(
            play,
            @"PuzzleLampPlay[\s\S]*?return sb\.ToString\(\);",
            RegexOptions.Singleline);
        Assert.True(lampBlock.Success, "Expected PuzzleLampPlay in PupilGroep78MockHtml.cs");

        var block = lampBlock.Value;
        Assert.Contains("ll-puzzle__lamps", block, StringComparison.Ordinal);
        Assert.Contains("PuzzleLampBeaconMarkup", block, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@_busy", block, StringComparison.Ordinal);
        Assert.DoesNotContain("False ||", block, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Leerling.Puzzle.Vuurtoren.Lamp", play, StringComparison.Ordinal);
        Assert.Contains("Leerling.Puzzle.Vuurtoren.Done", play, StringComparison.Ordinal);

        var ssr = PuzzleSsrSource();
        Assert.Contains("PuzzleLampPlay", ssr, StringComparison.Ordinal);
    }
}
