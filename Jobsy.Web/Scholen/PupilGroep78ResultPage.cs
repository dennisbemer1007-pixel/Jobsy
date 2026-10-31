using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Theme-specific assets for groep 7/8 screen 10 (Dit ben jij).</summary>
public static class PupilGroep78ResultPage
{
    public static int ResultNodeIndex(string? journeyThemeKey)
    {
        var nodes = PupilGroep78JourneyStripNodes.ForTheme(journeyThemeKey);
        return nodes.Count - 1;
    }

    public static string BandTestId(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                "ll-tuin-result-band",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                "ll-ster-result-band",
            _ => "ll-zee-result-band",
        };

    public static string BandExtraClass(string? journeyThemeKey)
        => journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) =>
                "rs-tuin",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) =>
                "rs-ster",
            _ => "",
        };

    /// <summary>Tile illustration URLs in competence → riasec → values → culture order.</summary>
    public static string TileImageUrl(string? journeyThemeKey, int tileIndex)
    {
        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase))
        {
            return tileIndex switch
            {
                0 => "/leerling/werelden/img/dot-tuin-1.jpg",
                1 => "/leerling/werelden/img/dot-tuin-2.jpg",
                2 => "/leerling/werelden/img/dot-tuin-4.jpg",
                _ => "/leerling/werelden/img/dot-tuin-6.jpg",
            };
        }

        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase))
        {
            return tileIndex switch
            {
                0 => "/leerling/werelden/img/dot-ster-1.jpg",
                1 => "/leerling/werelden/img/dot-ster-2.jpg",
                2 => "/leerling/werelden/img/dot-ster-4.jpg",
                _ => "/leerling/werelden/img/dot-ster-5.jpg",
            };
        }

        return tileIndex switch
        {
            0 => "/leerling/werelden/img/rif.jpg",
            1 => "/leerling/werelden/img/grot.jpg",
            2 => "/leerling/werelden/img/vuurtoren.jpg",
            _ => "/leerling/werelden/img/lagune.jpg",
        };
    }

    public static int TileIndex(string modelKey)
        => modelKey.ToLowerInvariant() switch
        {
            "competence" => 0,
            "riasec" => 1,
            "values" => 2,
            _ => 3,
        };
}
