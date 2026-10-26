using System.Globalization;
using System.Text;
using Jobsy.Web.Media;

namespace Jobsy.Web.Passport;

internal static class PassportSsrRingHtml
{
    internal static void Append(StringBuilder sb, double competence, double career, double culture, double values, string ariaLabel)
    {
        sb.Append("<svg class=\"passport-dna-ring\" viewBox=\"0 0 120 120\" role=\"img\" aria-label=\"");
        sb.Append(System.Net.WebUtility.HtmlEncode(ariaLabel));
        sb.Append("\">");
        const double cx = 60, cy = 60, r = 46;
        const double gapDeg = 8;
        const double sweep = (360 - gapDeg * 4) / 4.0;
        var arcs = new (string Token, double Progress)[]
        {
            ("var(--brand)", competence),
            ("var(--success)", career),
            ("var(--gold)", culture),
            ("var(--warn)", values),
        };
        var start = -90.0;
        for (var i = 0; i < arcs.Length; i++)
        {
            var (token, progress) = arcs[i];
            var ghost = progress <= 0.001;
            sb.Append(ArcPath(cx, cy, r, start, sweep, ghost ? "var(--border)" : token, ghost ? 0.35 : 0.22, 7));
            if (!ghost)
            {
                sb.Append(ArcPath(cx, cy, r, start, sweep * Math.Clamp(progress, 0, 1), token, 1, 7));
            }

            start += sweep + gapDeg;
        }

        sb.Append("<circle cx=\"60\" cy=\"60\" r=\"34\" fill=\"var(--surface)\" stroke=\"var(--gold-light)\" stroke-width=\"1.5\" stroke-dasharray=\"3 3\" />");
        sb.Append("<image href=\"/");
        sb.Append(BrandImages.Webp128);
        sb.Append("\" x=\"36\" y=\"36\" width=\"48\" height=\"48\" preserveAspectRatio=\"xMidYMid meet\" /></svg>");
    }

    private static string ArcPath(double cx, double cy, double r, double startDeg, double sweepDeg, string stroke, double opacity, double width)
    {
        if (sweepDeg <= 0.01)
        {
            return "";
        }

        var start = Polar(cx, cy, r, startDeg);
        var end = Polar(cx, cy, r, startDeg + sweepDeg);
        var large = sweepDeg > 180 ? 1 : 0;
        var d = FormattableString.Invariant(
            $"M {start.x:0.###} {start.y:0.###} A {r:0.###} {r:0.###} 0 {large} 1 {end.x:0.###} {end.y:0.###}");
        var op = opacity.ToString("0.###", CultureInfo.InvariantCulture);
        var w = width.ToString("0.###", CultureInfo.InvariantCulture);
        return $"<path d=\"{d}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{w}\" stroke-linecap=\"round\" opacity=\"{op}\" />";
    }

    private static (double x, double y) Polar(double cx, double cy, double r, double deg)
    {
        var rad = deg * Math.PI / 180d;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }
}
