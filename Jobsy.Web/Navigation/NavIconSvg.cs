namespace Jobsy.Web.Navigation;

/// <summary>Helpers for inline SVG icon markup.</summary>
public static class NavIconSvg
{
    /// <summary>Fallback when route CSS fails: inline SVGs without width/height can expand to the viewport.</summary>
    public static string WithIntrinsicSize(string? svg, int size = 24)
    {
        if (string.IsNullOrWhiteSpace(svg))
        {
            return svg ?? "";
        }

        const string open = "<svg";
        var idx = svg.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return svg;
        }

        var tagEnd = svg.IndexOf('>', idx);
        var tag = tagEnd > idx ? svg[idx..tagEnd] : svg[idx..];
        if (HasExplicitWidth(tag))
        {
            return svg;
        }

        var after = idx + open.Length;
        return string.Concat(svg.AsSpan(0, after), $" width=\"{size}\" height=\"{size}\"", svg.AsSpan(after));
    }

    private static bool HasExplicitWidth(string openTag)
    {
        for (var i = 0; i < openTag.Length; i++)
        {
            if (openTag[i] is not ('w' or 'W'))
            {
                continue;
            }

            if (i > 0 && !char.IsWhiteSpace(openTag[i - 1]))
            {
                continue;
            }

            var rest = openTag.AsSpan(i);
            if (rest.StartsWith("width=\"", StringComparison.OrdinalIgnoreCase)
                || rest.StartsWith("width='", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
