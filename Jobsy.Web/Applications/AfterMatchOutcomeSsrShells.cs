using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchOutcomeSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchOutcomeSsrData.PageData data,
        Guid applicationId)
    {
        var sb = new StringBuilder();
        AppendHeroStrip(sb);
        sb.Append("<div class=\"ndm ndm-page ndm-outcome-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"outcome\" data-ndm-outcome-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\">");

        AppendIntro(sb, culture, data);
        sb.Append("<div class=\"g2 ndm-outcome-g2\">");
        AppendMainStack(sb, culture, data);
        AppendFeedbackAside(sb, culture);
        sb.Append("</div></main>");

        AppendChatDock(sb, culture, data);
        AppendMockupFooter(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendHeroStrip(StringBuilder sb)
        => KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.NuNietDesk, KreeftHeaderStripAssets.NuNietMob);

    private static void AppendIntro(StringBuilder sb, CultureState culture, AfterMatchOutcomeSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<section class=\"intro\"><nav class=\"bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\"><a href=\"");
        sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(thread.CompanyName));
        sb.Append("</span><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.PageTitle"]));
        sb.Append("</span></nav><h1>");
        var isOffer = thread.Outcome == AfterMatchOutcomeKind.Offer;
        sb.Append(WebUtility.HtmlEncode(culture[isOffer ? "AfterMatch.Screen13.Hero.TitleOffer" : "AfterMatch.Screen13.Hero.Title"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(culture[isOffer ? "AfterMatch.Screen13.Hero.LeadOffer" : "AfterMatch.Screen13.Hero.Lead"]));
        sb.Append("</p></section>");
    }

    private static void AppendMainStack(StringBuilder sb, CultureState culture, AfterMatchOutcomeSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<div class=\"stack\"><div class=\"card\"><div class=\"co\"><span class=\"lg\">");
        sb.Append(WebUtility.HtmlEncode(CompanyInitials(thread.CompanyName)));
        sb.Append("</span><div><h3 style=\"font-size:1.2rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.VacancyTitle));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(VacancySubtitle(thread)));
        sb.Append("</p></div></div><div class=\"hr ndm-outcome-card-hr\"></div>");

        AppendTimeline(sb, culture, data);
        sb.Append("<div class=\"hr ndm-outcome-card-hr\"></div><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Screen13.FeedbackTitleFmt"], thread.CompanyName)));
        sb.Append("</b><p style=\"margin-top:6px;font-style:italic\">\"");
        sb.Append(WebUtility.HtmlEncode(data.OutcomeReason));
        sb.Append("\"</p>");

        sb.Append("<div class=\"note ndm-outcome-activities\" style=\"margin-top:14px\">");
        sb.Append(NdMIcons.Clipboard);
        sb.Append("<span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.ActivitiesPrefix"]));
        sb.Append(" <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ActivitiesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ActivitiesTab"]));
        sb.Append("</a>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.ActivitiesSuffix"]));
        sb.Append("</span></div></div>");

        AppendWhatNowCard(sb, culture, data);
        sb.Append("</div>");
    }

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchOutcomeSsrData.PageData data)
    {
        sb.Append("<nav class=\"tl ndm-outcome-tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("\">");
        AppendTimelineStep(sb, culture["AfterMatch.Timeline.Yes"], data.YesDateLabel, done: true);
        AppendTimelineStep(sb, culture["AfterMatch.Timeline.Plan"], data.PlanDateLabel, done: true);
        AppendTimelineStep(sb, culture["AfterMatch.Timeline.Interview"], data.InterviewDateLabel, done: true);
        AppendTimelineStep(sb, culture["AfterMatch.Timeline.Outcome"], data.OutcomeDateLabel, done: true);
        sb.Append("</nav>");
    }

    private static void AppendTimelineStep(
        StringBuilder sb,
        string label,
        string dateLabel,
        bool done)
    {
        sb.Append("<div class=\"s");
        if (done)
        {
            sb.Append(" done");
        }

        sb.Append("\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(dateLabel));
        sb.Append("</small></div>");
    }

    private static void AppendWhatNowCard(StringBuilder sb, CultureState culture, AfterMatchOutcomeSsrData.PageData data)
    {
        sb.Append("<div class=\"card ndm-outcome-whatnow\"><h2 style=\"font-size:1.2rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.WhatNow"]));
        sb.Append("</h2><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.WhatNowLead"]));
        sb.Append("</p><div style=\"margin-top:8px\">");

        foreach (var job in data.SimilarJobs)
        {
            sb.Append("<div class=\"row ndm-outcome-job\" style=\"padding:10px 0;border-top:1px solid var(--border)\"><span class=\"ico\">");
            sb.Append(WebUtility.HtmlEncode(job.Initials));
            sb.Append("</span><div class=\"sp\"><b style=\"color:var(--brand)\">");
            sb.Append(WebUtility.HtmlEncode(job.Title));
            sb.Append("</b><div class=\"small muted\">");
            sb.Append(WebUtility.HtmlEncode(job.Subtitle));
            sb.Append("</div>");
            if (!string.IsNullOrWhiteSpace(job.Hint))
            {
                sb.Append("<div class=\"xs\">");
                sb.Append(WebUtility.HtmlEncode(job.Hint!));
                sb.Append("</div>");
            }

            sb.Append("</div>");
            if (job.VacancyId is Guid vacancyId)
            {
                sb.Append("<a class=\"btn line sm\" href=\"");
                sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Vacancy(vacancyId)));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.ViewJob"]));
                sb.Append("</a>");
            }
            else
            {
                sb.Append("<a class=\"btn line sm\" href=\"");
                sb.Append(WebUtility.HtmlEncode(data.SimilarJobsHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.ViewJob"]));
                sb.Append("</a>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div><div class=\"row wrapflex ndm-outcome-actions\" style=\"margin-top:12px\"><a class=\"btn\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.SimilarJobsHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.MoreJobs"]));
        sb.Append("</a><a class=\"btn line\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.LearnHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.LearnCareer"]));
        sb.Append("</a></div></div>");
    }

    private static void AppendFeedbackAside(StringBuilder sb, CultureState culture)
    {
        sb.Append("<aside class=\"card ndm-outcome-aside\"><h2 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.FeedbackAsideTitle"]));
        sb.Append("</h2><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.FeedbackAsideLead"]));
        sb.Append("</p><div class=\"hr ndm-outcome-card-hr\"></div><b class=\"small\" style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.FeedbackPrompt"]));
        sb.Append("</b><p class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen13.FeedbackHint"]));
        sb.Append("</p><div class=\"row wrapflex ndm-outcome-chips\" style=\"gap:6px;margin-top:8px\" data-ndm-outcome-chips>");
        AppendChip(sb, culture["AfterMatch.Screen13.Chip.Friendly"]);
        AppendChip(sb, culture["AfterMatch.Screen13.Chip.Clear"]);
        AppendChip(sb, culture["AfterMatch.Screen13.Chip.Quick"]);
        sb.Append("</div></aside>");
    }

    private static void AppendChip(StringBuilder sb, string label)
    {
        sb.Append("<button type=\"button\" class=\"chip\" data-ndm-outcome-chip>");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</button>");
    }

    private static void AppendChatDock(StringBuilder sb, CultureState culture, AfterMatchOutcomeSsrData.PageData data)
    {
        sb.Append("<div class=\"chatdock ndm-outcome-chatdock\"><button type=\"button\" class=\"askl\" data-ndm-ask-lobsy data-assistant-fallback=\"");
        sb.Append(WebUtility.HtmlEncode(data.AssistantHref));
        sb.Append("\"><img src=\"/brand/svg/lobsy-merkteken.svg\" alt=\"\">");
        sb.Append(WebUtility.HtmlEncode(culture["Assistant.SubtitleWarm"]));
        sb.Append("</button></div>");
    }

    private static void AppendMockupFooter(StringBuilder sb, CultureState culture)
    {
        sb.Append("<footer class=\"lf ndm-mockup-footer\"><div class=\"ll\"><a href=\"/privacy\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"/algemene-voorwaarden\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Terms"]));
        sb.Append("</a><a href=\"/toegankelijkheid\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a><a href=\"/bronnen\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Sources"]));
        sb.Append("</a></div><span>© ");
        sb.Append(DateTime.UtcNow.Year);
        sb.Append(" Lobsy</span></footer>");
    }

    private static string VacancySubtitle(AfterMatchThreadDto t) =>
        string.Join(" · ", new[] { t.CompanyName, t.LocationOrLink, t.TravelLabel }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string CompanyInitials(string company)
    {
        var parts = company.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : company.Length >= 2 ? company[..2].ToUpperInvariant() : company.ToUpperInvariant();
    }
}
