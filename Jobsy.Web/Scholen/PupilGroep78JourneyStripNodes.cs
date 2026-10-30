using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Theme-aware journey strip nodes for groep 7/8 puzzle screens.</summary>
public static class PupilGroep78JourneyStripNodes
{
    public sealed record Node(string TitleKey, string? ImageUrl, bool IsPuzzle, bool IsResult = false);

    public static IReadOnlyList<Node> ForTheme(string? journeyThemeKey)
    {
        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase))
        {
            return PupilTuinJourneyBar.Nodes
                .Select(n => new Node(n.TitleKey, n.ImageUrl, n.IsPuzzle, n.IsResult))
                .ToList();
        }

        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase))
        {
            return PupilSterJourneyBar.Nodes
                .Select(n => new Node(n.TitleKey, n.ImageUrl, n.IsPuzzle, n.IsResult))
                .ToList();
        }

        return PupilZeeJourneyBar.Nodes
            .Select(n => new Node(n.TitleKey, n.ImageUrl, n.IsPuzzle, n.IsResult))
            .ToList();
    }

    public static int IntroNodeIndex(string? journeyThemeKey, string? puzzleKey)
    {
        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase))
        {
            return PupilTuinJourneyBar.IntroNodeIndex(puzzleKey);
        }

        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase))
        {
            return PupilSterJourneyBar.IntroNodeIndex(puzzleKey);
        }

        return PupilZeeJourneyBar.IntroNodeIndex(puzzleKey);
    }

    public static string DoneLineWidth(string? journeyThemeKey, int currentIndex)
    {
        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase))
        {
            return PupilTuinJourneyBar.DoneLineWidth(currentIndex);
        }

        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase))
        {
            return PupilSterJourneyBar.DoneLineWidth(currentIndex);
        }

        return PupilZeeJourneyBar.DoneLineWidth(currentIndex);
    }

    public static string PuzzlePlayBandTestId(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                "ll-tuin-puzzel-play-band",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                "ll-ster-puzzel-play-band",
            _ => "ll-zee-puzzel-play-band",
        };

    public static string PuzzlePlayBandClass(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                "pp-tuin",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                "pp-ster",
            _ => "",
        };
}
