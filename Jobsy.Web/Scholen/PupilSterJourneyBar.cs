using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Journey strip nodes for groep 7/8 Sterrenkust question screen (mockup 08).</summary>
public static class PupilSterJourneyBar
{
    public sealed record Node(string TitleKey, string? ImageUrl, bool IsPuzzle, bool IsResult = false);

    public static readonly IReadOnlyList<Node> Nodes =
    [
        new("Leerling.Route.Ster.Stop.Duinpad.Title", "/leerling/werelden/img/dot-ster-1.jpg", false),
        new("Leerling.Route.Stop.Schelpenrij.Title", null, true),
        new("Leerling.Route.Ster.Stop.Maangrot.Title", "/leerling/werelden/img/dot-ster-2.jpg", false),
        new("Leerling.Route.Stop.Schatkaart.Title", null, true),
        new("Leerling.Route.Stop.Island.Title", "/leerling/werelden/img/dot-ster-3.jpg", false),
        new("Leerling.Route.Stop.Lighthouse.Title", "/leerling/werelden/img/dot-ster-4.jpg", false),
        new("Leerling.Route.Stop.Lamps.Title", null, true),
        new("Leerling.Route.Ster.Stop.Sterrenbaai.Title", "/leerling/werelden/img/dot-ster-5.jpg", false),
        new("Leerling.Route.Stop.Result.Title", "/leerling/werelden/img/dot-ster-6.jpg", false, IsResult: true),
    ];

    public static int CurrentNodeIndex(string? worldKey, bool completed)
    {
        if (completed)
        {
            return Nodes.Count - 1;
        }

        return worldKey?.ToLowerInvariant() switch
        {
            "koraalrif" => 0,
            "schatgrot" => 2,
            "vuurtoren" => 5,
            "lagune" => 7,
            _ => 0,
        };
    }

    public static int IntroNodeIndex(string? puzzleKey)
        => puzzleKey?.ToLowerInvariant() switch
        {
            "schelpenrij" => 1,
            "schatkaart" => 3,
            "vuurtorenlampen" => 6,
            _ => 0,
        };

    public static string DoneLineWidth(int currentIndex)
    {
        var n = Nodes.Count;
        if (n <= 1)
        {
            return "0";
        }

        var ratio = (double)currentIndex / (n - 1);
        return $"calc((100% - 52px) * {ratio.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    public static int PartNumber(string? worldKey)
        => worldKey?.ToLowerInvariant() switch
        {
            "koraalrif" => 1,
            "schatgrot" => 2,
            "vuurtoren" => 3,
            "lagune" => 4,
            _ => 1,
        };

    public static string WorldBandImage(string? worldKey)
        => worldKey?.ToLowerInvariant() switch
        {
            "schatgrot" => "/leerling/werelden/img/sterren-kaart.jpg",
            "vuurtoren" => "/leerling/werelden/img/sterren-strand.jpg",
            "lagune" => "/leerling/werelden/img/sterren-strand.jpg",
            _ => "/leerling/werelden/img/sterren-kaart.jpg",
        };

    public static string WorldKickerKey(string? worldKey)
        => worldKey?.ToLowerInvariant() switch
        {
            "schatgrot" => "Leerling.World.Ster.Maangrot.Title",
            "vuurtoren" => "Leerling.World.Ster.Vuurtoren.Title",
            "lagune" => "Leerling.World.Ster.Sterrenbaai.Title",
            _ => "Leerling.World.Ster.Duinpad.Title",
        };

    public static int SegmentDoneBefore(int indexInWorldOneBased)
        => Math.Max(0, indexInWorldOneBased - 1);
}
