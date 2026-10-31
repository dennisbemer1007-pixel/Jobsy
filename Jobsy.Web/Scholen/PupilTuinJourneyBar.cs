using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Journey strip nodes for groep 7/8 Eilandtuin question screen (mockup 05).</summary>
public static class PupilTuinJourneyBar
{
    public sealed record Node(string TitleKey, string? ImageUrl, bool IsPuzzle, bool IsResult = false);

    public static readonly IReadOnlyList<Node> Nodes =
    [
        new("Leerling.Route.Tuin.Stop.Bloemenbaai.Title", "/leerling/werelden/img/dot-tuin-1.jpg", false),
        new("Leerling.Route.Stop.Schelpenrij.Title", null, true),
        new("Leerling.Route.Tuin.Stop.Koraaltuin.Title", "/leerling/werelden/img/dot-tuin-2.jpg", false),
        new("Leerling.Route.Stop.Schatkaart.Title", null, true),
        new("Leerling.Route.Stop.Island.Title", "/leerling/werelden/img/dot-tuin-3.jpg", false),
        new("Leerling.Route.Tuin.Stop.Waterval.Title", "/leerling/werelden/img/dot-tuin-4.jpg", false),
        new("Leerling.Route.Stop.Lamps.Title", null, true),
        new("Leerling.Route.Tuin.Stop.Zonnelagune.Title", "/leerling/werelden/img/dot-tuin-6.jpg", false),
        new("Leerling.Route.Stop.Result.Title", "/leerling/werelden/img/dot-tuin-5.jpg", false, IsResult: true),
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
            "schatgrot" => "/leerling/werelden/img/tuin-koraal.jpg",
            "vuurtoren" => "/leerling/werelden/img/tuin-koraal.jpg",
            "lagune" => "/leerling/werelden/img/tuin-baai.jpg",
            _ => "/leerling/werelden/img/tuin-baai.jpg",
        };

    public static string WorldKickerKey(string? worldKey)
        => worldKey?.ToLowerInvariant() switch
        {
            "schatgrot" => "Leerling.World.Tuin.Koraaltuin.Title",
            "vuurtoren" => "Leerling.World.Tuin.Waterval.Title",
            "lagune" => "Leerling.World.Tuin.Zonnelagune.Title",
            _ => "Leerling.World.Tuin.Bloemenbaai.Title",
        };

    public static int SegmentDoneBefore(int indexInWorldOneBased)
        => Math.Max(0, indexInWorldOneBased - 1);
}
