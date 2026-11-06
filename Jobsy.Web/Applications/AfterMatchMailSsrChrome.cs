using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchMailSsrChrome
{
    internal static void AppendChatDock(StringBuilder sb, CultureState culture, string assistantHref)
    {
        sb.Append("<div class=\"chatdock ndm-mail-chatdock\"><button type=\"button\" class=\"askl\" data-ndm-ask-lobsy data-assistant-fallback=\"");
        sb.Append(WebUtility.HtmlEncode(assistantHref));
        sb.Append("\"><img src=\"/brand/svg/lobsy-merkteken.svg\" alt=\"\">");
        sb.Append(WebUtility.HtmlEncode(culture["Assistant.SubtitleWarm"]));
        sb.Append("</button></div>");
    }

    internal static void AppendMockupFooter(StringBuilder sb, CultureState culture)
    {
        sb.Append("<footer class=\"lf ndm-mockup-footer\"><div class=\"ll\"><a href=\"/privacy\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"/algemene-voorwaarden\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Terms"]));
        sb.Append("</a><a href=\"/toegankelijkheid\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a><a href=\"/bronnen\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Sources"]));
        sb.Append("</a></div><span>© ");
        sb.Append(DateTime.UtcNow.Year);
        sb.Append(" Lobsy</span></footer>");
    }
}
