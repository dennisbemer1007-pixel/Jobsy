using System.Text;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilWereldSsrHtml
{
    private sealed record WorldCard(string Key, string ImageStem, string BgPos, string TitleKey, string DescKey, string Accent);

    private static readonly WorldCard[] Cards =
    [
        new(PupilJourneyTheme.ZeeExpeditie, "haven", "center 45%", "Leerling.Wereld.Zee.Title", "Leerling.Wereld.Zee.Desc", "#10284a"),
        new(PupilJourneyTheme.Eilandtuin, "tuin-baai", "center 60%", "Leerling.Wereld.Tuin.Title", "Leerling.Wereld.Tuin.Desc", "#3a2230"),
        new(PupilJourneyTheme.Sterrenkust, "sterren-kaart", "center 30%", "Leerling.Wereld.Ster.Title", "Leerling.Wereld.Ster.Desc", "#141c36"),
    ];

    public static string BuildBody(CultureState culture, string antiforgeryHtml, string? savedTheme, string savePath)
    {
        var sb = new StringBuilder();
        sb.Append("""<section class="band kz" aria-hidden="true"></section><div class="wrap kz-overlap"><div class="card kz-card">""");
        sb.Append("<div class=\"kz-head\"><div><p class=\"kicker\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.Kicker"]));
        sb.Append("</p><h1 class=\"kz-h serif\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.Heading"]));
        sb.Append("</h1><p class=\"kz-lead\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.Lead"])).Append("</p></div></div>");
        sb.Append("<div class=\"worlds\" role=\"group\">");
        foreach (var card in Cards)
        {
            var selected = string.Equals(savedTheme, card.Key, StringComparison.OrdinalIgnoreCase);
            sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\">");
            sb.Append(antiforgeryHtml);
            sb.Append("<input type=\"hidden\" name=\"ThemeKey\" value=\"").Append(PupilLeerlingSsrParts.E(card.Key)).Append("\" />");
            sb.Append("<button type=\"button\" data-ll-wereld-card=\"1\" class=\"wcard");
            if (selected) sb.Append(" sel");
            sb.Append("\" data-testid=\"ll-wereld-").Append(PupilLeerlingSsrParts.E(card.Key)).Append("\">");
            sb.Append("<div class=\"wimg\" style=\"background-image:image-set(url('/leerling/werelden/img/").Append(card.ImageStem);
            sb.Append("-390w.jpg') 1x);background-position:").Append(card.BgPos).Append("\"></div>");
            sb.Append("<div class=\"wbody\"><h2 class=\"serif\">").Append(PupilLeerlingSsrParts.E(culture[card.TitleKey]));
            sb.Append("</h2><p>").Append(PupilLeerlingSsrParts.E(culture[card.DescKey])).Append("</p></div></button></form>");
        }
        sb.Append("</div></div></div>");
        return sb.ToString();
    }
}
