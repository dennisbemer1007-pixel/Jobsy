using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerOutcomeSsrShells
{
    internal static string BuildBody(CultureState culture, AfterMatchEmployerOutcomeSsrData.PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();
        var thread = data.Thread;
        sb.Append("<div class=\"ndm ndm-wg\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"outcome\" data-ndm-employer-ssr=\"1\" data-ndm-employer-outcome-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><div class=\"g2\"><div class=\"wc stack\">");
        AppendCandidateHeader(sb, culture, thread);
        sb.Append("<div class=\"hr\"></div>");
        AppendTimeline(sb, culture, data);
        sb.Append("<div class=\"hr\"></div>");
        AppendOutcomeForm(sb, culture, data);
        sb.Append("</div>");
        AppendPreviewAside(sb, culture, data);
        sb.Append("</div><p style=\"margin-top:16px\"><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ApplicationDetailHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Back"]));
        sb.Append("</a></p></div>");
        return sb.ToString();
    }

    private static void AppendCandidateHeader(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        sb.Append("<div class=\"row\"><span class=\"av w\" style=\"width:46px;height:46px\">");
        sb.Append(NdMIcons.User);
        sb.Append("</span><div><h3 style=\"font-size:1.25rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.CandidateName));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(CandidateSubtitle(culture, thread)));
        sb.Append("</p></div></div>");
    }

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchEmployerOutcomeSsrData.PageData data)
    {
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        if (!string.IsNullOrWhiteSpace(data.YesStepHint))
        {
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(data.YesStepHint));
            sb.Append("</small>");
        }

        sb.Append("</div><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.PlanStepHint ?? ""));
        sb.Append("</small></div><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.InterviewStepHint ?? ""));
        sb.Append("</small></div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Now"]));
        sb.Append("</small></div></nav>");
    }

    private static void AppendOutcomeForm(StringBuilder sb, CultureState culture, AfterMatchEmployerOutcomeSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen12.Question"]));
        sb.Append("</span><div class=\"outc\">");
        AppendOutcomeOption(sb, culture, AfterMatchOutcomeKind.Offer, thread.CandidateName, false);
        AppendOutcomeOption(sb, culture, AfterMatchOutcomeKind.NextRound, thread.CandidateName, false);
        AppendOutcomeOption(sb, culture, AfterMatchOutcomeKind.NotNow, thread.CandidateName, true);
        sb.Append("</div><span class=\"lbl\" style=\"margin-top:16px\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Screen12.ReasonRequiredFmt"], thread.CandidateName)));
        sb.Append("</span><div class=\"row wrapflex\" style=\"gap:6px\">");
        var first = true;
        foreach (var chip in data.ReasonChips)
        {
            sb.Append("<button type=\"button\" class=\"chip");
            if (first)
            {
                sb.Append(" on");
                first = false;
            }

            sb.Append("\" data-ndm-outcome-chip>");
            sb.Append(WebUtility.HtmlEncode(chip));
            sb.Append("</button>");
        }

        sb.Append("</div><textarea class=\"inp area\" style=\"margin-top:8px;width:100%;min-height:84px;resize:vertical;font-family:inherit\" data-ndm-outcome-reason rows=\"4\">");
        sb.Append(WebUtility.HtmlEncode(data.DefaultReason));
        sb.Append("</textarea><div class=\"row wrapflex\" style=\"margin-top:14px\"><button type=\"button\" class=\"btn\" data-ndm-outcome-submit>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen12.Submit"]));
        sb.Append("</button><a class=\"btn line\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.TalentPoolHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen12.TalentPool"]));
        sb.Append("</a></div>");
    }

    private static void AppendOutcomeOption(
        StringBuilder sb,
        CultureState culture,
        AfterMatchOutcomeKind kind,
        string candidateName,
        bool selected)
    {
        var (title, hint) = kind switch
        {
            AfterMatchOutcomeKind.Offer => (culture["AfterMatch.Screen12.Offer"], string.Format(culture["AfterMatch.Screen12.Offer.HintFmt"], candidateName)),
            AfterMatchOutcomeKind.NextRound => (culture["AfterMatch.Screen12.NextRound"], culture["AfterMatch.Screen12.NextRound.Hint"]),
            _ => (culture["AfterMatch.Screen12.NotNow"], culture["AfterMatch.Screen12.NotNow.Hint"])
        };

        sb.Append("<button type=\"button\" class=\"");
        if (selected)
        {
            sb.Append("on ");
        }

        sb.Append("\" data-ndm-employer-outcome data-outcome-kind=\"");
        sb.Append(kind);
        sb.Append("\"><b>");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</b><span class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(hint));
        sb.Append("</span></button>");
    }

    private static void AppendPreviewAside(StringBuilder sb, CultureState culture, AfterMatchEmployerOutcomeSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<aside class=\"wc\"><h2 style=\"font-size:1.1rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Outcome.PreviewTitle"]));
        sb.Append("</h2><div class=\"card\" style=\"margin-top:10px;background:#fbf6ef\" data-ndm-outcome-preview><div data-ndm-preview-pane=\"Offer\" hidden><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Outcome.PreviewOfferTitleFmt"], thread.CandidateName)));
        sb.Append("</b><p class=\"small\" style=\"margin-top:6px\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Outcome.PreviewOfferLeadFmt"], thread.CompanyName)));
        sb.Append("</p><p class=\"small\" style=\"margin-top:6px;font-style:italic\" data-ndm-preview-quote-offer\">\"");
        sb.Append(WebUtility.HtmlEncode(data.DefaultReason));
        sb.Append("\"</p></div><div data-ndm-preview-pane=\"NextRound\" hidden><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Outcome.PreviewNextTitleFmt"], thread.CandidateName)));
        sb.Append("</b><p class=\"small\" style=\"margin-top:6px\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Outcome.PreviewNextLeadFmt"], thread.CompanyName)));
        sb.Append("</p><p class=\"small\" style=\"margin-top:6px;font-style:italic\" data-ndm-preview-quote-nextround\">\"");
        sb.Append(WebUtility.HtmlEncode(data.DefaultReason));
        sb.Append("\"</p></div><div data-ndm-preview-pane=\"NotNow\"><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Outcome.PreviewNotNowTitleFmt"], thread.CandidateName)));
        sb.Append("</b><p class=\"small\" style=\"margin-top:6px\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Outcome.PreviewNotNowLeadFmt"], thread.CompanyName)));
        sb.Append("</p><p class=\"small\" style=\"margin-top:6px;font-style:italic\" data-ndm-preview-quote-notnow\">\"");
        sb.Append(WebUtility.HtmlEncode(data.DefaultReason));
        sb.Append("\"</p></div></div><p class=\"xs muted\" style=\"margin-top:10px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Outcome.ToneNote"]));
        sb.Append("</p><div class=\"note\" style=\"margin-top:12px\"><span>");
        sb.Append(NdMIcons.Info);
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Outcome.OfferNote"]));
        sb.Append("</span></div></aside>");
    }

    private static string CandidateSubtitle(CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc?.ToLocalTime().ToString("d MMMM", CultureInfo.GetCultureInfo("nl-NL"));
        var yesPart = yesDate is not null ? string.Format(culture["AfterMatch.Employer.YesOn"], yesDate) : null;
        return string.Join(" · ", new[] { thread.VacancyTitle, culture["AfterMatch.Demo.Team"], yesPart }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
