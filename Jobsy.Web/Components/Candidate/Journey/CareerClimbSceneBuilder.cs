using System.Globalization;
using System.Text;

namespace Jobsy.Web.Components.Candidate.Journey;

/// <summary>
/// Builds the decorative SVG layer of <see cref="CareerClimbScene"/>: light rays towards the
/// dream stone, specks in the deep, seaweed, the trail, the stones and the old-shell shards.
/// Classes only — colour comes from <c>features/carriere.css</c> tokens.
/// </summary>
public static class CareerClimbSceneBuilder
{
    /// <summary>Old shell shard left behind on a passed stone.</summary>
    private const string ShardPath = "m-7 0 4-7 5 3-1 4 6 1-2 4z";

    public static string BuildSvg(
        IReadOnlyList<CareerClimbGeometry.Point> stones,
        IReadOnlyList<ClimbStone> states,
        int currentIndex,
        bool mobile,
        bool celebrate,
        bool listen = false)
    {
        var w = CareerClimbGeometry.Width(mobile);
        var h = CareerClimbGeometry.Height(mobile);
        var svg = new StringBuilder();

        AppendRays(svg, stones, w, h, mobile, celebrate);
        AppendSpecks(svg, w, h, mobile);

        if (!mobile)
        {
            AppendWeed(svg, w, h);
        }

        AppendTrail(svg, stones, currentIndex, mobile);
        AppendStones(svg, stones, states, mobile);
        AppendBubbles(svg, stones, currentIndex, mobile);

        if (listen)
        {
            AppendAntenna(svg, stones, currentIndex, mobile);
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"<svg class=\"career-scene__svg\" viewBox=\"0 0 {N(w)} {N(h)}\" preserveAspectRatio=\"none\" width=\"100%\" height=\"100%\" aria-hidden=\"true\" focusable=\"false\">{svg}</svg>");
    }

    private static void AppendRays(
        StringBuilder svg,
        IReadOnlyList<CareerClimbGeometry.Point> stones,
        double w,
        double h,
        bool mobile,
        bool celebrate)
    {
        _ = h;
        var target = stones.Count > 0 ? stones[^1] : new CareerClimbGeometry.Point(w * 0.5, 0);
        var cls = celebrate ? "career-scene__ray career-scene__ray--bright" : "career-scene__ray";
        var spread = mobile ? 26.0 : 74.0;

        for (var i = 0; i < 3; i++)
        {
            var offset = (i - 1) * spread;
            var topStart = w + offset - spread * 1.6;
            var topEnd = topStart + spread * 0.9;
            svg.Append(CultureInfo.InvariantCulture,
                $"<path class=\"{cls}\" d=\"M{N(topStart)} 0L{N(topEnd)} 0L{N(target.X + spread * 0.35)} {N(target.Y)}L{N(target.X - spread * 0.35)} {N(target.Y)}Z\"/>");
        }
    }

