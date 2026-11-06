using System.Globalization;
using System.Linq;
using System.Text;

namespace Jobsy.Web.Scholen;

/// <summary>Inline SVG helpers for teacher class overview (no chart libraries).</summary>
public static class TeacherOverviewChartSvg
{
    public sealed record Segment(string Label, int Value, string Color);

    public sealed record BarItem(string Label, int Value, int? Max = null);

    public static string Donut(
        IReadOnlyList<Segment> segments,
        string emptyLabel,
        int size = 200,
        int stroke = 28)
    {
        var total = segments.Sum(s => Math.Max(0, s.Value));
        if (total <= 0)
        {
            return EmptyDonut(size, stroke, emptyLabel);
        }

        var cx = size / 2d;
        var cy = size / 2d;
        var r = (size - stroke) / 2d;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"""<svg class="sch-chart-donut__svg" viewBox="0 0 {size} {size}" role="img" aria-hidden="true">""");
        var offset = 0d;
        foreach (var seg in segments.Where(s => s.Value > 0))
        {
            var pct = 100d * seg.Value / total;
            var dash = Math.PI * 2 * r * (pct / 100d);
            var gap = Math.PI * 2 * r - dash;
            sb.Append(CultureInfo.InvariantCulture,
                $"""<circle cx="{cx:0.##}" cy="{cy:0.##}" r="{r:0.##}" fill="none" stroke="{seg.Color}" stroke-width="{stroke}" """
                + $"""stroke-dasharray="{dash:0.##} {gap:0.##}" stroke-dashoffset="{-offset:0.##}" transform="rotate(-90 {cx:0.##} {cy:0.##})" />""");
            offset += dash;
        }

        sb.Append(CultureInfo.InvariantCulture,
            $"""<circle cx="{cx:0.##}" cy="{cy:0.##}" r="{r - stroke / 2d:0.##}" fill="var(--surface, #fff)" />""");
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string HorizontalBars(
        IReadOnlyList<BarItem> items,
        string emptyLabel,
        int width = 360,
        int barHeight = 14,
        int gap = 10,
        string barColor = "#0f2747")
    {
        if (items.Count == 0)
        {
            return $"""<p class="sch-chart-empty">{emptyLabel}</p>""";
        }

        var max = items.Max(i => i.Max ?? i.Value);
        max = Math.Max(1, max);
        var labelWidth = 118;
        var chartWidth = width - labelWidth - 36;
        var height = items.Count * (barHeight + gap) + 4;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"""<svg class="sch-chart-bars__svg" viewBox="0 0 {width} {height}" role="img" aria-hidden="true">""");
        var y = 2;
        foreach (var item in items)
        {
            var w = chartWidth * Math.Clamp(item.Value, 0, max) / max;
            sb.Append(CultureInfo.InvariantCulture,
                $"""<text x="0" y="{y + barHeight - 2}" class="sch-chart-bars__label">{Escape(item.Label)}</text>""");
            sb.Append(CultureInfo.InvariantCulture,
                $"""<rect x="{labelWidth}" y="{y}" width="{chartWidth}" height="{barHeight}" rx="4" fill="#e8edf3" />""");
            sb.Append(CultureInfo.InvariantCulture,
                $"""<rect x="{labelWidth}" y="{y}" width="{w:0.##}" height="{barHeight}" rx="4" fill="{barColor}" />""");
            sb.Append(CultureInfo.InvariantCulture,
                $"""<text x="{labelWidth + chartWidth + 6}" y="{y + barHeight - 2}" class="sch-chart-bars__value">{item.Value}</text>""");
            y += barHeight + gap;
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string VerticalColumns(
        IReadOnlyList<BarItem> items,
        string emptyLabel,
        int width = 420,
        int chartHeight = 120,
        int gap = 12,
        string barColor = "#0f2747")
    {
        if (items.Count == 0)
        {
            return $"""<p class="sch-chart-empty">{Escape(emptyLabel)}</p>""";
        }

        var max = Math.Max(1, items.Max(i => i.Value));
        var colWidth = Math.Max(32, (width - gap * (items.Count - 1)) / items.Count);
        var labelHeight = 36;
        var height = chartHeight + labelHeight + 8;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"""<svg class="sch-chart-columns__svg" viewBox="0 0 {width} {height}" role="img" aria-hidden="true">""");
        var x = 0d;
        foreach (var item in items)
        {
            var stem = chartHeight * Math.Clamp(item.Value, 0, max) / max;
            var y = chartHeight - stem;
            sb.Append(CultureInfo.InvariantCulture,
                $"""<rect x="{x:0.##}" y="{y:0.##}" width="{colWidth:0.##}" height="{stem:0.##}" rx="6" fill="{barColor}" />""");
            var label = TruncateLabel(item.Label, 14);
            sb.Append(CultureInfo.InvariantCulture,
                $"""<text x="{x + colWidth / 2d:0.##}" y="{chartHeight + 14}" text-anchor="middle" class="sch-chart-columns__label">{Escape(label)}</text>""");
            sb.Append(CultureInfo.InvariantCulture,
                $"""<text x="{x + colWidth / 2d:0.##}" y="{chartHeight + 28}" text-anchor="middle" class="sch-chart-columns__value">{item.Value}</text>""");
            x += colWidth + gap;
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string TruncateLabel(string label, int maxChars)
        => label.Length <= maxChars ? label : label[..Math.Max(0, maxChars - 1)] + "…";

    private static string EmptyDonut(int size, int stroke, string label)
    {
        var cx = size / 2d;
        var cy = size / 2d;
        var r = (size - stroke) / 2d;
        return $"""
            <svg class="sch-chart-donut__svg is-empty" viewBox="0 0 {size} {size}" role="img" aria-hidden="true">
              <circle cx="{cx:0.##}" cy="{cy:0.##}" r="{r:0.##}" fill="none" stroke="#e2e8f0" stroke-width="{stroke}" />
              <text x="{cx:0.##}" y="{cy:0.##}" text-anchor="middle" dominant-baseline="middle" class="sch-chart-empty-inline">{Escape(label)}</text>
            </svg>
            """;
    }

    private static string Escape(string s)
        => s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
