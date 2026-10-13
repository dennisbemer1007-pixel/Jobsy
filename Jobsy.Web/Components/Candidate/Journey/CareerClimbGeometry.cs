namespace Jobsy.Web.Components.Candidate.Journey;

/// <summary>
/// Stone anchors for <c>CareerClimbScene</c> (§S). The lobster climbs from the deep
/// (first point) to the light (last point, the golden dream stone).
/// Five stones sit exactly on the anchors; other counts are spread by arc length
/// along the same zig-zag path so stones never crowd each other.
/// </summary>
public static class CareerClimbGeometry
{
    public const double DesktopWidth = 404;
    public const double DesktopHeight = 748;
    public const double MobileWidth = 390;
    public const double MobileHeight = 180;

    /// <summary>Minimum distance between two stone centres on the desktop zone.</summary>
    public const double DesktopMinDistance = 60;

    /// <summary>Minimum distance between two stone centres on the mobile band.</summary>
    public const double MobileMinDistance = 44;

    private const int AnchorCount = 5;

    public sealed record Point(double X, double Y);

    private static readonly Point[] DesktopAnchors =
    [
        new(70, 690),
        new(250, 560),
        new(110, 420),
        new(280, 280),
        new(140, 110)
    ];

    private static readonly Point[] MobileAnchors =
    [
        new(30, 151),
        new(108, 124),
        new(190, 96),
        new(272, 68),
        new(356, 34)
    ];

    /// <summary>
    /// Stone centres for a plan with <paramref name="stepCount"/> steps:
    /// "Nu" + the plan steps + the dream stone.
    /// </summary>
    public static IReadOnlyList<Point> Points(int stepCount, bool mobile)
    {
        var stones = Math.Clamp(stepCount, 1, 6) + 2;
        var anchors = mobile ? MobileAnchors : DesktopAnchors;

        if (stones == AnchorCount)
        {
            return anchors;
        }

        return Spread(anchors, stones);
    }

    public static double Width(bool mobile) => mobile ? MobileWidth : DesktopWidth;

    public static double Height(bool mobile) => mobile ? MobileHeight : DesktopHeight;

    public static double MinDistance(bool mobile) => mobile ? MobileMinDistance : DesktopMinDistance;

    public static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static IReadOnlyList<Point> Spread(Point[] anchors, int stones)
    {
        var lengths = new double[anchors.Length - 1];
        var total = 0d;
        for (var i = 0; i < lengths.Length; i++)
        {
            lengths[i] = Distance(anchors[i], anchors[i + 1]);
            total += lengths[i];
        }

        var result = new List<Point>(stones);
        for (var i = 0; i < stones; i++)
        {
            var target = stones == 1 ? 0 : total * i / (stones - 1);
            result.Add(PointAt(anchors, lengths, target));
        }

        return result;
    }

    private static Point PointAt(Point[] anchors, double[] lengths, double distance)
    {
        var walked = 0d;
        for (var i = 0; i < lengths.Length; i++)
        {
            if (distance <= walked + lengths[i] || i == lengths.Length - 1)
            {
                var t = lengths[i] <= 0 ? 0 : Math.Clamp((distance - walked) / lengths[i], 0, 1);
                var a = anchors[i];
                var b = anchors[i + 1];
                return new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            }

            walked += lengths[i];
        }

        return anchors[^1];
    }
}
