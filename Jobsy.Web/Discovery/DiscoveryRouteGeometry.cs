namespace Jobsy.Web.Discovery;

/// <summary>Fixed SVG paths for SSR overview (no client layout measure).</summary>
internal static class DiscoveryRouteGeometry
{
    /// <summary>Desktop brush path through five haltes (viewBox 0 0 1000 320).</summary>
    public const string DesktopPathD =
        "M 72 200 C 120 120, 180 100, 250 150 S 380 220, 500 165 S 620 90, 720 140 S 860 200, 928 175";

    /// <summary>Mobile trail through five stops (viewBox 0 0 360 900).</summary>
    public const string MobileFuturePathD =
        "M 180 40 C 220 120, 120 200, 180 280 S 240 420, 180 520 S 120 660, 180 760 S 240 820, 180 860";

    public const string MobileDonePathD =
        "M 180 40 C 220 120, 120 200, 180 280 S 240 420, 180 520";

    /// <summary>SSR mobile: trail ends above the destination label plate.</summary>
    public const string MobileFuturePathSsrD =
        "M 180 40 C 220 120, 120 200, 180 280 S 240 420, 180 520 S 120 660, 180 710";
}
