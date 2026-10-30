using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

/// <summary>Shared day/slot matrix markup for onboarding step 2 and passport SSR.</summary>
internal static class AvailabilityMatrixSsrHtml
{
    internal enum MatrixInputMode
    {
        SubmitButton,
        BatchCheckbox,
    }

    internal static void Append(StringBuilder sb, CultureState culture, HashSet<string> selected, string matrixButtonName)
        => Append(sb, culture, selected, matrixButtonName, MatrixInputMode.SubmitButton);

    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        HashSet<string> selected,
        string matrixButtonName,
        MatrixInputMode inputMode)
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
                if (inputMode == MatrixInputMode.BatchCheckbox)
                {
                    AppendBatchCheckboxCell(sb, culture, matrixButtonName, day, slot, key, on);
                }
                else
                {
                    AppendSubmitButtonCell(sb, culture, matrixButtonName, day, slot, key, on);
                }
            }
        }
    }

    private static void AppendSubmitButtonCell(
        StringBuilder sb,
        CultureState culture,
        string matrixButtonName,
        string day,
        string slot,
        string key,
        bool on)
    {
        sb.Append("<button type=\"submit\" name=\"");
        sb.Append(WebUtility.HtmlEncode(matrixButtonName));
        sb.Append("\" class=\"availability-matrix__cell availability-matrix__cell--btn");
        if (on)
        {
            sb.Append(" is-on");
        }

        sb.Append("\" style=\"");
        sb.Append(CellStyle(on));
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
            sb.Append(MarkSpan());
        }

        sb.Append("</button>");
    }

    private static void AppendBatchCheckboxCell(
        StringBuilder sb,
        CultureState culture,
        string matrixButtonName,
        string day,
        string slot,
        string key,
        bool on)
    {
        sb.Append("<label class=\"availability-matrix__cell");
        if (on)
        {
            sb.Append(" is-on");
        }

        sb.Append("\" style=\"");
        sb.Append(CellStyle(on));
        sb.Append("\" data-passport-matrix-cell=\"1\" data-ob-key=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\" aria-pressed=\"");
        sb.Append(on ? "true" : "false");
        sb.Append("\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(UiLabels.Weekday(culture, day)));
        sb.Append(' ');
        sb.Append(WebUtility.HtmlEncode(UiLabels.AvailabilitySlot(culture, slot)));
        sb.Append("\"><input type=\"checkbox\" name=\"");
        sb.Append(WebUtility.HtmlEncode(matrixButtonName));
        sb.Append("\" value=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append('"');
        if (on)
        {
            sb.Append(" checked");
        }

        sb.Append(" /");
        sb.Append('>');
        if (on)
        {
            sb.Append(MarkSpan());
        }

        sb.Append("</label>");
    }

    private static string CellStyle(bool on)
        => on
            ? "background:color-mix(in srgb, var(--brand) 20%, var(--surface));border:2px solid var(--brand);min-height:44px;box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:var(--brand)"
            : "background:var(--surface);border:1px solid var(--border);min-height:44px;box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:transparent";

    private static string MarkSpan()
        => "<span class=\"availability-matrix__mark\" style=\"font-weight:700;font-size:1.125rem;line-height:1\" aria-hidden=\"true\">✓</span>";
}
