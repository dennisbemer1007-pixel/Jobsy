using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrListHtml
{
    internal static void AppendListRows(
        StringBuilder sb,
        CultureState culture,
        BanenkaartSsrQuery.State query,
        IReadOnlyList<VacancyListItem> vacancies,
        bool compact)
    {
        sb.Append("<div class=\"kb-list-rows\" data-testid=\"kb-list-rows\">");
        var index = 0;
        foreach (var vacancy in vacancies)
        {
            AppendListRow(sb, culture, query, vacancy, compact, index == 0);
            index++;
        }

        sb.Append("</div>");
    }

    private static void AppendListRow(
        StringBuilder sb,
        CultureState culture,
        BanenkaartSsrQuery.State query,
        VacancyListItem vacancy,
        bool compact,
        bool eager)
    {
        var id = vacancy.Id.ToString("D");
        var href = DetailHref(id, query);
        var title = WebUtility.HtmlEncode(vacancy.Title);
        var company = WebUtility.HtmlEncode(KbIntermediaryDisplay.CompanyLine(culture, vacancy));
        var place = WebUtility.HtmlEncode(PlaceLine(vacancy));
        var wage = FormatWage(culture, vacancy);
        var travel = vacancy.TravelMinutes is int mins and > 0
            ? $"± {mins} {culture["Discovery.Minutes"]} · {UiLabels.TransportVerb(culture, query.Transport)}"
            : null;
        var sector = WebUtility.HtmlEncode(SectorLabel(culture, vacancy));

        sb.Append("<article class=\"kb-list-row");
        if (compact)
        {
            sb.Append(" kb-list-row--compact kb-list-row--card");
        }

        sb.Append("\" data-vacancy-id=\"").Append(id).Append("\" data-testid=\"kb-list-row\">");
        sb.Append("<a class=\"kb-list-row__media\" href=\"").Append(href).Append("\">");
        if (!string.IsNullOrWhiteSpace(vacancy.ImageUrl))
        {
            sb.Append("<img class=\"kb-list-row__photo\" src=\"")
                .Append(WebUtility.HtmlEncode(vacancy.ImageUrl))
                .Append("\" alt=\"\" width=\"400\" height=\"250\" loading=\"")
                .Append(eager ? "eager" : "lazy")
                .Append("\" decoding=\"async\" />");
        }

        if (!string.IsNullOrWhiteSpace(sector))
        {
            sb.Append("<span class=\"kb-list-row__sector\">").Append(sector).Append("</span>");
        }

        sb.Append("<div class=\"kb-list-row__overlay\">");
        sb.Append("<h2 class=\"kb-list-row__title\">").Append(title).Append("</h2>");
        sb.Append("<p class=\"kb-list-row__overlay-meta\">");
        if (travel is not null)
        {
            sb.Append("<span class=\"kb-list-row__travel\">")
                .Append(WebUtility.HtmlEncode(travel))
                .Append("</span>");
        }

        if (!string.IsNullOrWhiteSpace(wage))
        {
            sb.Append("<span class=\"kb-list-row__wage\">")
                .Append(WebUtility.HtmlEncode(wage))
                .Append("</span>");
        }

        sb.Append("</p></div></a>");
        sb.Append("<div class=\"kb-list-row__footer\">");
        sb.Append("<p class=\"kb-list-row__company\">").Append(company).Append("</p>");
        if (!string.IsNullOrWhiteSpace(place))
        {
            sb.Append("<p class=\"kb-list-row__place\">").Append(place).Append("</p>");
        }

        sb.Append("</div></article>");
    }

    private static string DetailHref(string id, BanenkaartSsrQuery.State query)
    {
        var href = "/vacancies/" + id;
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Transport) && !string.Equals(query.Transport, "Fiets", StringComparison.OrdinalIgnoreCase))
        {
            q.Add("transport=" + Uri.EscapeDataString(query.Transport));
        }

        if (query.MaxTravelMinutes is not 20)
        {
            q.Add("maxMinutes=" + query.MaxTravelMinutes);
        }

        return q.Count == 0 ? href : href + "?" + string.Join("&", q);
    }

    private static string? SectorLabel(CultureState culture, VacancyListItem vacancy)
    {
        if (vacancy.IsHighlighted)
        {
            return culture["WgVac.Visibility.Highlighted"];
        }

        if (!string.IsNullOrWhiteSpace(vacancy.CategoryName))
        {
            return vacancy.CategoryName;
        }

        foreach (var wt in vacancy.WorkTypes)
        {
            if (!string.IsNullOrWhiteSpace(wt))
            {
                return UiLabels.WorkType(culture, wt);
            }
        }

        return null;
    }

    private static string? PlaceLine(VacancyListItem vacancy)
    {
        if (!string.IsNullOrWhiteSpace(vacancy.CompanyAddress))
        {
            return vacancy.CompanyAddress.Trim();
        }

        return null;
    }

    private static string? FormatWage(CultureState culture, VacancyListItem vacancy)
    {
        if (!vacancy.WageVisible || vacancy.HourlyWage is not decimal wage)
        {
            return null;
        }

        return $"€ {wage:0.00}{culture["Wage.PerHour"]}";
    }
}
