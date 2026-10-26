using System.Net;
using System.Text;
using Jobsy.Web.UwvMockups;

namespace Jobsy.Web.Career;

internal static class CareerSsrKansAsideHtml
{
    internal static void Append(StringBuilder sb, string lang)
    {
        sb.Append("<aside class=\"carriere-kans-aside detail-card\" data-testid=\"career-kans-aside\" aria-labelledby=\"carriere-kans-aside-title\">");
        sb.Append("<h2 id=\"carriere-kans-aside-title\" class=\"carriere-kans-aside__title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideTitle(lang)));
        sb.Append("</h2><p class=\"carriere-kans-aside__lead muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideLead(lang)));
        sb.Append("</p><ul class=\"carriere-kans-aside__list\" role=\"list\"><li><strong>");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideDemandTitle(lang)));
        sb.Append("</strong><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideDemandLead(lang)));
        sb.Append("</span></li><li><strong>");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideRetrainTitle(lang)));
        sb.Append("</strong><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideRetrainLead(lang)));
        sb.Append("</span></li></ul><p class=\"carriere-kans-aside__adviseur\">");
        sb.Append("<a href=\"/candidate/adviseur-werk\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideAdviseurLink(lang)));
        sb.Append("</a><br><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.AsideAdviseurNote(lang)));
        sb.Append("</span></p><p class=\"carriere-kans-aside__source muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.SampleSourceFooter(lang)));
        sb.Append("</p></aside>");
    }
}
