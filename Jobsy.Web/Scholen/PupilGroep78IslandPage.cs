using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Groep 7/8 Pauze-eiland screens 11a/11b (mockups 11a-*, 11b-*).</summary>
public static class PupilGroep78IslandPage
{
    public const int IslandNodeIndex = 4;

    public static string BandTestId(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                "ll-tuin-eiland-band",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                "ll-ster-eiland-band",
            _ => "ll-zee-eiland-band",
        };

    public static string BandExtraClass(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                "is-tuin",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                "is-ster",
            _ => "",
        };

    public static string BandImage(string? journeyThemeKey)
        => PupilGroep78JourneyStripNodes.ForTheme(journeyThemeKey)[IslandNodeIndex].ImageUrl
           ?? "/leerling/werelden/img/eiland.jpg";

    public static IReadOnlyList<PupilGroep78JourneyStripNodes.Node> Nodes(string? journeyThemeKey)
        => PupilGroep78JourneyStripNodes.ForTheme(journeyThemeKey);

    public static string DoneLineWidth(string? journeyThemeKey)
        => PupilGroep78JourneyStripNodes.DoneLineWidth(journeyThemeKey, IslandNodeIndex);

    public sealed record WorldDeco(string C1, string C2, string C3, string C4);

    public static WorldDeco Deco(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                new("wd-leaf wd1", "wd-flower wd2", "wd-leaf wd3", "wd-petal wd4"),
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                new("wd-star wd1", "wd-moon wd2", "wd-star wd3", "wd-star wd4"),
            _ => new("wd-shell wd1", "wd-wave wd2", "wd-fish wd3", "wd-shell wd4"),
        };
}
