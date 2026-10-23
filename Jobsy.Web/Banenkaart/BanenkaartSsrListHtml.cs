using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

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
        var href = "/vacancies/" + id;
        var title = WebUtility.HtmlEncode(vacancy.Title);
        var company = WebUtility.HtmlEncode(KbIntermediaryDisplay.CompanyLine(culture, vacancy));
        var wage = FormatWage(culture, vacancy);
        var travel = vacancy.TravelMinutes is int mins and > 0
            ? $"± {mins} {culture["Discovery.Minutes"]} · {UiLabels.TransportVerb(culture, query.Transport)}"
            : null;

        sb.Append("<article class=\"kb-list-row");
        if (compact)
        {
            sb.Append(" kb-list-row--compact");
        }

        sb.Append("\" data-vacancy-id=\"").Append(id).Append("\" data-testid=\"kb-list-row\">");
        sb.Append("<a class=\"kb-list-row__media\" href=\"").Append(href).Append("\">");
        if (!string.IsNullOrWhiteSpace(vacancy.ImageUrl))
        {
            sb.Append("<img class=\"kb-list-row__photo\" src=\"")
                .Append(WebUtility.HtmlEncode(vacancy.ImageUrl))
                .Append("\" alt=\"\" width=\"400\" height=\"267\" loading=\"")
                .Append(eager ? "eager" : "lazy")
                .Append("\" decoding=\"async\" />");
        }

        if (compact && travel is not null)
        {
            sb.Append("<span class=\"kb-list-row__travel-badge\">")
                .Append(WebUtility.HtmlEncode(travel))
                .Append("</span>");
        }

        sb.Append("</a><div class=\"kb-list-row__body\"><div class=\"kb-list-row__top\">");
        sb.Append("<div class=\"kb-list-row__titles\"><h2 class=\"kb-list-row__title\"><a href=\"")
            .Append(href)
            .Append("\">")
            .Append(title)
            .Append("</a></h2><p class=\"kb-list-row__company\">")
            .Append(company)
            .Append("</p></div>");
        if (!compact && travel is not null)
        {
            sb.Append("<div class=\"kb-list-row__travel\"><span>")
                .Append(WebUtility.HtmlEncode(travel))
                .Append("</span></div>");
        }

        sb.Append("</div>");
        var why = KbFitMapping.WhyLine(vacancy, culture);
        if (!string.IsNullOrWhiteSpace(why))
        {
            sb.Append("<p class=\"kb-why-line\">").Append(WebUtility.HtmlEncode(why)).Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(wage))
        {
            sb.Append("<div class=\"kb-list-row__meta\"><span class=\"kb-list-row__meta-item\">")
                .Append(WebUtility.HtmlEncode(wage))
                .Append("</span></div>");
        }

        sb.Append("<div class=\"kb-list-row__actions\"><a class=\"kb-list-row__view\" href=\"")
            .Append(href)
            .Append("\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.List.View"]))
            .Append("</a></div></div></article>");
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
