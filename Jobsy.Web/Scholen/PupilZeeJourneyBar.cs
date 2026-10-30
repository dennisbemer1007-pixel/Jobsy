using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Journey strip nodes for groep 7/8 Zee-expeditie question screen (mockup 02).</summary>
public static class PupilZeeJourneyBar
{
    public sealed record Node(string TitleKey, string? ImageUrl, bool IsPuzzle, bool IsResult = false);

    public static readonly IReadOnlyList<Node> Nodes =
    [
        new("Leerling.Route.Stop.Reef.Title", "/leerling/werelden/img/rif.jpg", false),
        new("Leerling.Route.Stop.Schelpenrij.Title", null, true),
        new("Leerling.Route.Stop.Cave.Title", "/leerling/werelden/img/grot.jpg", false),
        new("Leerling.Route.Stop.Schatkaart.Title", null, true),
        new("Leerling.Route.Stop.Island.Title", "/leerling/werelden/img/eiland.jpg", false),
        new("Leerling.Route.Stop.Lighthouse.Title", "/leerling/werelden/img/vuurtoren.jpg", false),
        new("Leerling.Route.Stop.Lamps.Title", null, true),
        new("Leerling.Route.Stop.Lagoon.Title", "/leerling/werelden/img/lagune.jpg", false),
        new("Leerling.Route.Stop.Result.Title", null, false, IsResult: true),
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
            "schatgrot" => "/leerling/werelden/img/grot.jpg",
            "vuurtoren" => "/leerling/werelden/img/vuurtoren.jpg",
            "lagune" => "/leerling/werelden/img/lagune.jpg",
            _ => "/leerling/werelden/img/rif.jpg",
        };

    public static string WorldKickerKey(string? worldKey)
        => worldKey?.ToLowerInvariant() switch
        {
            "schatgrot" => "Leerling.World.Cave.Title",
            "vuurtoren" => "Leerling.World.Lighthouse.Title",
            "lagune" => "Leerling.World.Lagoon.Title",
            _ => "Leerling.World.Reef.Title",
        };

    public static int SegmentDoneBefore(int indexInWorldOneBased)
        => Math.Max(0, indexInWorldOneBased - 1);
}