    private static void AppendSpecks(StringBuilder svg, double w, double h, bool mobile)
    {
        var rnd = new Random(mobile ? 71 : 17);
        var count = mobile ? 14 : 44;
        for (var i = 0; i < count; i++)
        {
            var x = rnd.NextDouble() * w;
            var y = h * 0.45 + rnd.NextDouble() * h * 0.55;
            var r = 1 + i % 3 * 0.5;
            svg.Append(CultureInfo.InvariantCulture,
                $"<circle class=\"career-scene__speck career-scene__speck--{i % 3}\" cx=\"{N(x)}\" cy=\"{N(y)}\" r=\"{r.ToString("0.#", CultureInfo.InvariantCulture)}\"/>");
        }
    }

    private static void AppendWeed(StringBuilder svg, double w, double h)
    {
        var rnd = new Random(29);
        for (var i = 0; i < 6; i++)
        {
            var x = rnd.NextDouble() * w;
            var height = 70 + rnd.NextDouble() * 90;
            var sway = rnd.NextDouble() * 26 - 13;
            svg.Append(CultureInfo.InvariantCulture,
                $"<path class=\"career-scene__weed\" d=\"M{N(x)} {N(h)}C{N(x + sway)} {N(h - height * 0.35)} {N(x - sway)} {N(h - height * 0.65)} {N(x + sway * 0.6)} {N(h - height)}\"/>");
        }
    }

    private static void AppendTrail(
        StringBuilder svg,
        IReadOnlyList<CareerClimbGeometry.Point> stones,
        int currentIndex,
        bool mobile)
    {
        var lift = mobile ? 7.0 : 16.0;
        for (var i = 0; i < stones.Count - 1; i++)
        {
            var a = stones[i];
            var b = stones[i + 1];
            var cls = i < currentIndex ? "career-scene__trail career-scene__trail--done" : "career-scene__trail";
            svg.Append(CultureInfo.InvariantCulture,
                $"<path class=\"{cls}\" d=\"M{N(a.X)} {N(a.Y - lift)}Q{N((a.X + b.X) / 2)} {N((a.Y + b.Y) / 2 - lift * 2.2)} {N(b.X)} {N(b.Y - lift)}\"/>");
        }
    }

    private static void AppendStones(
        StringBuilder svg,
        IReadOnlyList<CareerClimbGeometry.Point> stones,
        IReadOnlyList<ClimbStone> states,
        bool mobile)
    {
        var rx = mobile ? 22.0 : 48.0;
        var ry = mobile ? 9.0 : 20.0;

        for (var i = 0; i < stones.Count; i++)
        {
            var p = stones[i];
            var state = i < states.Count ? states[i].State : ClimbStoneState.Todo;
            var isDream = state == ClimbStoneState.Dream;
            var sx = isDream ? rx * 1.12 : rx;
            var sy = isDream ? ry * 1.12 : ry;

            if (isDream)
            {
                svg.Append(CultureInfo.InvariantCulture,
                    $"<ellipse class=\"career-scene__glow\" cx=\"{N(p.X)}\" cy=\"{N(p.Y - sy)}\" rx=\"{N(sx * 2.1)}\" ry=\"{N(sy * 3.4)}\"/>");
            }

            svg.Append(CultureInfo.InvariantCulture,
                $"<path class=\"career-scene__stone career-scene__stone--{StateClass(state)}\" d=\"{StonePath(p.X, p.Y, sx, sy)}\"/>");

            if (isDream)
            {
                svg.Append(CultureInfo.InvariantCulture,
                    $"<path class=\"career-scene__stone-rim\" d=\"{StonePath(p.X, p.Y, sx, sy)}\"/>");
            }

            if (state == ClimbStoneState.Done)
            {
                svg.Append(CultureInfo.InvariantCulture,
                    $"<g class=\"career-scene__shard\" transform=\"translate({N(p.X + sx * 0.55)} {N(p.Y - sy * 1.1)}) scale({(mobile ? "0.7" : "1.4")})\"><path d=\"{ShardPath}\"/></g>");
            }
        }
    }

    private static void AppendBubbles(
        StringBuilder svg,
        IReadOnlyList<CareerClimbGeometry.Point> stones,
        int currentIndex,
        bool mobile)
    {
        if (stones.Count == 0)
        {
            return;
        }

        var p = stones[Math.Clamp(currentIndex, 0, stones.Count - 1)];
        var count = mobile ? 3 : 5;
        for (var i = 0; i < count; i++)
        {
            var r = mobile ? 1.6 + i % 3 * 0.8 : 3 + i % 3 * 2.5;
            var dx = (mobile ? 12 : 34) + i % 2 * (mobile ? 6 : 16);
            var dy = (mobile ? 10 : 34) + i * (mobile ? 9 : 40);
            svg.Append(CultureInfo.InvariantCulture,
                $"<circle class=\"career-scene__bubble career-scene__bubble--{i % 3}\" cx=\"{N(p.X + dx)}\" cy=\"{N(p.Y - dy)}\" r=\"{r.ToString("0.#", CultureInfo.InvariantCulture)}\"/>");
        }
    }

    /// <summary>Empty state: the antenna arcs from the lobster towards the dream stone.</summary>
    private static void AppendAntenna(
        StringBuilder svg,
        IReadOnlyList<CareerClimbGeometry.Point> stones,
        int currentIndex,
        bool mobile)
    {
        if (stones.Count < 2)
        {
            return;
        }

        var from = stones[Math.Clamp(currentIndex, 0, stones.Count - 1)];
        var to = stones[^1];
        var lift = mobile ? 22.0 : 90.0;
        svg.Append(CultureInfo.InvariantCulture,
            $"<path class=\"career-scene__antenna\" d=\"M{N(from.X)} {N(from.Y - lift * 0.4)}Q{N((from.X + to.X) / 2)} {N((from.Y + to.Y) / 2 - lift)} {N(to.X)} {N(to.Y - lift * 0.3)}\"/>");
    }

    private static string StonePath(double cx, double cy, double rx, double ry)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"M{N(cx - rx)} {N(cy)}C{N(cx - rx)} {N(cy - ry * 1.2)} {N(cx - rx * 0.3)} {N(cy - ry * 1.55)} {N(cx + rx * 0.35)} {N(cy - ry * 1.2)}C{N(cx + rx)} {N(cy - ry * 0.9)} {N(cx + rx * 1.05)} {N(cy - ry * 0.2)} {N(cx + rx)} {N(cy)}Z");

    private static string StateClass(ClimbStoneState state) => state switch
    {
        ClimbStoneState.Done => "done",
        ClimbStoneState.Now => "now",
        ClimbStoneState.Dream => "dream",
        _ => "todo"
    };

    private static string N(double value)
        => ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
}
