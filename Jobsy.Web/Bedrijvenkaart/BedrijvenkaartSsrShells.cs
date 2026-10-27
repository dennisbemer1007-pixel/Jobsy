using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
namespace Jobsy.Web.Bedrijvenkaart;

internal static class BedrijvenkaartSsrShells
{
    internal static string BuildBody(CultureState culture, BedrijvenkaartSsrData.PageModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"bk-page panel-page--cand-ill\" data-bedrijvenkaart-ssr=\"1\" data-testid=\"bedrijvenkaart-ssr-root\">");
        sb.Append("<div class=\"bk-page__hero-stack\">");
        sb.Append("<div class=\"bk-map-toggle\" role=\"tablist\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.MapToggle.Jobs"]))
            .Append("\">");
        sb.Append("<a href=\"").Append(WebUtility.HtmlEncode(KbRoutes.Map)).Append("\" role=\"tab\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.MapToggle.Jobs"]))
            .Append("</a>");
        sb.Append("<a href=\"").Append(WebUtility.HtmlEncode(KbRoutes.CompanyMap)).Append("\" class=\"is-active\" role=\"tab\" aria-selected=\"true\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.MapToggle.Companies"]))
            .Append("</a>");
        sb.Append("</div></div>");

        sb.Append("<div class=\"bk-page__frame\">");
        sb.Append("<p class=\"bk-info-banner\">").Append(WebUtility.HtmlEncode(culture["Bk.InfoBanner"])).Append("</p>");

        AppendFilters(sb, culture, model.RadiusKm);
        sb.Append("<p class=\"bk-result-meta\">")
            .Append(model.Pins.Count)
            .Append(" bedrijven binnen ")
            .Append(model.RadiusKm)
            .Append(" km • ")
            .Append(model.OpenCount)
            .Append(" staan open voor jou</p>");

        sb.Append("<div class=\"bk-shell\">");
        sb.Append("<div class=\"bk-list\" role=\"list\">");
        foreach (var pin in model.Pins)
        {
            AppendCard(sb, culture, pin);
        }

        sb.Append("</div><div class=\"bk-map-pane\">");
        sb.Append("<div id=\"bedrijvenkaart-map\" class=\"job-map\" aria-label=\"Kaart\"></div>");
        sb.Append("<div class=\"bk-map-legend\">");
        sb.Append("<div><span class=\"bk-chip bk-chip--open\">")
            .Append(WebUtility.HtmlEncode(culture["Bk.OpenLabel"]))
            .Append("</span></div>");
        sb.Append("<div>Alle bedrijven op Lobsy</div>");
        sb.Append("<div>Uitzendbureau vestiging</div>");
        sb.Append("</div></div></div></div></div>");
        return sb.ToString();
    }

    private static void AppendFilters(StringBuilder sb, CultureState culture, int radiusKm)
    {
        sb.Append("<div class=\"bk-filters\" data-bk-filter-form=\"1\">");
        sb.Append("<div class=\"bk-filters__track\">");
        sb.Append("<div class=\"bk-filters__row\">");
        sb.Append("<div class=\"bk-filters__group\" role=\"group\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.AllSectors"]))
            .Append("\"><button type=\"button\" class=\"jobsy-action is-active\" aria-pressed=\"true\">")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.AllSectors"]))
            .Append("</button></div>");
        sb.Append("<div class=\"bk-filters__group\" role=\"group\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.AllFields"]))
            .Append("\"><button type=\"button\" class=\"jobsy-action is-active\" aria-pressed=\"true\">")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.AllFields"]))
            .Append("</button></div>");
        sb.Append("<div class=\"bk-filters__group\" role=\"group\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.Distance"]))
            .Append("\">");
        foreach (var km in new[] { 10, 25, 40 })
        {
            var active = km == radiusKm;
            sb.Append("<button type=\"button\" class=\"jobsy-action")
                .Append(active ? " is-active" : "")
                .Append("\" aria-pressed=\"")
                .Append(active ? "true" : "false")
                .Append("\" data-bk-radius-km=\"")
                .Append(km)
                .Append("\">")
                .Append(km)
                .Append(" km</button>");
        }

        sb.Append("</div></div>");
        sb.Append("<div class=\"bk-filters__row bk-filters__row--chips\">");
        sb.Append("<button type=\"button\" class=\"jobsy-action\" aria-pressed=\"false\" data-bk-filter=\"openOnly\">")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.OpenOnly"]))
            .Append("</button>");
        sb.Append("<button type=\"button\" class=\"jobsy-action is-active\" aria-pressed=\"true\" data-bk-filter=\"agencies\">")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.Agencies"]))
            .Append("</button>");
        sb.Append("<button type=\"button\" class=\"jobsy-action\" aria-pressed=\"false\" data-bk-filter=\"respondsFast\">")
            .Append(WebUtility.HtmlEncode(culture["Bk.Filter.RespondsFast"]))
            .Append("</button>");
        sb.Append("</div></div></div>");
    }

    private static void AppendCard(StringBuilder sb, CultureState culture, CompanyMapPinDto pin)
    {
        sb.Append("<article class=\"bk-card\" role=\"listitem\" data-bk-company-id=\"")
            .Append(WebUtility.HtmlEncode(pin.CompanyId.ToString("D")))
            .Append("\">");
        sb.Append("<div class=\"bk-card__row\"><div class=\"bk-card__avatar\">")
            .Append(WebUtility.HtmlEncode(Initials(pin.Name)))
            .Append("</div><div>");
        sb.Append("<h2 class=\"bk-card__title\">").Append(WebUtility.HtmlEncode(pin.Name)).Append("</h2>");
        sb.Append("<p class=\"bk-card__meta\">")
            .Append(WebUtility.HtmlEncode(pin.SectorLabel ?? "Bedrijf"))
            .Append(" • ")
            .Append(WebUtility.HtmlEncode(pin.City ?? "Nederland"))
            .Append("</p>");
        if (pin.OpenForInitiative)
        {
            sb.Append("<span class=\"bk-chip bk-chip--open\">")
                .Append(WebUtility.HtmlEncode(culture["Bk.OpenLabel"]))
                .Append("</span>");
        }

        if (pin.VacancyCount > 0)
        {
            sb.Append("<span class=\"bk-chip\">").Append(pin.VacancyCount).Append(" vacatures</span>");
        }
        else
        {
            sb.Append("<span class=\"bk-chip\">Geen vacatures</span>");
        }

        if (pin.MatchLabel is InitiativeMatchLabel ml)
        {
            sb.Append("<span class=\"bk-chip ")
                .Append(ml == InitiativeMatchLabel.Strong ? "bk-chip--match-strong" : "bk-chip--match-good")
                .Append("\">")
                .Append(WebUtility.HtmlEncode(culture[InitiativeMatchWordingRules.ResourceKey(ml)]))
                .Append("</span>");
        }

        sb.Append("</div></div></article>");
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant();
    }
}
