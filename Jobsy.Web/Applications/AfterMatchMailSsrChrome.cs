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
}
