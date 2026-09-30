using System.Globalization;
using System.Text;

namespace Jobsy.Web.Components.Leerling.Scene;

/// <summary>Builds the decorative scene SVG markup from build.py <c>scene()</c>.</summary>
public static class LeerlingSceneBuilder
{
    public static string BuildSvg(int depth, int width, int height, double lobsterX)
    {
        depth = Math.Clamp(depth, 0, 11);
        var r = new Random(depth * 7 + width);
        var scale = width > 600 ? 1.0 : 0.42;
        var e = new StringBuilder();

        if (depth == 0)
        {
            var sx = width * 0.9;
            var sy0 = width > 600 ? height * 0.13 : height * 0.2;
            var sr = width > 600 ? 44.0 : 16.0;
            e.Append(CultureInfo.InvariantCulture,
                $"<circle cx=\"{sx}\" cy=\"{sy0}\" r=\"{sr * 1.7}\" class=\"ll-scene__sun-glow\"/>");
            e.Append(CultureInfo.InvariantCulture,
                $"<circle cx=\"{sx}\" cy=\"{sy0}\" r=\"{sr}\" class=\"ll-scene__sun\"/>");
            var wy = height * 0.42;
            e.Append(CultureInfo.InvariantCulture,
                $"<path d=\"M0 {wy}Q{width * .25} {wy - 10} {width * .5} {wy}T{width} {wy}V{height}H0Z\" class=\"ll-scene__sea\"/>");
            e.Append(CultureInfo.InvariantCulture,
                $"<path d=\"M0 {wy + 30}Q{width * .25} {wy + 22} {width * .5} {wy + 30}T{width} {wy + 30}V{height}H0Z\" class=\"ll-scene__sea-soft\"/>");
            var waveCount = width > 600 ? 8 : 3;
            for (var k = 0; k < waveCount; k++)
            {
                var x = r.NextDouble() * 0.8 * width + 0.05 * width;
                var y = wy + 22 + k % 3 * 30 + (r.NextDouble() * 12 - 6);
                e.Append(CultureInfo.InvariantCulture,
                    $"<path d=\"M{x} {y}q18 -7 36 0t36 0\" class=\"ll-scene__foam\"/>");
            }

            var sy = height * 0.70;
            e.Append(CultureInfo.InvariantCulture,
                $"<path d=\"M0 {sy}C{width * .3} {sy - 30} {width * .62} {sy + 10} {width} {sy - 20}V{height}H0Z\" class=\"ll-scene__sand\"/>");
            e.Append(CultureInfo.InvariantCulture,
                $"<path d=\"M0 {sy + 2}C{width * .3} {sy - 28} {width * .62} {sy + 12} {width} {sy - 18}\" class=\"ll-scene__sand-edge\"/>");
        }

        if (depth is 1 or 2)
        {
            var sl = height * (depth == 1 ? 0.16 : 0.05);
            e.Append(CultureInfo.InvariantCulture,
                $"<path d=\"M0 {sl}Q{width * .25} {sl + 8} {width * .5} {sl}T{width} {sl}\" class=\"ll-scene__surface\"/>");
            for (var k = 0; k < 6; k++)
            {
                var x = r.NextDouble() * width;
                var y = r.NextDouble() * (height * 0.6 - sl - 40) + sl + 40;
                e.Append(CultureInfo.InvariantCulture,
                    $"<path d=\"M{x} {y}q14 -6 28 0t28 0\" class=\"ll-scene__foam-soft\"/>");
            }

            var by = height * (depth == 1 ? 0.80 : 0.86);
            e.Append(CultureInfo.InvariantCulture,
                $"<path d=\"M0 {by}C{width * .35} {by - 24} {width * .7} {by + 12} {width} {by - 8}V{height}H0Z\" class=\"ll-scene__sand\"/>");
            if (depth == 2)
            {
                e.Append(Rock(width * 0.9, height, 80 * scale, 50 * scale, 0.35));
                e.Append(Weed(width * 0.93, height - 20, 90 * scale, 10 * scale, Math.Max(3, 6 * scale), 0.4));
            }
        }

        if (depth is >= 3 and <= 6)
        {
            for (var k = 0; k < 3; k++)
            {
                var x = width * (0.15 + 0.3 * k);
                var opacity = 0.10 - 0.012 * depth;
                e.Append(CultureInfo.InvariantCulture,
                    $"<path d=\"M{x} 0L{x + width * .08} 0L{x + width * .18} {height * .8}L{x + width * .02} {height * .8}Z\" style=\"fill:var(--surface);opacity:{opacity:0.###}\"/>");
            }

            var n = 3 + depth;
            for (var k = 0; k < n; k++)
            {
                var x = r.NextDouble() * width;
                e.Append(Weed(x, height + 5, (90 + r.NextDouble() * (110 + depth * 20)) * scale,
                    (r.NextDouble() * 36 - 18) * scale, Math.Max(3, (5 + r.NextDouble() * 4) * scale),
                    0.45 + depth * 0.03));
            }

            e.Append(Rock(width * 0.08, height, 140 * scale, 90 * scale, 0.55));
            e.Append(Rock(width * 0.95, height, 170 * scale, 120 * scale, 0.55));
            e.Append(Rock(width * 0.55, height, 90 * scale, 40 * scale, 0.45));
            if (depth >= 4)
            {
                e.Append(Rock(-20, height * 0.55, 90 * scale, 160 * scale, 0.35));
            }

            if (depth == 6)
            {
                e.Append(CultureInfo.InvariantCulture,
                    $"<ellipse cx=\"{lobsterX}\" cy=\"{height * .66}\" rx=\"{Math.Min(width * .24, 300)}\" ry=\"{height * .34}\" class=\"ll-scene__calm\"/>");
            }
        }

        if (depth is >= 7 and <= 10)
        {
            var k = depth - 7;
            var count = width > 600 ? 45 + 15 * k : 26 + 6 * k;
            for (var i = 0; i < count; i++)
            {
                e.Append(CultureInfo.InvariantCulture,
                    $"<circle cx=\"{r.NextDouble() * width:0}\" cy=\"{r.NextDouble() * height:0}\" r=\"{0.8 + r.NextDouble() * 1.4:0.0}\" style=\"fill:var(--surface);opacity:{0.12 + r.NextDouble() * 0.38:0.00}\"/>");
            }
        }

        if (depth is >= 3 and <= 10)
        {
            var by = height * 0.55;
            var bubbles = depth >= 7 ? 5 : 3;
            for (var j = 0; j < bubbles; j++)
            {
                var rr = 3 + j % 3 * 2.5;
                e.Append(CultureInfo.InvariantCulture,
                    $"<circle class=\"ll-bub ll-bub--{j % 3}\" cx=\"{lobsterX + 50 + j % 2 * 18 - j * 4}\" cy=\"{by - j * 46}\" r=\"{rr}\" />");
            }
        }

        var hAttr = width > 600 ? "100%" : $"{height}px";
        return $"<svg class=\"ll-scene__svg\" viewBox=\"0 0 {width} {height}\" preserveAspectRatio=\"none\" width=\"100%\" height=\"{hAttr}\" aria-hidden=\"true\">{e}</svg>";
    }

    private static string Weed(double x, double bas, double h, double sway, double w, double op)
        => string.Create(CultureInfo.InvariantCulture,
            $"<path d=\"M{x} {bas}C{x + sway} {bas - h * .35} {x - sway} {bas - h * .65} {x + sway * .6} {bas - h}\" class=\"ll-scene__weed\" style=\"opacity:{op:0.##}\" stroke-width=\"{w:0.#}\"/>");

    private static string Rock(double cx, double cy, double rx, double ry, double op)
        => string.Create(CultureInfo.InvariantCulture,
            $"<path d=\"M{cx - rx} {cy}C{cx - rx} {cy - ry * 1.1} {cx - rx * .2} {cy - ry * 1.35} {cx + rx * .3} {cy - ry * 1.1}C{cx + rx} {cy - ry * .8} {cx + rx * 1.05} {cy - ry * .2} {cx + rx} {cy}Z\" class=\"ll-scene__rock\" style=\"opacity:{op:0.##}\"/>");
}
