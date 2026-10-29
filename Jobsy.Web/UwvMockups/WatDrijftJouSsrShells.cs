using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.UwvMockups;

internal static class WatDrijftJouSsrShells
{
    internal static string BuildBody(CultureState culture, WatDrijftJouSsrData data)
    {
        var sb = new StringBuilder(2048);
        var lang = culture.Language;
        if (!data.UwvEnabled)
        {
            sb.Append("<div class=\"panel-page\"><p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Error"]));
            sb.Append("</p></div>");
            return sb.ToString();
        }

        sb.Append("<div class=\"journey-page panel-page--cand-ill\">");
        sb.Append("<header class=\"cand-hero cand-hero--kompas-band cand-hero--with-art opl-wrap\"><div class=\"cand-hero__copy\">");
        sb.Append("<span class=\"cand-hero__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "My results" : "Mijn resultaten"));
        sb.Append("</span><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.ResultsTitle(lang)));
        sb.Append("</h1><p class=\"cand-hero__lead\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.Lead(lang)));
        sb.Append("</p></div></header><div class=\"opl-wrap\">");
        AppendDrijfverenCard(sb, lang, data);
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static void AppendDrijfverenCard(StringBuilder sb, string lang, WatDrijftJouSsrData data)
    {
        sb.Append("<section class=\"detail-card career-card uwv-block uwv-drijfveren\" aria-labelledby=\"uwv-drijfveren-title\">");
        sb.Append("<p class=\"uwv-block__eyebrow\"><i class=\"uwv-block__dot\" aria-hidden=\"true\"></i>");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.SectionTitle(lang)));
        sb.Append("</p><h2 id=\"uwv-drijfveren-title\" class=\"uwv-block__title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.CardTitle(lang)));
        sb.Append("</h2><p class=\"uwv-block__lead\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.CardLead(lang)));
        sb.Append("</p><ul class=\"uwv-drijfveren__chips\">");
        foreach (var d in data.Drivers)
        {
            sb.Append("<li class=\"uwv-drijfveren__chip\"><strong>");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? d.LabelEn : d.LabelNl));
            sb.Append("</strong><p>");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? d.ExplainEn : d.ExplainNl));
            sb.Append("</p></li>");
        }

        sb.Append("</ul><p class=\"uwv-drijfveren__adviseur\"><a class=\"uwv-inline-link\" href=\"/candidate/adviseur-werk\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.AdviseurLink(lang)));
        sb.Append("</a></p></section>");
    }
}
