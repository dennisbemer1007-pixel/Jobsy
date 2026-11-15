using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkAbAnswerScaleHtml
{
    public static string RenderContrastCards(string optionA, string optionB)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="wa-ab-contrast" role="group" aria-labelledby="wa-ab-q-title">""");
        sb.Append("<p id=\"wa-ab-q-title\" class=\"wa-ab-contrast__intro\">Wat past het beste bij jou?</p>");
        sb.Append("<div class=\"wa-ab-contrast__options\">");
        sb.Append("""<div class="wa-ab-contrast__opt wa-ab-contrast__opt--a" data-testid="wa-ab-contrast-a"><span class="wa-ab-contrast__tag" aria-hidden="true">A</span><p>""");
        sb.Append(E(optionA));
        sb.Append("</p></div>");
        sb.Append("""<div class="wa-ab-contrast__opt wa-ab-contrast__opt--b" data-testid="wa-ab-contrast-b"><span class="wa-ab-contrast__tag" aria-hidden="true">B</span><p>""");
        sb.Append(E(optionB));
        sb.Append("</p></div></div></div>");
        return sb.ToString();
    }

    public static string RenderScale(int? selected)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="wa-ab-scale answers answers--contrast" role="radiogroup" aria-label="Jouw keuze op de schaal" data-testid="wa-ab-scale">""");
        foreach (var (value, label) in WerkgeverApkAbContrastScale.Labels)
        {
            var isSel = selected == value;
            sb.Append("<button type=\"button\" class=\"ans");
            if (isSel)
            {
                sb.Append(" sel");
            }

            sb.Append("\" role=\"radio\" data-wa-ab-value=\"");
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" aria-checked=\"");
            sb.Append(isSel ? "true" : "false");
            sb.Append("\" aria-label=\"");
            sb.Append(E($"{label}, optie {value} van 5"));
            sb.Append("\" tabindex=\"");
            sb.Append(TabIndexFor(value, selected));
            sb.Append("\"><span class=\"lvl\" aria-hidden=\"true\"><i style=\"height:");
            sb.Append(WerkgeverApkAbContrastScale.FillPercent(value).ToString(CultureInfo.InvariantCulture));
            sb.Append("%\"></i></span>");
            sb.Append(E(label));
            sb.Append("</button>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static int TabIndexFor(int value, int? selected)
    {
        if (selected is int s)
        {
            return value == s ? 0 : -1;
        }

        return value == WerkgeverApkAbContrastScale.Labels[0].Value ? 0 : -1;
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}