using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrActiveFilters
{
    internal static void Append(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        var q = model.Query;
        var inner = new StringBuilder();
        var tags = 0;

        if (q.OriginLat is not null)
        {
            tags++;
            var travel = string.Equals(q.Transport, "Fiets", StringComparison.OrdinalIgnoreCase)
                ? culture.Format("Kb.Filter.TravelChipBike", q.MaxTravelMinutes)
                : culture.Format(
                    "Kb.Filter.TravelChip",
                    q.MaxTravelMinutes,
                    UiLabels.TransportVerb(culture, q.Transport));
            AppendTag(inner, "kb-active-tag-travel", travel, culture.Format("Kb.Filter.RemoveTag", travel));
        }

        foreach (var wt in q.WorkTypes)
        {
            tags++;
            AppendTag(inner, "kb-active-tag-worktype", UiLabels.WorkType(culture, wt), null);
        }

        if (q.AgeYears is int age)
        {
            tags++;
            AppendTag(inner, "kb-active-tag-age", culture.Format("Kb.Filter.AgeTag", age), null);
        }

        if (q.MinMatchPercent > 0 && model.ShowFitFilters)
        {
            tags++;
            AppendTag(inner, "kb-active-tag-match", culture.Format("Kb.Filter.MatchTag", q.MinMatchPercent), null);
        }

        if (!string.IsNullOrWhiteSpace(q.SearchQuery))
        {
            tags++;
            AppendTag(inner, "kb-active-tag-search", q.SearchQuery!, null);
        }

        if (tags == 0)
        {
            return;
        }

        sb.Append("<div class=\"kb-active-filters\" data-testid=\"kb-active-filters\">");
        sb.Append(inner);
        sb.Append("<button type=\"button\" class=\"kb-active-filters__clear\" data-testid=\"kb-active-filters-clear\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.Wissen"]))
            .Append("</button></div>");
    }

    private static void AppendTag(StringBuilder sb, string testId, string label, string? removeAria)
    {
        sb.Append("<button type=\"button\" class=\"kb-active-tag\" data-testid=\"")
            .Append(testId)
            .Append('"');
        if (!string.IsNullOrWhiteSpace(removeAria))
        {
            sb.Append(" aria-label=\"")
                .Append(WebUtility.HtmlEncode(removeAria))
                .Append('"');
        }

        sb.Append("><span>")
            .Append(WebUtility.HtmlEncode(label))
            .Append("</span><span class=\"kb-active-tag__remove\" aria-hidden=\"true\">")
            .Append('\u00d7')
            .Append("</span></button>");
    }
}
