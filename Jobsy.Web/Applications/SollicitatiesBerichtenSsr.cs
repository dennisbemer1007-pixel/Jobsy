using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class SollicitatiesBerichtenSsr
{
    internal static void AppendBerichtenTab(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data)
    {
        var threads = data.AllRows
            .Where(r => r.AfterMatch is not null && r.ApplicationId is Guid)
            .OrderByDescending(r => r.AfterMatch!.LastMessageAtUtc ?? r.SortAtUtc)
            .ToList();

        if (threads.Count == 0)
        {
            sb.Append("<div class=\"sollic-berichten-empty empty\">");
            sb.Append("<p class=\"sollic-berichten-empty__title\">");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.Berichten.EmptyTitle"]));
            sb.Append("</p><p class=\"muted small\">");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.Berichten.EmptyBody"]));
            sb.Append("</p><a class=\"btn btn-primary\" href=\"");
            sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.BuildUrl(
                ApplicationsSsrQuery.TabSollicitaties,
                ApplicationsSsrQuery.FilterAll,
                null)));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
            sb.Append("</a></div>");
            return;
        }

        var selectedId = data.BerichtenThreadId ?? threads[0].ApplicationId;
        var selectedRow = threads.FirstOrDefault(r => r.ApplicationId == selectedId) ?? threads[0];
        var thread = data.BerichtenThread;

        sb.Append("<div class=\"sollic-berichten thr\" data-testid=\"sollicitaties-berichten-tab\">");
        sb.Append("<div class=\"sollic-berichten tlist\">");
        foreach (var row in threads)
        {
            AppendThreadItem(sb, culture, data, row, row.ApplicationId == selectedRow.ApplicationId);
        }

        sb.Append("<div class=\"sollic-berichten tsafe\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Berichten.SafePanel"]));
        sb.Append("</div></div>");

        if (thread is not null && selectedRow.ApplicationId is Guid appId)
        {
            AppendThreadMain(sb, culture, selectedRow, appId, thread);
        }

        sb.Append("</div>");
    }

    private static void AppendThreadItem(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data,
        SollicitationRow row,
        bool active)
    {
        var appId = row.ApplicationId!.Value;
        var deskHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabBerichten,
            ApplicationsSsrQuery.FilterAll,
            null,
            appId);
        var mobHref = AfterMatchSollicitatiesUrls.Messages(appId);
        sb.Append("<a class=\"sollic-berichten ti sollic-berichten-ti--desk");
        if (active)
        {
            sb.Append(" on");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(deskHref));
        sb.Append("\" data-enhance-nav=\"false\"><span class=\"logo-sq\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><div><b>");
        sb.Append(WebUtility.HtmlEncode(row.CompanyName));
        sb.Append("</b><p>");
        sb.Append(WebUtility.HtmlEncode(
            SollicitatiesBerichtenPresentation.ThreadSubtitle(culture, row, active ? data.BerichtenThread : null)));
        sb.Append("</p><p>");
        sb.Append(WebUtility.HtmlEncode(SollicitatiesBerichtenPresentation.ThreadListPreview(culture, row)));
        sb.Append("</p></div>");
        if (row.AfterMatch!.UnreadMessageCount > 0)
        {
            sb.Append("<span class=\"n\">");
            sb.Append(row.AfterMatch.UnreadMessageCount);
            sb.Append("</span>");
        }

        sb.Append("</a>");

        sb.Append("<a class=\"sollic-berichten ti sollic-berichten-ti--mob");
        if (active)
        {
            sb.Append(" on");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(mobHref));
        sb.Append("\" data-enhance-nav=\"false\"><span class=\"logo-sq\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><div><b>");
        sb.Append(WebUtility.HtmlEncode(row.CompanyName));
        sb.Append("</b><p>");
        sb.Append(WebUtility.HtmlEncode(SollicitatiesBerichtenPresentation.ThreadSubtitle(culture, row, null)));
        sb.Append("</p><p>");
        sb.Append(WebUtility.HtmlEncode(SollicitatiesBerichtenPresentation.ThreadListPreview(culture, row)));
        sb.Append("</p></div>");
        if (row.AfterMatch!.UnreadMessageCount > 0)
        {
            sb.Append("<span class=\"n\">");
            sb.Append(row.AfterMatch.UnreadMessageCount);
            sb.Append("</span>");
        }

        sb.Append("</a>");
    }

    private static void AppendThreadMain(
        StringBuilder sb,
        CultureState culture,
        SollicitationRow row,
        Guid appId,
        AfterMatchThreadDto thread)
    {
        sb.Append("<div class=\"sollic-berichten tmain\">");
        sb.Append("<div class=\"sollic-berichten thd\"><span class=\"logo-sq\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><div><b>");
        var headContact = thread.WithWhom ?? row.CompanyName;
        sb.Append(WebUtility.HtmlEncode($"{headContact} · {row.CompanyName}"));
        sb.Append("</b><p>");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Apps.Berichten.AboutRole"], row.Title)));
        sb.Append("</p></div><a class=\"lnk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabSollicitaties,
            ApplicationsSsrQuery.FilterAll,
            row.RowId)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Berichten.ViewApplication"]));
        sb.Append("</a></div>");

        if (SollicitatiesBerichtenPresentation.BothYesSystemLine(culture, thread) is { } bothYes)
        {
            sb.Append("<div class=\"sollic-berichten sys\">");
            sb.Append(WebUtility.HtmlEncode(bothYes));
            sb.Append("</div>");
        }

        foreach (var message in thread.Messages)
        {
            sb.Append("<div class=\"sollic-berichten msg");
            if (message.IsMine)
            {
                sb.Append(" me");
            }

            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(message.Body));
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(SollicitatiesBerichtenPresentation.FormatMessageMeta(culture, message)));
            sb.Append("</small></div>");
        }

        if (SollicitatiesBerichtenPresentation.SlotProposalSystemLine(culture, thread) is { } slotsLine)
        {
            sb.Append("<div class=\"sollic-berichten sysact\"><span>");
            sb.Append(WebUtility.HtmlEncode(slotsLine));
            sb.Append("</span><a class=\"btn btn-primary btn--sm\" href=\"");
            sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.PickSlot(appId)));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.PickSlot"]));
            sb.Append("</a></div>");
        }
        else if (thread.InterviewStatus == InterviewStatus.Scheduled && thread.ScheduledStartUtc is DateTime start)
        {
            sb.Append("<div class=\"sollic-berichten sysact\"><span>");
            sb.Append(WebUtility.HtmlEncode(string.Format(
                culture["Apps.Berichten.SysScheduled"],
                SollicitatiesBerichtenPresentation.FormatShortDate(culture, start))));
            sb.Append("</span><a class=\"btn btn-primary btn--sm\" href=\"");
            sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.AfterMatchBase(appId)));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.ViewInterview"]));
            sb.Append("</a></div>");
        }

        sb.Append("<a class=\"sollic-berichten compose\" href=\"");
        sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Messages(appId)));
        sb.Append("\" data-enhance-nav=\"false\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("</span><span class=\"btn btn-primary\" aria-hidden=\"true\">");
        sb.Append(SollicitatiesInlineIcons.Send16);
        sb.Append("</span></a><p class=\"sollic-berichten safe\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Privacy"]));
        sb.Append("</p></div>");
    }
}
