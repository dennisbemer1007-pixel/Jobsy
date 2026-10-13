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
        var lines = Wrap(content, 16);
        var sb = new System.Text.StringBuilder();
        sb.Append("<text class=\"").Append(cssClass)
            .Append("\" x=\"").Append(Fmt(x))
            .Append("\" y=\"").Append(Fmt(y))
            .Append("\" text-anchor=\"").Append(anchor)
            .Append("\" dominant-baseline=\"middle\">");
        sb.Append(titleXml);
        for (var i = 0; i < lines.Count; i++)
        {
            if (i == 0)
            {
                sb.Append(WebUtility.HtmlEncode(lines[i]));
            }
            else
            {
                sb.Append("<tspan x=\"").Append(Fmt(x))
                    .Append("\" dy=\"1.15em\">")
                    .Append(WebUtility.HtmlEncode(lines[i]))
                    .Append("</tspan>");
            }
        }

        sb.Append("</text>");
        return sb.ToString();
    }

    private static List<string> Wrap(string? text, int maxChars)
    {
        var words = (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var next = current.Length == 0 ? word : current + " " + word;
            if (next.Length > maxChars && current.Length > 0)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = next;
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return lines.Count == 0 ? [""] : lines;
    }
}
