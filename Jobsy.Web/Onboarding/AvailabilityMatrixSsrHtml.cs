using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

/// <summary>Shared day/slot matrix markup for onboarding step 2 and passport SSR.</summary>
internal static class AvailabilityMatrixSsrHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, HashSet<string> selected, string matrixButtonName)
    {
        var days = AvailabilityDayPartMatrixExport.Days;
        var slots = AvailabilityDayPartMatrixExport.Slots;
        sb.Append("<div class=\"availability-matrix__corner\"></div>");
        foreach (var slot in slots)
        {
            sb.Append("<div class=\"availability-matrix__slot\">");
            sb.Append(WebUtility.HtmlEncode(UiLabels.AvailabilitySlot(culture, slot)));
            sb.Append("</div>");
        }

        foreach (var day in days)
        {
            sb.Append("<div class=\"availability-matrix__day\">");
            sb.Append(WebUtility.HtmlEncode(UiLabels.Weekday(culture, day)));
            sb.Append("</div>");
            foreach (var slot in slots)
            {
                var key = day + ":" + slot;
                var on = selected.Contains(key);
                sb.Append("<button type=\"submit\" name=\"");
                sb.Append(WebUtility.HtmlEncode(matrixButtonName));
                sb.Append("\" class=\"availability-matrix__cell availability-matrix__cell--btn");
                if (on)
                {
                    sb.Append(" is-on");
                }

                sb.Append("\" style=\"");
                sb.Append(on
                    ? "background:color-mix(in srgb, var(--brand) 20%, var(--surface));border:2px solid var(--brand);min-height:44px;box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:var(--brand)"
                    : "background:var(--surface);border:1px solid var(--border);min-height:44px;box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:transparent");
                sb.Append("\" value=\"");
                sb.Append(WebUtility.HtmlEncode(key));
                sb.Append("\" data-ob-key=\"");
                sb.Append(WebUtility.HtmlEncode(key));
                sb.Append("\" aria-pressed=\"");
                sb.Append(on ? "true" : "false");
                sb.Append("\" aria-label=\"");
                sb.Append(WebUtility.HtmlEncode(UiLabels.Weekday(culture, day)));
                sb.Append(' ');
                sb.Append(WebUtility.HtmlEncode(UiLabels.AvailabilitySlot(culture, slot)));
                sb.Append("\">");
                if (on)
                {
                    sb.Append("<span class=\"availability-matrix__mark\" style=\"font-weight:700;font-size:1.125rem;line-height:1\" aria-hidden=\"true\">✓</span>");
                }

                sb.Append("</button>");
            }
        }
    }
}
