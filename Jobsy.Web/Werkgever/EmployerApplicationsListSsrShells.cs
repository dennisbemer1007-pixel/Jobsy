using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Werkgever;

/// <summary>Static HTML for werkgever sollicitaties list view (<c>?view=lijst</c>).</summary>
internal static class EmployerApplicationsListSsrShells
{
    internal static string BuildRouteShell(CultureState culture, EmployerApplicationsListSsrQuery.Parsed _)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"wg-app wg-app--dense\" data-testid=\"wg-applications-list-ssr\" data-wg-list-ssr=\"1\">");
        sb.Append("<div class=\"wg-card\">");
        AppendViewToggle(sb, culture, listActive: true);
        sb.Append("<p class=\"muted\" role=\"status\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Loading"]));
        sb.Append("</p></div></div>");
        return sb.ToString();
    }

    private static void AppendViewToggle(StringBuilder sb, CultureState culture, bool listActive)
    {
        sb.Append("<div class=\"wg-app__views\" role=\"group\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["WgNav.Applications"]));
        sb.Append("\"><a class=\"");
        sb.Append(listActive ? "" : "is-active");
        sb.Append("\" href=\"/werkgever/sollicitaties/pipeline\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.View.Pipeline"]));
        sb.Append("</a><a class=\"");
        sb.Append(listActive ? "is-active" : "");
        sb.Append("\" href=\"/werkgever/sollicitaties?view=lijst\" aria-current=\"");
        sb.Append(listActive ? "page" : "false");
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.View.List"]));
        sb.Append("</a></div>");
    }
}
