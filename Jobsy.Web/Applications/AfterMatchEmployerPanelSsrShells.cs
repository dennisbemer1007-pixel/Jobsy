using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerPanelSsrShells
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string ApplicationDetailHref,
        string? StatusNote,
        IReadOnlyList<DateTime> InitialSelectedUtc,
        InterviewFormat InitialFormat,
        string InitialLocation,
        string InitialWithWhom,
        string InitialNote,
        string InitialPanel,
        AfterMatchEmployerMessagesSsrData.PageData? MessagesHub,
        AfterMatchEmployerOutcomeSsrData.PageData? OutcomeOverlay = null);

    internal static string BuildBody(
        CultureState culture,
        PageData data,
        Guid applicationId,
        bool showProposeAction = true)
    {
        var sb = new StringBuilder();
        var panel = string.IsNullOrWhiteSpace(data.InitialPanel) ? "" : data.InitialPanel.Trim().ToLowerInvariant();
        if (panel is "slots")
        {
            panel = "propose";
        }

        sb.Append("<div class=\"ndm ndm-wg ndm-wg-page\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"");
        sb.Append(WebUtility.HtmlEncode(panel));
        sb.Append("\" data-ndm-employer-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\" data-ndm-title-home=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen02.Hero.Title"]));
        sb.Append("\" data-ndm-lead-home=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen02.Hero.Lead"]));
        sb.Append("\" data-ndm-title-propose=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen03.Hero.Title"]));
        sb.Append("\" data-ndm-lead-propose=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen03.Hero.Lead"]));
        sb.Append("\" data-ndm-title-messages=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.Hero.Title"]));
        sb.Append("\" data-ndm-lead-messages=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen11.Hero.Lead"]));
        sb.Append("\" data-enhance-nav=\"false\"><div data-ndm-wg-flow");
        if (panel is "messages" or "outcome" or "cancel" or "propose")
        {
            sb.Append(" hidden");
        }

        sb.Append("><div class=\"g2 ndm-wg-g2\">");
        AppendMainCard(sb, culture, data, panel, showProposeAction, applicationId);
        AppendShareAside(sb, culture, data.Thread);
        AppendProposePanel(sb, culture, data);
        if (data.OutcomeOverlay is not null)
        {
            AfterMatchEmployerOutcomeSsrShells.AppendInlinePanel(
                sb,
                culture,
                data.OutcomeOverlay,
                applicationId,
                startVisible: panel == "outcome");
        }

        sb.Append("</div></div>");

        if (data.MessagesHub is not null)
        {
            sb.Append("<div data-ndm-messages-panel");
            if (panel is not "messages")
            {
                sb.Append(" hidden");
            }

            sb.Append("><p style=\"margin-bottom:12px\"><button type=\"button\" class=\"lk\" data-ndm-close-panel>");
            sb.Append(WebUtility.HtmlEncode(culture["WgApp.Back"]));
            sb.Append("</button></p>");
            sb.Append(AfterMatchEmployerMessagesSsrShells.BuildBody(
                culture,
                data.MessagesHub,
                applicationId,
                showSelectedThreadPanel: false));
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    internal static string BuildDoneSummaryBody(CultureState culture, AfterMatchThreadDto _, Guid applicationId)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ndm ndm-wg ndm-wg-page\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"\" data-ndm-employer-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\" data-enhance-nav=\"false\"><div class=\"wc stack\"><div class=\"note\"><span>");
        sb.Append(NdMIcons.Info);
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen12.SubmittedNote"]));
        sb.Append("</span></div></div></div>");
        return sb.ToString();
    }

    private static void AppendMainCard(
        StringBuilder sb,
        CultureState culture,
        PageData data,
        string panel,
        bool showProposeAction,
        Guid applicationId)
    {
        var thread = data.Thread;
        var scheduled = thread.InterviewStatus == InterviewStatus.Scheduled;
        var slotsSent = thread.InterviewStatus == InterviewStatus.Proposing && thread.OpenSlots.Count > 0;
        sb.Append("<div class=\"wc stack ndm-wg-main\" data-ndm-wg-main><div class=\"row\"><span class=\"av w\" style=\"width:46px;height:46px\">");
        sb.Append(WebUtility.HtmlEncode(AfterMatchEmployerEmployerPresentation.CandidateInitials(thread)));
        sb.Append("</span><div><h3 style=\"font-size:1.25rem\">");
        sb.Append(WebUtility.HtmlEncode(AfterMatchEmployerEmployerPresentation.CandidateDisplayName(thread)));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(CandidateSubtitle(culture, thread)));
        sb.Append("</p></div></div><div class=\"hr\"></div>");
        AppendTimeline(sb, culture, thread);

        if (panel is not "propose")
        {
            if (scheduled)
            {
                AppendScheduledHomeGrid(sb, culture, thread);
            }
            else if (thread.InterviewStatus != InterviewStatus.Done)
            {
                if (slotsSent)
                {
                    AppendSlotsSentGrid(sb, culture, thread);
                }
                else
                {
                    AppendWhatGrid(sb, culture);
                }
            }
        }

        if (panel is "propose")
        {
            sb.Append("<div class=\"note\" data-ndm-propose-info-note><span>");
            sb.Append(NdMIcons.Info);
            sb.Append("</span><span>");
            sb.Append(WebUtility.HtmlEncode(FormatCandidateWaitNote(culture, thread)));
            sb.Append("</span></div>");
        }
        else if (!string.IsNullOrWhiteSpace(data.StatusNote))
        {
            sb.Append("<div class=\"note\"><span>");
            sb.Append(NdMIcons.Info);
            sb.Append("</span><span>");
            sb.Append(WebUtility.HtmlEncode(data.StatusNote));
            sb.Append("</span></div>");
        }

        if (panel is not "propose")
        {
            sb.Append("<div class=\"hr\"></div><div class=\"row wrapflex\">");
            if (showProposeAction)
            {
                sb.Append("<button type=\"button\" class=\"btn\" data-ndm-open-panel=\"propose\">");
                sb.Append(NdMIcons.Calendar);
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Action.Propose"]));
                sb.Append("</button>");
            }
            else if (scheduled)
            {
                sb.Append("<a class=\"btn line\" href=\"");
                sb.Append($"/werkgever/sollicitaties/{applicationId:D}/after-match/reschedule-ssr");
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Reschedule"]));
                sb.Append("</a><a class=\"btn line\" href=\"");
                sb.Append($"/werkgever/sollicitaties/{applicationId:D}/after-match?panel=cancel");
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Cancel"]));
                sb.Append("</a><button type=\"button\" class=\"btn\" data-ndm-open-panel=\"outcome\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Scheduled.OutcomeAction"]));
                sb.Append("</button>");
            }

            sb.Append("<button type=\"button\" class=\"btn line\" data-ndm-open-panel=\"messages\">");
            sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Action.Messages"]));
            sb.Append("</button><span class=\"sp\"></span><span class=\"small muted hide-m\">");
            if (showProposeAction)
            {
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Expectation"]));
            }

            sb.Append("</span></div>");
        }

        sb.Append("</div>");
    }

    private static void AppendScheduledHomeGrid(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        var when = thread.ScheduledStartUtc is DateTime start
            ? AfterMatchMailSsrTime.ToAmsterdam(start).ToString(
                "dddd d MMMM, HH:mm",
                CultureInfo.GetCultureInfo("nl-NL"))
            : culture["AfterMatch.Employer.Scheduled.WhenFallback"];
        sb.Append("<div class=\"what\"><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Scheduled.Tile.When"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(when));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.Reminders"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Scheduled.Tile.Reminders"]));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Scheduled.Tile.Outcome"]));
        sb.Append("</p></div></div>");
    }

    private static string FormatCandidateWaitNote(CultureState culture, AfterMatchThreadDto thread)
    {
        var first = AfterMatchEmployerEmployerPresentation.CandidateFirstName(thread);
        return string.Format(culture["AfterMatch.Employer.Propose.CandidateNoteFmt"], first, first);
    }

    private static void AppendSlotsSentGrid(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        var first = AfterMatchEmployerEmployerPresentation.CandidateFirstName(thread);
        sb.Append("<div class=\"what\"><div><b>");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.SlotsSent.TitleFmt"], first)));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.SlotsSent.BodyFmt"], first, first)));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.What.Interview.Title"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.WhatNow.Interview.Body"]));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.What.Outcome.Title"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.WhatNow.Outcome.Body"]));
        sb.Append("</p></div></div>");
    }

    private static void AppendWhatGrid(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"what\"><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.WhatNow.Slots.Title"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.WhatNow.Slots.Body"]));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.What.Interview.Title"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.WhatNow.Interview.Body"]));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.What.Outcome.Title"]));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.WhatNow.Outcome.Body"]));
        sb.Append("</p></div></div>");
    }

    private static void AppendShareAside(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        sb.Append("<aside class=\"wc stack\" data-ndm-share-aside><h2 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Share.Title"]));
        sb.Append("</h2><div class=\"list\" style=\"margin-top:10px\"><div><span class=\"ico navy\">");
        sb.Append(NdMIcons.User);
        sb.Append("</span><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Share.Passport"]));
        sb.Append("</p></div>");
        if (!string.IsNullOrWhiteSpace(thread.CandidateEmail))
        {
            sb.Append("<div><span class=\"ico navy\">");
            sb.Append(NdMIcons.Mail);
            sb.Append("</span><p class=\"small\">");
            sb.Append(WebUtility.HtmlEncode(thread.CandidateEmail));
            sb.Append("</p></div>");
        }

        sb.Append("<div><span class=\"ico navy\">");
        sb.Append(NdMIcons.Phone);
        sb.Append("</span><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Share.Phone"]));
        sb.Append("</p></div><div><span class=\"ico navy\">");
        sb.Append(NdMIcons.Bank);
        sb.Append("</span><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Share.Bank"]));
        sb.Append("</p></div></div></aside>");
    }

    internal static void AppendProposePanel(StringBuilder sb, CultureState culture, PageData data)
    {
        var panel = string.IsNullOrWhiteSpace(data.InitialPanel) ? "" : data.InitialPanel.Trim().ToLowerInvariant();
        if (panel is "slots")
        {
            panel = "propose";
        }

        sb.Append("<aside class=\"wc side pop stack\" data-ndm-propose-panel");
        if (panel is not "propose")
        {
            sb.Append(" hidden");
        }

        sb.Append("><div class=\"shd\"><span class=\"ico navy\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Title"]));
        sb.Append("</h2><p class=\"muted small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Lead"]));
        sb.Append("</p></div><button type=\"button\" class=\"xbtn\" data-ndm-close-panel aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Close"]));
        sb.Append("\">");
        sb.Append(NdMIcons.Close);
        sb.Append("</button></div><div class=\"row wrapflex\" style=\"gap:6px\" data-ndm-propose-mode-chips><button type=\"button\" class=\"chip on\" data-ndm-propose-mode=\"manual\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Manual"]));
        sb.Append("</button><button type=\"button\" class=\"chip\" data-ndm-propose-mode=\"agenda\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.ShareAgenda"]));
        sb.Append("</button></div><div data-ndm-manual-block><div data-ndm-picked-slots>");
        foreach (var start in data.InitialSelectedUtc.OrderBy(d => d))
        {
            AppendPickedSlot(sb, start);
        }

        sb.Append("</div><button type=\"button\" class=\"lk small\" data-ndm-add-slot tabindex=\"-1\" style=\"margin-top:4px\">");
        sb.Append(NdMIcons.Plus);
        sb.Append(' ');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.AddSlot"]));
        sb.Append("</button><div class=\"stack\" data-ndm-add-slot-form hidden style=\"margin-top:8px\"><div class=\"row wrapflex\" style=\"gap:8px;align-items:flex-end\"><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.AddDate"]));
        sb.Append("</span><input type=\"date\" class=\"inp\" data-ndm-add-date style=\"width:auto\" /></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.AddTime"]));
        sb.Append("</span><input type=\"time\" class=\"inp\" data-ndm-add-time style=\"width:auto\" step=\"900\" /></div><button type=\"button\" class=\"btn sm\" data-ndm-confirm-add-slot>");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Add"]));
        sb.Append("</button></div></div></div><div data-ndm-agenda-block hidden><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.AgendaPlaceholder"]));
        sb.Append("</p></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.Where"]));
        sb.Append("</span><div class=\"row wrapflex\" style=\"gap:6px\" data-ndm-format-chips>");
        AppendFormatChip(sb, culture, InterviewFormat.OnSite, data.InitialFormat);
        AppendFormatChip(sb, culture, InterviewFormat.Online, data.InitialFormat);
        AppendFormatChip(sb, culture, InterviewFormat.Phone, data.InitialFormat);
        sb.Append("</div><div class=\"inp ndm-wg-inp-field\" style=\"margin-top:8px;width:100%\"><span class=\"ndm-wg-inp-ico\" aria-hidden=\"true\">");
        sb.Append(NdMIcons.MapPin);
        sb.Append("</span><input class=\"ndm-wg-inp-control\" data-ndm-location placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Where"]));
        sb.Append("\" value=\"");
        sb.Append(WebUtility.HtmlEncode(data.InitialLocation));
        sb.Append("\" /></div></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.WithWhom"]));
        sb.Append("</span><div class=\"inp ndm-wg-inp-field\" style=\"width:100%\"><span class=\"ndm-wg-inp-ico\" aria-hidden=\"true\">");
        sb.Append(NdMIcons.User);
        sb.Append("</span><input class=\"ndm-wg-inp-control\" data-ndm-with-whom value=\"");
        sb.Append(WebUtility.HtmlEncode(data.InitialWithWhom));
        sb.Append("\" /></div></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Note"]));
        sb.Append("</span><textarea class=\"inp area\" style=\"width:100%\" data-ndm-note>");
        sb.Append(WebUtility.HtmlEncode(data.InitialNote));
        sb.Append("</textarea></div><div class=\"row wrapflex\"><button type=\"button\" class=\"btn\" data-ndm-submit-propose");
        if (data.InitialSelectedUtc.Count is < 2 or > 3)
        {
            sb.Append(" disabled");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen03.Submit"]));
        sb.Append("</button><a class=\"lk\" href=\"#\" data-ndm-close-panel>");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Cancel"]));
        sb.Append("</a></div></aside>");
    }

    private static void AppendFormatChip(
        StringBuilder sb,
        CultureState culture,
        InterviewFormat fmt,
        InterviewFormat selected)
    {
        var on = fmt == selected ? " on" : "";
        sb.Append("<button type=\"button\" class=\"chip");
        sb.Append(on);
        sb.Append("\" data-ndm-format=\"");
        sb.Append((int)fmt);
        sb.Append("\">");
        sb.Append(FormatIcon(fmt));
        sb.Append(WebUtility.HtmlEncode(FormatLabel(culture, fmt)));
        sb.Append("</button>");
    }

    internal static void AppendPickedSlot(StringBuilder sb, DateTime startUtc)
    {
        var start = AfterMatchMailSsrTime.ToAmsterdam(startUtc);
        var end = start.AddMinutes(30);
        var nl = CultureInfo.GetCultureInfo("nl-NL");
        var dayLabel = CapitalizeFirst(start.ToString("dddd d MMMM", nl));
        sb.Append("<div class=\"slot on\" data-ndm-picked-slot=\"");
        sb.Append(WebUtility.HtmlEncode(startUtc.ToString("O")));
        sb.Append("\"><span class=\"ico navy\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div class=\"sp\"><b>");
        sb.Append(WebUtility.HtmlEncode(dayLabel));
        sb.Append("</b><span class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode($"{start:HH:mm} tot {end:HH:mm}"));
        sb.Append("</span></div><button type=\"button\" class=\"xbtn\" data-ndm-remove-slot aria-label=\"Verwijderen\">");
        sb.Append(NdMIcons.Close);
        sb.Append("</button></div>");
    }

    private static string CapitalizeFirst(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0], CultureInfo.GetCultureInfo("nl-NL")) + value[1..];

    private static string FormatLabel(CultureState culture, InterviewFormat fmt) => fmt switch
    {
        InterviewFormat.Online => culture["AfterMatch.Screen03.Format.Online"],
        InterviewFormat.Phone => culture["AfterMatch.Screen03.Format.Phone"],
        _ => culture["AfterMatch.Screen03.Format.OnSite"],
    };

    private static string FormatIcon(InterviewFormat fmt) => fmt switch
    {
        InterviewFormat.Online => NdMIcons.Video,
        InterviewFormat.Phone => NdMIcons.Phone,
        _ => NdMIcons.MapPin,
    };

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        var nl = CultureInfo.GetCultureInfo("nl-NL");
        var yesDate = thread.AcceptedAtUtc is DateTime accepted
            ? AfterMatchMailSsrTime.ToAmsterdam(accepted).ToString("d MMM", nl)
            : null;
        var interviewDate = thread.ScheduledStartUtc is DateTime scheduledStart
            ? AfterMatchMailSsrTime.ToAmsterdam(scheduledStart).ToString("d MMM", nl)
            : null;
        var isScheduled = thread.InterviewStatus == InterviewStatus.Scheduled;
        var isProposing = thread.InterviewStatus == InterviewStatus.Proposing;
        var outcomeDone = thread.Outcome is not null;

        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        if (!string.IsNullOrWhiteSpace(yesDate))
        {
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(yesDate));
            sb.Append("</small>");
        }

        var slotsSent = isProposing && thread.OpenSlots.Count > 0;
        sb.Append("</div><div class=\"s");
        if (isScheduled || slotsSent)
        {
            sb.Append(" done");
        }
        else if (isProposing)
        {
            sb.Append(" now");
        }

        sb.Append("\"><i>");
        if (isScheduled || slotsSent)
        {
            sb.Append(NdMIcons.Check);
        }
        else
        {
            sb.Append(NdMIcons.Calendar);
        }

        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(
            isScheduled
                ? (interviewDate ?? yesDate ?? "")
                : slotsSent
                    ? culture["AfterMatch.Timeline.PlanWaiting"]
                    : isProposing
                        ? culture["AfterMatch.Timeline.PlanNow"]
                        : culture["AfterMatch.Timeline.PlanNow"]));
        sb.Append("</small></div><div class=\"s");
        if (isScheduled)
        {
            sb.Append(" now");
        }
        else if (slotsSent)
        {
            sb.Append(" now");
        }
        else if (outcomeDone && !isProposing)
        {
            sb.Append(" done");
        }

        sb.Append("\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(
            isScheduled || outcomeDone
                ? (interviewDate ?? "")
                : slotsSent
                    ? culture["AfterMatch.Timeline.PlanWaiting"]
                    : ""));
        sb.Append("</small></div><div class=\"s");
        if (outcomeDone)
        {
            sb.Append(" done");
        }

        sb.Append("\"><i>");
        if (outcomeDone)
        {
            sb.Append(NdMIcons.Check);
        }
        else
        {
            sb.Append(NdMIcons.Flag);
        }

        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small>");
        sb.Append("</small></div></nav>");
    }

    private static string CandidateSubtitle(CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc is DateTime accepted
            ? AfterMatchMailSsrTime.ToAmsterdam(accepted).ToString("d MMM", CultureInfo.GetCultureInfo("nl-NL"))
            : null;
        var yesPart = yesDate is not null
            ? string.Format(culture["AfterMatch.Employer.YesOn"], yesDate)
            : null;
        return string.Join(
            " · ",
            new[] { thread.VacancyTitle, culture["AfterMatch.Demo.Team"], yesPart }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

}
