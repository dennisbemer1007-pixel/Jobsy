using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class ApplicationsSsrClusterTabsSsr
{
    internal const string ActiveSollicitaties = "sollicitaties";
    internal const string ActiveBerichten = "berichten";
    internal const string ActiveActiviteiten = "activiteiten";
    internal const string ActiveLiked = "liked";

    internal sealed record TabContext(
        bool AfterMatchEnabled,
        bool ShowActiviteitenTab,
        int UnreadMessagesTotal,
        int SavedCount,
        string Active);

    internal static void AppendSegment(StringBuilder sb, CultureState culture, TabContext ctx)
    {
        sb.Append("<nav class=\"sollicitaties-segment\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("\">");
        AppendTabLink(
            sb,
            ctx,
            ActiveSollicitaties,
            ApplicationsSsrQuery.BuildUrl(ApplicationsSsrQuery.TabSollicitaties, ApplicationsSsrQuery.FilterAll, null),
            culture["Nav.ApplicationsTab"],
            badge: null);
        if (ctx.AfterMatchEnabled)
        {
            AppendTabLink(
                sb,
                ctx,
                ActiveBerichten,
                ApplicationsSsrQuery.BuildUrl(ApplicationsSsrQuery.TabBerichten, ApplicationsSsrQuery.FilterAll, null),
                culture["Apps.Ndm.Messages"],
                badge: ctx.UnreadMessagesTotal > 0 ? ctx.UnreadMessagesTotal : null);
        }

        if (ctx.ShowActiviteitenTab)
        {
            AppendTabLink(
                sb,
                ctx,
                ActiveActiviteiten,
                ApplicationsSsrQuery.BuildUrl(ApplicationsSsrQuery.TabActiviteiten, ApplicationsSsrQuery.FilterAll, null),
                culture["Nav.ActivitiesTab"],
                badge: null);
        }

        sb.Append("<a class=\"sollicitaties-segment__tab");
        if (string.Equals(ctx.Active, ActiveLiked, StringComparison.Ordinal))
        {
            sb.Append(" is-active\" aria-current=\"page");
        }

        sb.Append("\" href=\"/candidate/liked\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.SavedTab"]));
        if (ctx.SavedCount > 0)
        {
            sb.Append("<span class=\"sollicitaties-segment__badge\" aria-hidden=\"true\">");
            sb.Append(ctx.SavedCount);
            sb.Append("</span>");
        }

        sb.Append("</a></nav>");
    }

    private static void AppendTabLink(
        StringBuilder sb,
        TabContext ctx,
        string key,
        string href,
        string label,
        int? badge)
    {
        var active = string.Equals(ctx.Active, key, StringComparison.Ordinal);
        sb.Append("<a class=\"sollicitaties-segment__tab");
        if (active)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\" data-enhance-nav=\"false\"");
        if (active)
        {
            sb.Append(" aria-current=\"page\"");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(label));
        if (badge is > 0)
        {
            sb.Append("<span class=\"sollicitaties-segment__badge\" aria-hidden=\"true\">");
            sb.Append(badge.Value);
            sb.Append("</span>");
        }

        sb.Append("</a>");
    }
}
