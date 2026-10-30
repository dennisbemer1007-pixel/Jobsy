using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrListHtml
{
    internal static void AppendLagenMockupListRows(
        StringBuilder sb,
        CultureState culture,
        BanenkaartSsrQuery.State query,
        IReadOnlyList<VacancyListItem> vacancies)
    {
        sb.Append("<div class=\"kb-list-rows\" data-testid=\"kb-list-rows\">");
        foreach (var vacancy in vacancies)
        {
            AppendLagenMockupRow(sb, culture, query, vacancy);
        }

        sb.Append("</div>");
    }

    private static void AppendLagenMockupRow(
        StringBuilder sb,
        CultureState culture,
        BanenkaartSsrQuery.State query,
        VacancyListItem vacancy)
    {
        var id = vacancy.Id.ToString("D");
        var href = WebUtility.HtmlEncode(DetailHref(id, query));
        var title = WebUtility.HtmlEncode(vacancy.Title);
        var company = WebUtility.HtmlEncode(KbIntermediaryDisplay.CompanyLine(culture, vacancy));
        var sector = WebUtility.HtmlEncode(SectorLabel(culture, vacancy) ?? "");
        var viaIntermediary = vacancy.IntermediaryCompanyId is not null
            || !string.IsNullOrWhiteSpace(vacancy.OfferedByLabel);
        var typeClass = viaIntermediary ? "tm i" : "tm w";
        var typeLabel = viaIntermediary ? "Via intermediair" : "Bij werkgever";
        var travel = vacancy.TravelMinutes is int mins and > 0
            ? $"{mins} min {UiLabels.TransportVerb(culture, query.Transport)}"
            : "";
        var hours = HoursLabel(culture, vacancy);
        var bg = vacancy.CategoryColorHex is { Length: > 0 } hex && hex.StartsWith('#')
            ? hex
            : "#E3EEDD";
        var fg = "#3F6E31";

        sb.Append("<a class=\"vc\" href=\"").Append(href).Append("\" data-vacancy-id=\"")
            .Append(id).Append("\" data-testid=\"kb-list-row\">");
        var hasPhoto = !string.IsNullOrWhiteSpace(vacancy.ImageUrl);
        if (hasPhoto)
        {
            var photoUrl = Jobsy.Core.Media.VacancyImageUrls.MapPopupPhotoPath(vacancy.Id, mobile: true);
            sb.Append("<div class=\"vimg vimg--photo\" style=\"background:linear-gradient(135deg,")
                .Append(WebUtility.HtmlEncode(bg)).Append(",#fff 150%);\">");
            sb.Append("<img class=\"kb-list-lazy-photo\" alt=\"\" width=\"400\" height=\"250\" decoding=\"async\" ")
                .Append("data-kb-list-photo=\"1\" data-photo-src=\"")
                .Append(WebUtility.HtmlEncode(photoUrl))
                .Append("\" />");
        }
        else
        {
            sb.Append("<div class=\"vimg\" style=\"background:linear-gradient(135deg,")
                .Append(WebUtility.HtmlEncode(bg)).Append(",#fff 150%);color:")
                .Append(WebUtility.HtmlEncode(fg)).Append("\">");
            sb.Append("<svg class=\"big kb-wt-ico--big\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M4 8l8-4 8 4v8l-8 4-8-4z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.3\"/></svg>");
        }

        if (!string.IsNullOrEmpty(sector))
        {
            sb.Append("<span class=\"vtag\">").Append(sector).Append("</span>");
        }

        sb.Append("</div><div class=\"vb\"><b>").Append(title).Append("</b>");
        sb.Append("<div class=\"who\"><span class=\"").Append(typeClass).Append("\">")
            .Append(WebUtility.HtmlEncode(typeLabel)).Append("</span></div>");
        sb.Append("<div class=\"who\" style=\"margin-top:4px\">").Append(company).Append("</div>");
        sb.Append("<div class=\"meta\">");
        if (!string.IsNullOrEmpty(travel))
        {
            sb.Append("<span>").Append(BanenkaartSsrTransportIcons.MetaTravelSvg(query.Transport))
                .Append(WebUtility.HtmlEncode(travel)).Append("</span>");
        }

        if (!string.IsNullOrEmpty(hours))
        {
            sb.Append("<span>").Append(WebUtility.HtmlEncode(hours)).Append("</span>");
        }

        sb.Append("</div></div></a>");
    }

    private static string? HoursLabel(CultureState culture, VacancyListItem vacancy)
    {
        var min = (int)(vacancy.MinHoursPerWeek ?? 0);
        var max = (int)(vacancy.MaxHoursPerWeek ?? 0);
        if (min <= 0 && max <= 0)
        {
            return null;
        }

        if (max <= 0)
        {
            max = 40;
        }

        return culture.Format("Kb.Filter.HoursValue", min, max);
    }

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
        if (!string.IsNullOrWhiteSpace(query.Transport))
        {
            q.Add("transport=" + Uri.EscapeDataString(query.Transport));
        }

        if (query.MaxTravelMinutes is int maxMinutes)
        {
            q.Add("maxMinutes=" + maxMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return q.Count > 0 ? href + "?" + string.Join("&", q) : href;
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
