using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerMessagesSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchEmployerMessagesSsrData.PageData data,
        Guid contextApplicationId)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ndm ndm-wg ndm-wg-messages-page\" data-testid=\"ndm-werkgever-live\" data-ndm-view=\"messages\" data-ndm-employer-messages-ssr=\"1\" data-context-application-id=\"");
        sb.Append(contextApplicationId.ToString("D"));
        sb.Append("\" data-messages-base=\"");
        sb.Append(WebUtility.HtmlEncode(data.MessagesBasePath));
        sb.Append("\" data-enhance-nav=\"false\"><div class=\"thr ndm-wg-messages-thr\" data-testid=\"ndm-employer-messages-hub\"><div class=\"tlist\">");

        var avatarIndex = 0;
        foreach (var row in data.Threads)
        {
            AppendThreadItem(sb, culture, data, row, avatarIndex++);
        }

        AppendSafeNote(sb, culture, data);
        sb.Append("</div>");

        foreach (var row in data.Threads)
        {
            var appId = row.Thread.ApplicationId;
            var active = appId == data.SelectedThreadId;
            sb.Append("<div class=\"tmain ndm-wg-messages-tmain\" data-ndm-thread-panel data-ndm-thread-id=\"");
            sb.Append(appId.ToString("D"));
            sb.Append('"');
            if (!active)
            {
                sb.Append(" hidden");
            }

            sb.Append('>');
            AppendThreadMain(sb, culture, row);
            sb.Append("</div>");
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static void AppendThreadItem(
        StringBuilder sb,
        CultureState culture,
        AfterMatchEmployerMessagesSsrData.PageData data,
        AfterMatchEmployerMessagesSsrData.ThreadRow row,
        int avatarIndex)
    {
        var appId = row.Thread.ApplicationId;
        var active = appId == data.SelectedThreadId;
        var href = AfterMatchEmployerSollicitatiesUrls.MessagesThread(data.ContextApplicationId, appId);
        var displayName = AfterMatchEmployerBerichtenPresentation.DisplayName(culture, row.Application);
        var unread = AfterMatchEmployerBerichtenPresentation.UnreadFromCandidate(row.Thread);

        sb.Append("<a class=\"ti ndm-wg-messages-ti");
        if (active)
        {
            sb.Append(" on");
        }

        sb.Append("\" data-ndm-thread-select href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\" data-ndm-thread-id=\"");
        sb.Append(appId.ToString("D"));
        sb.Append("\" data-ndm-candidate=\"");
        sb.Append(WebUtility.HtmlEncode(displayName));
        sb.Append("\" data-enhance-nav=\"false\"");
        if (active)
        {
            sb.Append(" aria-current=\"true\"");
        }

        sb.Append("><span class=\"av ");
        sb.Append(AfterMatchEmployerBerichtenPresentation.AvatarClass(avatarIndex));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(AfterMatchEmployerBerichtenPresentation.Initials(row.Application)));
        sb.Append("</span><div class=\"sp\"><b>");
        sb.Append(WebUtility.HtmlEncode(displayName));
        sb.Append("</b><p>");
        sb.Append(WebUtility.HtmlEncode(
            AfterMatchEmployerBerichtenPresentation.ThreadSubtitle(culture, row.Thread)));
        sb.Append("</p></div>");
        if (unread > 0)
        {
            sb.Append("<span class=\"pill warm\">");
            sb.Append(unread);
            sb.Append("</span>");
        }

        sb.Append("</a>");
    }

    private static void AppendSafeNote(
        StringBuilder sb,
        CultureState culture,
        AfterMatchEmployerMessagesSsrData.PageData data)
    {
        sb.Append("<div style=\"padding:16px\"><div class=\"note\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z\"/></svg><span><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.SafeTitle"]));
        sb.Append("</b> ");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.SafeBody"]));
        sb.Append("</span></div><p class=\"xs muted\" style=\"margin-top:10px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.ArchiveHint"]));
        sb.Append(" <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ArchiveHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.ArchiveLink"]));
        sb.Append("</a>.</p></div>");
    }

    private static void AppendThreadMain(
        StringBuilder sb,
        CultureState culture,
        AfterMatchEmployerMessagesSsrData.ThreadRow row)
    {
        var thread = row.Thread;
        var appId = thread.ApplicationId;
        var displayName = AfterMatchEmployerBerichtenPresentation.DisplayName(culture, row.Application);

        sb.Append("<div class=\"row ndm-wg-messages-thd\"><span class=\"av w\">");
        sb.Append(WebUtility.HtmlEncode(AfterMatchEmployerBerichtenPresentation.Initials(row.Application)));
        sb.Append("</span><div class=\"sp\"><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(displayName));
        sb.Append("</b><div class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(
            AfterMatchEmployerBerichtenPresentation.ThreadSubtitle(culture, thread)));
        sb.Append("</div></div>");
        if (thread.InterviewStatus == InterviewStatus.Scheduled)
        {
            sb.Append("<a class=\"btn line sm hide-m\" href=\"");
            sb.Append(WebUtility.HtmlEncode(AfterMatchEmployerSollicitatiesUrls.Reschedule(appId)));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.SideTitle.Reschedule"]));
            sb.Append("</a>");
        }

        sb.Append("</div><div class=\"thread\">");

        if (AfterMatchEmployerBerichtenPresentation.BothYesSystemLine(culture, thread) is { } bothYes)
        {
            sb.Append("<div class=\"sys\">");
            sb.Append(WebUtility.HtmlEncode(bothYes));
            sb.Append("</div>");
        }

        foreach (var message in thread.Messages)
        {
            sb.Append("<div class=\"msg");
            if (message.IsMine)
            {
                sb.Append(" me");
            }

            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(message.Body));
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(
                AfterMatchEmployerBerichtenPresentation.FormatMessageMeta(culture, thread, message)));
            sb.Append("</small></div>");
        }

        sb.Append("</div>");

        sb.Append("<div class=\"warnin ndm-wg-messages-sensitive\" data-ndm-sensitive-warning hidden><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z\"/></svg><span data-ndm-sensitive-text>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Messages.BlockedEmployer"]));
        sb.Append("</span></div>");

        sb.Append("<form class=\"compose ndm-wg-messages-compose\" data-ndm-compose data-application-id=\"");
        sb.Append(appId.ToString("D"));
        sb.Append("\"><span class=\"ndm-wg-messages-compose-attach\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 11 12 19a5 5 0 0 1-7-7l8-8a3.5 3.5 0 0 1 5 5l-8 8a2 2 0 0 1-3-3l7-7\"/></svg></span><label class=\"visually-hidden\" for=\"ndm-wg-compose-");
        sb.Append(appId.ToString("N"));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("</label><input class=\"ndm-wg-messages-compose-input\" id=\"ndm-wg-compose-");
        sb.Append(appId.ToString("N"));
        sb.Append("\" type=\"text\" name=\"body\" autocomplete=\"off\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("\" /><button type=\"submit\" class=\"btn sm ndm-wg-messages-compose-send\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("\">");
        sb.Append(NdMIcons.Send);
        sb.Append("</button></form><p class=\"xs muted ndm-wg-messages-quick\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.QuickReplies"]));
        sb.Append(": ");
        AppendQuickReply(sb, culture["AfterMatch.Screen11.QuickReply1"]);
        sb.Append(" · ");
        AppendQuickReply(sb, culture["AfterMatch.Screen11.QuickReply2"]);
        sb.Append(" · ");
        AppendQuickReply(sb, culture["AfterMatch.Screen11.QuickReply3"]);
        sb.Append("</p>");
    }

    private static void AppendQuickReply(StringBuilder sb, string text)
    {
        sb.Append("<button type=\"button\" class=\"lk ndm-wg-quick-reply\" data-ndm-quick-reply=\"");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.Append("</button>");
    }
}
