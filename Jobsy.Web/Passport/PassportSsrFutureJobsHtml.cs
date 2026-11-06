using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Passport;

internal static class PassportSsrFutureJobsHtml
{
    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        FutureJobsList list,
        string regionLabel,
        string regionNote,
        bool showCheck)
    {
        sb.Append("<section class=\"detail-card future-jobs\" data-testid=\"future-jobs\" aria-labelledby=\"passport-future-jobs-title\">");
        sb.Append("<p class=\"future-jobs__eyebrow\"><i class=\"future-jobs__dot\" aria-hidden=\"true\"></i>");
        sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.Eyebrow"]));
        sb.Append("</p><h3 id=\"passport-future-jobs-title\" class=\"future-jobs__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.Title"]));
        sb.Append("</h3>");

        if (list.NeedsTest)
        {
            sb.Append("<p class=\"future-jobs__empty\" data-testid=\"future-jobs-empty\">");
            sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.Empty"]));
            sb.Append("</p><a class=\"btn btn-primary future-jobs__empty-cta\" href=\"/candidate/career\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.EmptyCta"]));
            sb.Append("</a></section>");
            return;
        }

        sb.Append("<p class=\"future-jobs__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.Lead"]));
        sb.Append("</p><div class=\"future-jobs__where\"><span class=\"future-jobs__region\">");
        sb.Append(WebUtility.HtmlEncode(regionLabel));
        sb.Append("</span><small>");
        sb.Append(WebUtility.HtmlEncode(regionNote));
        sb.Append("</small></div>");

        if (list.Items.Count == 0)
        {
            sb.Append("<p class=\"future-jobs__none\" data-testid=\"future-jobs-none\">");
            sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.None"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ol class=\"future-jobs__list\">");
            for (var index = 0; index < list.Items.Count; index++)
            {
                var row = list.Items[index];
                var number = index + 1;
                var need = culture[$"FutureJobs.Need.{row.ItkbRank}"];
                sb.Append("<li class=\"future-jobs__row");
                if (number <= 3)
                {
                    sb.Append(" future-jobs__row--top");
                }

                sb.Append("\"><span class=\"future-jobs__nr\">");
                sb.Append(number);
                sb.Append("</span><div><h4 class=\"future-jobs__name\">");
                sb.Append(WebUtility.HtmlEncode(row.TitleNl));
                sb.Append("</h4><div class=\"future-jobs__need\" aria-label=\"");
                sb.Append(WebUtility.HtmlEncode(culture.Format("FutureJobs.Bar", need, Math.Clamp(row.ItkbRank + 1, 0, 5))));
                sb.Append("\"><b>");
                sb.Append(WebUtility.HtmlEncode(need));
                sb.Append("</b></div><p class=\"future-jobs__open\"><strong>");
                sb.Append(WebUtility.HtmlEncode(culture.Format("FutureJobs.Openings", Openings(row))));
                sb.Append("</strong></p>");
                if (!string.IsNullOrWhiteSpace(row.AiLine))
                {
                    sb.Append("<p class=\"future-jobs__ai\"><span>");
                    sb.Append(WebUtility.HtmlEncode(row.AiLine));
                    sb.Append("</span></p>");
                }

                sb.Append("<div class=\"future-jobs__acts\">");
                if (showCheck)
                {
                    sb.Append("<button type=\"button\" class=\"future-jobs__link\" data-passport-fit-future-check=\"");
                    sb.Append(WebUtility.HtmlEncode(row.TitleNl));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(culture["FutureJobs.Check"]));
                    sb.Append("</button>");
                }

                sb.Append("</div></div></li>");
            }

            sb.Append("</ol>");
        }

        var source = string.Format(CultureInfo.InvariantCulture, OccupationOutlook.SourceLine, list.Peildatum)
                     + " "
                     + culture["FutureJobs.Country"];
        sb.Append("<p class=\"future-jobs__source\">");
        sb.Append(WebUtility.HtmlEncode(source));
        sb.Append("</p></section>");
    }

    private static string Openings(FutureJobRow row)
    {
        if (row.OpeningsPer100 <= 0)
        {
            return "0";
        }

        return row.OpeningsPer100.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
