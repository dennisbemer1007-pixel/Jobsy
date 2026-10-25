using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerWatNuSsrShells
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string ProposeHref,
        string MessagesHref,
        string ApplicationDetailHref,
        string? StatusNote);

    internal static string BuildBody(CultureState culture, PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();
        var thread = data.Thread;
        sb.Append("<div class=\"ndm ndm-wg\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"\" data-ndm-employer-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><div class=\"g2\"><div class=\"stack\">");
        AppendCandidateCard(sb, culture, data);
        sb.Append("</div>");
        AppendShareAside(sb, culture, thread);
        sb.Append("</div><p style=\"margin-top:16px\"><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ApplicationDetailHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Back"]));
        sb.Append("</a></p></div>");
        return sb.ToString();
    }

    private static void AppendCandidateCard(StringBuilder sb, CultureState culture, PageData data)
    {
        var thread = data.Thread;
        sb.Append("<div class=\"wc stack\"><div class=\"row\"><span class=\"av w\" style=\"width:46px;height:46px\">");
        sb.Append(WebUtility.HtmlEncode(CandidateInitials(thread.CandidateName)));
        sb.Append("</span><div><h3 style=\"font-size:1.25rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.CandidateName));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(CandidateSubtitle(culture, thread)));
        sb.Append("</p></div></div><div class=\"hr\"></div>");
        AppendTimeline(sb, culture, thread);
        AppendWhatGrid(sb, culture);
        if (!string.IsNullOrWhiteSpace(data.StatusNote))
        {
            sb.Append("<div class=\"note\"><span>");
            sb.Append(NdMIcons.Info);
            sb.Append("</span><span>");
            sb.Append(WebUtility.HtmlEncode(data.StatusNote));
            sb.Append("</span></div>");
        }

        sb.Append("<div class=\"hr\"></div><div class=\"row wrapflex\"><a class=\"btn\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ProposeHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Action.Propose"]));
        sb.Append("</a><a class=\"btn line\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.MessagesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Action.Messages"]));
        sb.Append("</a><span class=\"sp\"></span><span class=\"small muted hide-m\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Expectation"]));
        sb.Append("</span></div></div>");
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
        sb.Append("<aside class=\"wc stack\"><h2 style=\"font-size:1.15rem\">");
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
