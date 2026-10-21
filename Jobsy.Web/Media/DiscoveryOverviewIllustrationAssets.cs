namespace Jobsy.Web.Media;

/// <summary>Watercolor assets for De ontdekkingsreis overview (route haltes + hero strip).</summary>
public static class DiscoveryOverviewIllustrationAssets
{
    public const string Version = "20261021-01";

    public static string HeroStrip =>
        $"/images/candidate-illustrated/discovery-overview-strip.webp?v={Version}";

    public static string Halte(string id) =>
        $"/images/discovery/haltes/{id}.webp?v={Version}";

    public static string TestAccent(string id) =>
        $"/images/discovery/test-accents/{id}.webp?v={Version}";
}
