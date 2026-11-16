using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkAbAnswerScaleHtml
{
    /// <summary>Mockup 13e: A en B naast elkaar; schaal met 5 dots eronder.</summary>
    public static string RenderBipolarRow(string optionA, string optionB, int? selected)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="wa-ab-bipolar" data-testid="wa-ab-bipolar" aria-labelledby="wa-ab-q-title">""");
        sb.Append("<div class=\"wa-ab-bipolar__cards\" data-testid=\"wa-ab-contrast-cards\">");
        sb.Append(Card("a", optionA));
        sb.Append(Card("b", optionB));
        sb.Append("</div>");
        sb.Append("<div class=\"wa-ab-bipolar__scale\">");
        sb.Append(Scale(selected));
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string Card(string side, string text)
    {
        var sb = new StringBuilder();
        sb.Append($"<div class=\"wa-ab-contrast__opt wa-ab-contrast__opt--{side}\" data-testid=\"wa-ab-contrast-{side}\">");
        sb.Append($"<span class=\"wa-ab-contrast__tag\" aria-hidden=\"true\">{side.ToUpperInvariant()}</span>");
        sb.Append("<p>").Append(E(text)).Append("</p></div>");
        return sb.ToString();
    }

    private static string Scale(int? selected)
    {
        var sb = new StringBuilder();
        sb.Append(
            """<div class="wa-ab-dots" role="radiogroup" aria-label="Jouw keuze op de schaal" data-testid="wa-ab-scale">""");
        sb.Append("<div class=\"wa-ab-dots__row\" role=\"presentation\">");
        foreach (var (value, _) in WerkgeverApkAbContrastScale.Labels)
        {
            var isSel = selected == value;
            sb.Append("<button type=\"button\" class=\"wa-ab-dot");
            if (isSel)
            {
                sb.Append(" sel");
            }

            sb.Append("\" role=\"radio\" data-wa-ab-value=\"");
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" aria-checked=\"");
            sb.Append(isSel ? "true" : "false");
            sb.Append("\" aria-label=\"");
            sb.Append(E(LabelFor(value)));
            sb.Append("\" tabindex=\"");
            sb.Append(TabIndexFor(value, selected));
            sb.Append("\"><span class=\"wa-ab-dot__ring\" aria-hidden=\"true\"></span></button>");
        }

        sb.Append("</div><div class=\"wa-ab-dots__labels\" aria-hidden=\"true\">");
        foreach (var (_, label) in WerkgeverApkAbContrastScale.Labels)
        {
            sb.Append("<span class=\"wa-ab-dots__label\">").Append(E(label)).Append("</span>");
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string LabelFor(int value) =>
        WerkgeverApkAbContrastScale.Labels.First(l => l.Value == value).LabelNl + $", optie {value} van 5";

    private static int TabIndexFor(int value, int? selected)
    {
        if (selected is int s)
        {
            return value == s ? 0 : -1;
        }

        return value == WerkgeverApkAbContrastScale.FullA ? 0 : -1;
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}