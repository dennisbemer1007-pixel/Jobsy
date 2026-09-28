using System.Globalization;
using System.Net;

namespace Jobsy.Web.Components.Pages.Candidate;

/// <summary>SVG helpers for radar charts (Blazor reserves the &lt;text&gt; tag in .razor).</summary>
internal static class ScoreRadarSvg
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Fmt(double v) => v.ToString("0.###", Invariant);

    public static string Text(
        string cssClass,
        double x,
        double y,
        string anchor,
        string content,
        string? title = null)
    {
        var titleXml = string.IsNullOrWhiteSpace(title)
            ? ""
            : "<title>" + WebUtility.HtmlEncode(title) + "</title>";
        return string.Concat(
            "<text class=\"", cssClass, "\" x=\"", Fmt(x), "\" y=\"", Fmt(y),
            "\" text-anchor=\"", anchor, "\" dominant-baseline=\"middle\">",
            titleXml,
            WebUtility.HtmlEncode(content),
            "</text>");
    }
}
