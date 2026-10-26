using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Passport;

internal static class PassportSsrHeroHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, string? firstName)
    {
        var greeting = BuildGreeting(culture, firstName);
        var band = CompactHeroPreviewCream.PaspoortKompasWide;
        var artDesktop = CandidateIllustratedImages.KompasHeroWide;
        var artMobile = CandidateIllustratedImages.MobileFigureUrl(artDesktop) ?? artDesktop;

        sb.Append("<header class=\"cand-hero cand-compact-hero cand-hero--compact-preview-c cand-compact-hero--onboarding\" data-testid=\"passport-greeting\" style=\"--cand-compact-hero-band:");
        sb.Append(band);
        sb.Append("\"><div class=\"cand-compact-hero__copy cand-hero__copy\"><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(greeting));
        sb.Append("</h1><p class=\"cand-hero__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Greeting.Lead"]));
        sb.Append("</p></div><div class=\"cand-compact-hero__art cand-hero__compact-c-art\" aria-hidden=\"true\">");
        sb.Append("<img class=\"cand-compact-hero__art-img cand-compact-hero__art-img--desktop\" src=\"");
        sb.Append(WebUtility.HtmlEncode(artDesktop));
        sb.Append("\" alt=\"\" loading=\"eager\" fetchpriority=\"high\" decoding=\"async\" data-testid=\"passport-hero-art-desktop\" />");
        sb.Append("<img class=\"cand-compact-hero__art-img cand-compact-hero__art-img--mobile\" src=\"");
        sb.Append(WebUtility.HtmlEncode(artMobile));
        sb.Append("\" alt=\"\" loading=\"eager\" fetchpriority=\"high\" decoding=\"async\" data-testid=\"cand-compact-hero-mobile-art\" />");
        sb.Append("</div></header>");
    }

    private static string BuildGreeting(CultureState culture, string? firstName)
    {
        var hour = DateTime.Now.Hour;
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return hour switch
            {
                < 12 => culture["Kompas.Greeting.PlainMorning"],
                < 18 => culture["Kompas.Greeting.PlainAfternoon"],
                _ => culture["Kompas.Greeting.PlainEvening"],
            };
        }

        var key = hour switch
        {
            < 12 => "Kompas.Greeting.Morning",
            < 18 => "Kompas.Greeting.Afternoon",
            _ => "Kompas.Greeting.Evening",
        };
        return culture.Format(key, firstName.Trim());
    }
}
