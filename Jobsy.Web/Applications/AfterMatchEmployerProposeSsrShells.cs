using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerProposeSsrShells
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string CancelHref,
        string ApplicationDetailHref,
        IReadOnlyList<DateTime> InitialSelectedUtc,
        InterviewFormat InitialFormat,
        string InitialLocation,
        string InitialWithWhom,
        string InitialNote);

    internal static string BuildBody(CultureState culture, PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();
        var thread = data.Thread;
        sb.Append("<div class=\"ndm ndm-wg ndm-wg-propose\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"propose\" data-ndm-employer-propose-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><div class=\"g2\"><div class=\"stack\">");
        AppendCandidateCard(sb, culture, thread);
        sb.Append("</div>");
        AppendProposeAside(sb, culture, data);
        sb.Append("</div><p style=\"margin-top:16px\"><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ApplicationDetailHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Back"]));
        sb.Append("</a></p></div>");
        return sb.ToString();
    }

    private static void AppendCandidateCard(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        sb.Append("<div class=\"wc stack\"><div class=\"row\"><span class=\"av w\" style=\"width:46px;height:46px\">");
        sb.Append(WebUtility.HtmlEncode(CandidateInitials(thread.CandidateName)));
        sb.Append("</span><div><h3 style=\"font-size:1.25rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.CandidateName));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(CandidateSubtitle(culture, thread)));
        sb.Append("</p></div></div><div class=\"hr\"></div>");
        AppendTimeline(sb, culture, thread);
        sb.Append("<div class=\"note\"><span>");
        sb.Append(NdMIcons.Info);
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.PickHint"]));
        sb.Append("</span></div></div>");
    }

    private static void AppendProposeAside(StringBuilder sb, CultureState culture, PageData data)
    {
        sb.Append("<aside class=\"wc side pop stack\"><div class=\"shd\"><span class=\"ico navy\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Title"]));
        sb.Append("</h2><p class=\"muted small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Lead"]));
        sb.Append("</p></div></div><div class=\"row wrapflex\" style=\"gap:6px\" data-ndm-propose-mode-chips><button type=\"button\" class=\"chip on\" data-ndm-propose-mode=\"manual\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Manual"]));
        sb.Append("</button><button type=\"button\" class=\"chip\" data-ndm-propose-mode=\"agenda\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.ShareAgenda"]));
        sb.Append("</button></div><div class=\"note warm\" data-ndm-propose-agenda-note\" hidden><span>");
        sb.Append(NdMIcons.Info);
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.AgendaSoon"]));
        sb.Append("</span></div>");
        NdMWeekCalendarSsr.AppendWeekCalendar(sb, NdMWeekCalendarSsr.DefaultWeekStartUtc, data.InitialSelectedUtc);
        sb.Append("<div data-ndm-picked-slots>");
        foreach (var start in data.InitialSelectedUtc.OrderBy(d => d))
        {
            AppendPickedSlot(sb, start);
        }

        sb.Append("</div><p class=\"small muted\" data-ndm-pick-hint>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.PickHint"]));
        sb.Append("</p><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen03.Format.OnSite"]));
        sb.Append("</span><div class=\"row wrapflex\" style=\"gap:6px\" data-ndm-format-chips>");
        AppendFormatChip(sb, culture, InterviewFormat.OnSite, data.InitialFormat);
        AppendFormatChip(sb, culture, InterviewFormat.Online, data.InitialFormat);
        AppendFormatChip(sb, culture, InterviewFormat.Phone, data.InitialFormat);
        sb.Append("</div><input class=\"inp\" style=\"margin-top:8px;width:100%\" data-ndm-location placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.Where"]));
        sb.Append("\" value=\"");
        sb.Append(WebUtility.HtmlEncode(data.InitialLocation));
        sb.Append("\" /></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Propose.WithWhom"]));
        sb.Append("</span><input class=\"inp\" style=\"width:100%\" data-ndm-with-whom value=\"");
        sb.Append(WebUtility.HtmlEncode(data.InitialWithWhom));
        sb.Append("\" /></div><div><span class=\"lbl\">");
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
        sb.Append("</button><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CancelHref));
        sb.Append("\" data-enhance-nav=\"false\">");
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
        sb.Append(WebUtility.HtmlEncode(FormatLabel(culture, fmt)));
        sb.Append("</button>");
    }

    private static void AppendPickedSlot(StringBuilder sb, DateTime startUtc)
    {
        var start = startUtc.ToLocalTime();
        sb.Append("<div class=\"slot on\" data-ndm-picked-slot=\"");
        sb.Append(WebUtility.HtmlEncode(startUtc.ToString("O")));
        sb.Append("\"><span class=\"ico navy\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div class=\"sp\"><b>");
        sb.Append(WebUtility.HtmlEncode(start.ToString("dddd d MMMM", CultureInfo.GetCultureInfo("nl-NL"))));
        sb.Append("</b><span class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode($"{start:HH:mm} tot {start.AddMinutes(30):HH:mm}"));
        sb.Append("</span></div><button type=\"button\" class=\"xbtn\" data-ndm-remove-slot aria-label=\"Verwijderen\">");
        sb.Append(NdMIcons.Close);
        sb.Append("</button></div>");
    }

    private static string FormatLabel(CultureState culture, InterviewFormat fmt) => fmt switch
    {
        InterviewFormat.Online => culture["AfterMatch.Screen03.Format.Online"],
        InterviewFormat.Phone => culture["AfterMatch.Screen03.Format.Phone"],
        _ => culture["AfterMatch.Screen03.Format.OnSite"],
    };

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc?.ToLocalTime().ToString(
            "d MMM",
            CultureInfo.GetCultureInfo("nl-NL"));
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

        sb.Append("</div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Now"]));
        sb.Append("</small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small></small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small></small></div></nav>");
    }

    private static string CandidateSubtitle(CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc?.ToLocalTime().ToString(
            "d MMM",
            CultureInfo.GetCultureInfo("nl-NL"));
        var yesPart = yesDate is not null
            ? string.Format(culture["AfterMatch.Employer.YesOn"], yesDate)
            : null;
        return string.Join(
            " · ",
            new[] { thread.VacancyTitle, culture["AfterMatch.Demo.Team"], yesPart }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string CandidateInitials(string candidateName)
    {
        var parts = candidateName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : candidateName.Length >= 2 ? candidateName[..2].ToUpperInvariant() : "?";
    }
}
