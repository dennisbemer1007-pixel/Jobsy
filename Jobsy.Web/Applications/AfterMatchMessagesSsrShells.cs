using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchMessagesSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchMessagesSsrData.PageData data,
        Guid contextApplicationId)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ndm ndm-page ndm-messages-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"messages\" data-ndm-messages-ssr=\"1\" data-context-application-id=\"");
        sb.Append(contextApplicationId.ToString("D"));
        sb.Append("\" data-messages-base=\"");
        sb.Append(WebUtility.HtmlEncode(data.MessagesBasePath));
        sb.Append("\" data-back-href=\"");
        sb.Append(WebUtility.HtmlEncode(data.BackHref));
        sb.Append("\"><main class=\"wrap\">");

        AppendIntro(sb, culture, data);
        AppendThreads(sb, culture, data);
        sb.Append("</main>");

        AppendChatDock(sb, culture);
        AppendMockupFooter(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendIntro(StringBuilder sb, CultureState culture, AfterMatchMessagesSsrData.PageData data)
    {
        var selectedRow = data.Apps.AllRows.FirstOrDefault(r => r.ApplicationId == data.SelectedThreadId);
        var company = selectedRow?.CompanyName ?? "";
        sb.Append("<section class=\"intro ndm-messages-intro\"><nav class=\"bc ndm-messages-bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\" data-ndm-messages-bc><a href=\"");
        sb.Append(WebUtility.HtmlEncode(KbRoutes.Applications));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a><i aria-hidden=\"true\"> › </i><span data-ndm-bc-company>");
        sb.Append(WebUtility.HtmlEncode(company));
        sb.Append("</span><i aria-hidden=\"true\"> › </i><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Hero.Title"]));
        sb.Append("</span></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Hero.Title"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Hero.Lead"]));
        sb.Append("</p></section>");
    }

    private static void AppendThreads(StringBuilder sb, CultureState culture, AfterMatchMessagesSsrData.PageData data)
    {
        var threads = data.Apps.AllRows
            .Where(SollicitatiesBerichtenPresentation.RowHasMessageThread)
            .OrderByDescending(r => r.AfterMatch!.LastMessageAtUtc ?? r.SortAtUtc)
            .ToList();

        sb.Append("<div class=\"thr ndm-messages-thr\" data-testid=\"ndm-messages-hub\"><div class=\"tlist\">");
        var avatarIndex = 0;
        foreach (var row in threads)
        {
            AppendThreadItem(sb, culture, data, row, avatarIndex++);
        }

        AppendSafeNote(sb, culture, data, threads);
        sb.Append("</div>");

        foreach (var row in threads)
        {
            var appId = row.ApplicationId!.Value;
            if (!data.Apps.BerichtenThreads.TryGetValue(appId, out var thread))
            {
                continue;
            }

            var active = appId == data.SelectedThreadId;
            sb.Append("<div class=\"tmain ndm-messages-tmain\" data-ndm-thread-panel data-ndm-thread-id=\"");
            sb.Append(appId.ToString("D"));
            sb.Append('"');
            if (!active)
            {
                sb.Append(" hidden");
            }

            sb.Append('>');
            AppendThreadMain(sb, culture, row, appId, thread);
            sb.Append("</div>");
        }

        sb.Append("</div>");
    }

    private static void AppendThreadItem(
        StringBuilder sb,
        CultureState culture,
        AfterMatchMessagesSsrData.PageData data,
        SollicitationRow row,
        int avatarIndex)
    {
        var appId = row.ApplicationId!.Value;
        data.Apps.BerichtenThreads.TryGetValue(appId, out var threadDto);
        var active = appId == data.SelectedThreadId;
        var href = AfterMatchMessagesSsrData.BuildThreadHref(appId);

        sb.Append("<a class=\"ti ndm-messages-ti");
        if (active)
        {
            sb.Append(" on");
        }

        sb.Append("\" data-ndm-thread-select href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\" data-ndm-thread-id=\"");
        sb.Append(appId.ToString("D"));
        sb.Append("\" data-ndm-company=\"");
        sb.Append(WebUtility.HtmlEncode(row.CompanyName));
        sb.Append("\" data-enhance-nav=\"false\"");
        if (active)
        {
            sb.Append(" aria-current=\"true\"");
        }

        sb.Append("><span class=\"av ");
        sb.Append(AvatarClass(avatarIndex));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><div class=\"sp\"><b>");
        sb.Append(WebUtility.HtmlEncode(row.CompanyName));
        sb.Append("</b><p>");
        sb.Append(WebUtility.HtmlEncode(
            SollicitatiesBerichtenPresentation.ThreadSubtitle(culture, row, threadDto)));
        sb.Append("</p></div>");
        if (row.AfterMatch!.UnreadMessageCount > 0)
        {
            sb.Append("<span class=\"pill warm\">");
            sb.Append(row.AfterMatch.UnreadMessageCount);
            sb.Append("</span>");
        }

        sb.Append("</a>");
    }

    private static void AppendSafeNote(
        StringBuilder sb,
        CultureState culture,
        AfterMatchMessagesSsrData.PageData data,
        IReadOnlyList<SollicitationRow> threads)
    {
        var reportHref = AfterMatchMessagesSsrData.BuildReportHref(threads[0]);
        sb.Append("<div style=\"padding:16px\"><div class=\"note\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z\"/></svg><span><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.SafeTitle"]));
        sb.Append("</b> ");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.SafeBody"]));
        sb.Append(" <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(reportHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.ReportLink"]));
        sb.Append("</a>.</span></div><p class=\"xs muted\" style=\"margin-top:10px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.ArchiveHint"]));
        sb.Append(" <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ArchiveHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.ArchiveLink"]));
        sb.Append("</a>.</p></div>");
    }

    private static void AppendThreadMain(
        StringBuilder sb,
        CultureState culture,
        SollicitationRow row,
        Guid appId,
        AfterMatchThreadDto thread)
    {
        sb.Append("<div class=\"row ndm-messages-thd\"><span class=\"av ");
        sb.Append(AvatarClass(0));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><div class=\"sp\"><b style=\"color:var(--brand)\">");
        var headContact = thread.WithWhom ?? row.CompanyName;
        sb.Append(WebUtility.HtmlEncode($"{headContact} · {row.CompanyName}"));
        sb.Append("</b><div class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(FormatThreadHeadSubtitle(culture, row, thread)));
        sb.Append("</div></div><a class=\"lk small hide-m ndm-messages-view-app\" href=\"");
        sb.Append(WebUtility.HtmlEncode(
            AfterMatchSollicitatiesUrls.ViewApplicationFromMessages(appId, thread)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Berichten.ViewApplication"]));
        sb.Append("</a></div><div class=\"thread\">");

        if (SollicitatiesBerichtenPresentation.BothYesSystemLine(culture, thread) is { } bothYes)
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
            sb.Append(WebUtility.HtmlEncode(SollicitatiesBerichtenPresentation.FormatMessageMeta(culture, message)));
            sb.Append("</small></div>");
        }

        if (thread.InterviewStatus == InterviewStatus.Scheduled && thread.ScheduledStartUtc is DateTime start)
        {
            sb.Append("<div class=\"sys\">");
            sb.Append(WebUtility.HtmlEncode(string.Format(
                culture["Apps.Berichten.SysScheduled"],
                SollicitatiesBerichtenPresentation.FormatShortDate(culture, start))));
            sb.Append("</div>");
        }

        sb.Append("</div>");

        if (!thread.ContactShare.PhoneAllowed)
        {
            sb.Append("<div class=\"warnin\" data-ndm-phone-hint><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z\"/></svg><span>");
            sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.PhoneWaitWarning"]));
            sb.Append("</span></div>");
        }

        sb.Append("<form class=\"compose ndm-messages-compose\" data-ndm-compose data-application-id=\"");
        sb.Append(appId.ToString("D"));
        sb.Append("\"><span class=\"ndm-messages-compose-attach\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 11 12 19a5 5 0 0 1-7-7l8-8a3.5 3.5 0 0 1 5 5l-8 8a2 2 0 0 1-3-3l7-7\"/></svg></span><label class=\"visually-hidden\" for=\"ndm-compose-");
        sb.Append(appId.ToString("N"));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("</label><input class=\"ndm-messages-compose-input\" id=\"ndm-compose-");
        sb.Append(appId.ToString("N"));
        sb.Append("\" type=\"text\" name=\"body\" autocomplete=\"off\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("\" /><button type=\"submit\" class=\"btn sm ndm-messages-compose-send\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Compose"]));
        sb.Append("\">");
        sb.Append(NdMIcons.Send);
        sb.Append("</button></form><p class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(FormatReplyPace(culture, thread)));
        sb.Append("</p>");
    }

    private static string FormatReplyPace(CultureState culture, AfterMatchThreadDto thread)
    {
        var contact = thread.WithWhom ?? thread.CompanyName;
        var template = culture["AfterMatch.Screen10.ReplyPace"];
        return template.Replace("Linda", contact, StringComparison.Ordinal);
    }

    private static string FormatThreadHeadSubtitle(
        CultureState culture,
        SollicitationRow row,
        AfterMatchThreadDto thread)
    {
        var about = string.Format(culture["Apps.Berichten.AboutRole"], row.Title);
        if (thread.ScheduledStartUtc is DateTime start)
        {
            var when = AfterMatchMailSsrTime.ToAmsterdam(start).ToString(
                "dddd d MMMM, HH:mm",
                System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));
            return $"{about} · gesprek {when}";
        }

        return about;
    }

    private static string AvatarClass(int index) => (index % 3) switch
    {
        1 => "g",
        2 => "",
        _ => "w",
    };

    private static void AppendChatDock(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"chatdock ndm-messages-chatdock\"><button type=\"button\" class=\"askl\" data-ndm-ask-lobsy data-assistant-fallback=\"/candidate/hoe-werkt-lobsy\"><img src=\"/brand/svg/lobsy-merkteken.svg\" alt=\"\">");
        sb.Append(WebUtility.HtmlEncode(culture["Assistant.SubtitleWarm"]));
        sb.Append("</button></div>");
    }

    private static void AppendMockupFooter(StringBuilder sb, CultureState culture)
    {
        sb.Append("<footer class=\"lf ndm-mockup-footer\"><div class=\"ll\"><a href=\"/privacy\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"/terms\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Terms"]));
        sb.Append("</a><a href=\"/accessibility\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a><a href=\"/sources\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Sources"]));
        sb.Append("</a></div><span>© ");
        sb.Append(DateTime.UtcNow.Year);
        sb.Append(" Lobsy</span></footer>");
    }
}
