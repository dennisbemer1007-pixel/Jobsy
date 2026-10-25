using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchBevestigingSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchBevestigingSsrData.PageData data,
        Guid applicationId,
        bool includeBackLink)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ndm ndm-page ndm-mail-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"bevestiging\" data-ndm-bevestiging-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\"><p class=\"crumb\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.Crumb"]));
        sb.Append("</p><h1 style=\"font-size:1.8rem;margin-bottom:14px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.PageTitle"]));
        sb.Append("</h1>");
        AfterMatchMailConfirmationSsrMarkup.AppendGrid(sb, culture, data.Thread, data.MailFields, data);
        if (includeBackLink)
        {
            sb.Append("<p style=\"margin-top:16px\"><a class=\"lk\" href=\"");
            sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Link.BackApplications"]));
            sb.Append("</a></p>");
        }

        sb.Append("</main>");
        AfterMatchMailSsrChrome.AppendChatDock(sb, culture, data.AssistantHref);
        AfterMatchMailSsrChrome.AppendMockupFooter(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }
}
