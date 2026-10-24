using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchRescheduleSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchRescheduleSsrData.PageData data,
        Guid applicationId,
        string initialMode)
    {
        var mode = string.Equals(initialMode, "cancel", StringComparison.OrdinalIgnoreCase) ? "cancel" : "reschedule";
        var sb = new StringBuilder();
        sb.Append("<div class=\"sollicitaties-hero-strip\" aria-hidden=\"true\"><div class=\"sollicitaties-hero-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp));
        sb.Append("\" alt=\"\" width=\"1440\" height=\"150\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp));
        sb.Append("\" alt=\"\" width=\"390\" height=\"90\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("</div></div>");

        sb.Append("<div class=\"ndm ndm-page ndm-reschedule-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"reschedule\" data-ndm-reschedule-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\" data-initial-mode=\"");
        sb.Append(WebUtility.HtmlEncode(mode));
        sb.Append("\" data-back-href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ConfirmedHref));
        sb.Append("\" data-applications-href=\"");
        sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
        sb.Append("\" data-propose-href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ProposeSlotHref));
        sb.Append("\"><main class=\"wrap\">");

        AppendIntro(sb, culture, data);
        sb.Append("<div class=\"g2 ndm-reschedule-g2\">");
        AppendMainCard(sb, culture, data);
        AppendSidePanel(sb, culture, data, mode);
        sb.Append("</div></main>");

        AppendChatDock(sb, culture);
        AppendMockupFooter(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendIntro(StringBuilder sb, CultureState culture, AfterMatchRescheduleSsrData.PageData data)
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
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Hero.Title"]));
        sb.Append("</span></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Hero.Title"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(data.HeroLead));
        sb.Append("</p></section>");
    }

    private static void AppendMainCard(
        StringBuilder sb,
        CultureState culture,
        AfterMatchRescheduleSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<div class=\"card\"><div class=\"co\"><span class=\"lg\">");
        sb.Append(WebUtility.HtmlEncode(CompanyInitials(thread.CompanyName)));
        sb.Append("</span><div><h3 style=\"font-size:1.2rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.VacancyTitle));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(VacancySubtitle(thread)));
        sb.Append("</p></div></div><div class=\"hr ndm-reschedule-card-hr\"></div>");

        AppendTimeline(sb, culture, data);
        sb.Append("<div class=\"hr ndm-reschedule-card-hr\"></div><div class=\"row\"><span class=\"ico green\">");
        sb.Append(CalendarIcon);
        sb.Append("</span><div><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(data.ScheduledTitle));
        sb.Append("</b><div class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(data.ScheduledSub));
        sb.Append("</div></div></div></div>");
    }

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchRescheduleSsrData.PageData data)
    {
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.YesDateLabel));
        sb.Append("</small></div><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.PlanDateLabel));
        sb.Append("</small></div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.InterviewDateLabel));
        sb.Append("</small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small></small></div></nav>");
    }

    private static void AppendSidePanel(
        StringBuilder sb,
        CultureState culture,
        AfterMatchRescheduleSsrData.PageData data,
        string mode)
    {
        var contact = data.ContactShort;
        sb.Append("<aside class=\"card side ndm-reschedule-side\"><div class=\"shd\"><span class=\"ico\">");
        sb.Append(CalendarIcon);
        sb.Append("</span><div><h2 data-ndm-side-title-reschedule");
        if (mode != "reschedule")
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.SideTitle.Reschedule"]));
        sb.Append("</h2><h2 data-ndm-side-title-cancel");
        if (mode != "cancel")
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.SideTitle.Cancel"]));
        sb.Append("</h2><p class=\"muted small\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Screen08.SideLead"], contact)));
        sb.Append("</p></div><button type=\"button\" class=\"xbtn\" data-ndm-reschedule-close aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Close"]));
        sb.Append("\">");
        sb.Append(CloseIcon);
        sb.Append("</button></div><div class=\"stack\"><div class=\"row wrapflex ndm-reschedule-mode-chips\" style=\"gap:6px\"><button type=\"button\" class=\"chip");
        if (mode == "reschedule")
        {
            sb.Append(" on");
        }

        sb.Append("\" data-ndm-mode-chip=\"reschedule\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Reschedule"]));
        sb.Append("</button><button type=\"button\" class=\"chip");
        if (mode == "cancel")
        {
            sb.Append(" on");
        }

        sb.Append("\" data-ndm-mode-chip=\"cancel\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Cancel"]));
        sb.Append("</button></div>");

        AppendReasonBlock(sb, culture);
        AppendWhenBlock(sb, culture, data, mode);
        AppendActions(sb, culture, data, mode);
        sb.Append("</div></aside>");
    }

    private static void AppendReasonBlock(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"ndm-reschedule-reason-block\"><span class=\"lbl ndm-reschedule-reason-lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.ReasonLong"]));
        sb.Append("</span><div class=\"row wrapflex ndm-reschedule-reason-chips\">");
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Sick"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Family"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Transport"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.OtherAppt"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Other"]);
        sb.Append("</div><textarea class=\"inp area ndm-reschedule-reason\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.ReasonPlaceholder"]));
        sb.Append("\" data-ndm-reason rows=\"3\"></textarea></div>");
    }

    private static void AppendReasonChip(StringBuilder sb, string label)
    {
        sb.Append("<button type=\"button\" class=\"chip\" data-ndm-reason-chip=\"");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</button>");
    }

    private static void AppendWhenBlock(
        StringBuilder sb,
        CultureState culture,
        AfterMatchRescheduleSsrData.PageData data,
        string mode)
    {
        sb.Append("<div data-ndm-when-block");
        if (mode == "cancel")
        {
            sb.Append(" hidden");
        }

        sb.Append("><span class=\"lbl ndm-reschedule-when-lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.WhenLabel"]));
        sb.Append("</span><div class=\"inp ndm-reschedule-when-display\"><span>");
        sb.Append(CalendarIcon);
        sb.Append("</span><span data-ndm-when-text>");
        if (!string.IsNullOrWhiteSpace(data.AlternateSlotLabel))
        {
            sb.Append(WebUtility.HtmlEncode(data.AlternateSlotLabel));
        }

        sb.Append("</span></div><a class=\"lk small ndm-reschedule-propose\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ProposeSlotHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.ProposeOther"]));
        sb.Append("</a></div>");
    }

    private static void AppendActions(
        StringBuilder sb,
        CultureState culture,
        AfterMatchRescheduleSsrData.PageData data,
        string mode)
    {
        sb.Append("<div class=\"row ndm-reschedule-actions\"><button type=\"button\" class=\"btn\" data-ndm-submit-reschedule");
        if (mode != "reschedule")
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Screen08.SendTo"], data.ContactShort)));
        sb.Append("</button><button type=\"button\" class=\"btn\" data-ndm-submit-cancel");
        if (mode != "cancel")
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.CancelConfirm"]));
        sb.Append("</button><button type=\"button\" class=\"lk ndm-reschedule-nevermind\" data-ndm-reschedule-nevermind>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.NeverMind"]));
        sb.Append("</button></div><p class=\"xs muted ndm-reschedule-cancel-footnote\" data-ndm-cancel-footnote");
        if (mode != "cancel")
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.CancelFootnote"]));
        sb.Append("</p>");
    }

    private static void AppendChatDock(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"chatdock ndm-reschedule-chatdock\"><button type=\"button\" class=\"askl\" data-ndm-ask-lobsy data-assistant-fallback=\"/candidate/hoe-werkt-lobsy\"><img src=\"/brand/svg/lobsy-merkteken.svg\" alt=\"\">");
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

    private const string CalendarIcon =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="4" y="5" width="16" height="15" rx="2"/><path d="M4 10h16M9 3v4M15 3v4"/></svg>""";

    private const string CloseIcon =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18"/></svg>""";

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
