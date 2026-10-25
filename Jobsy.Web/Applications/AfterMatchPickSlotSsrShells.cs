using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchPickSlotSsrShells
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    internal static string BuildBody(CultureState culture, AfterMatchPickSlotSsrData.PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"sollicitaties-hero-strip\" aria-hidden=\"true\"><div class=\"sollicitaties-hero-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp));
        sb.Append("\" alt=\"\" width=\"1440\" height=\"150\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp));
        sb.Append("\" alt=\"\" width=\"390\" height=\"90\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("</div></div>");

        sb.Append("<div class=\"ndm ndm-page ndm-pick-slot-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"pick-slot\" data-ndm-pick-slot-ssr=\"1\" data-enhance-nav=\"false\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\">");

        AppendIntro(sb, culture, data);
        sb.Append("<div class=\"g2 ndm-pick-slot-g2\">");
        AppendMainCard(sb, culture, data, applicationId);
        AppendAside(sb, culture);
        sb.Append("</div></main>");

        AppendChatDock(sb, culture);
        AppendMockupFooter(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendIntro(StringBuilder sb, CultureState culture, AfterMatchPickSlotSsrData.PageData data)
    {
        var thread = data.Thread;
        var lead = FormatLead(culture, thread);
        sb.Append("<section class=\"intro\"><nav class=\"bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\"><a href=\"");
        sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(thread.CompanyName));
        sb.Append("</span><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen04.PageTitle"]));
        sb.Append("</span></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen04.Hero.Title"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(lead));
        sb.Append("</p></section>");
    }

    private static string FormatLead(CultureState culture, AfterMatchThreadDto thread)
    {
        var contact = string.IsNullOrWhiteSpace(thread.WithWhom) ? thread.CompanyName : thread.WithWhom!;
        return string.Format(
            culture["AfterMatch.Screen04.Hero.LeadPick"],
            contact,
            thread.CompanyName,
            thread.OpenSlots.Count);
    }

    private static void AppendMainCard(
        StringBuilder sb,
        CultureState culture,
        AfterMatchPickSlotSsrData.PageData data,
        Guid applicationId)
    {
        var thread = data.Thread;
        var yesDate = thread.AcceptedAtUtc is DateTime acceptedAt
            ? AfterMatchMailSsrTime.ToAmsterdam(acceptedAt).ToString("d MMM", Nl)
            : "13 okt";

        sb.Append("<div class=\"card\"><div class=\"co\"><span class=\"lg\">");
        sb.Append(WebUtility.HtmlEncode(CompanyInitials(thread.CompanyName)));
        sb.Append("</span><div><h3 style=\"font-size:1.2rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.VacancyTitle));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(VacancySubtitle(thread)));
        sb.Append("</p></div></div><div class=\"hr\"></div>");

        AppendTimeline(sb, culture, yesDate);
        sb.Append("<div class=\"hr\"></div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen04.SlotsLabel"]));
        sb.Append("</span><div class=\"cal3\" data-ndm-slot-list>");

        for (var i = 0; i < data.SlotsOrdered.Count; i++)
        {
            var slot = data.SlotsOrdered[i];
            var selected = i == 0;
            sb.Append("<button type=\"button\" class=\"slot");
            if (selected)
            {
                sb.Append(" on");
            }

            sb.Append("\" data-slot-id=\"");
            sb.Append(slot.Id.ToString("D"));
            sb.Append("\" aria-pressed=\"");
            sb.Append(selected ? "true" : "false");
            sb.Append("\"><span class=\"radio");
            if (selected)
            {
                sb.Append(" on");
            }

            sb.Append("\"></span><div class=\"sp\"><span class=\"slot-title\">");
            sb.Append(WebUtility.HtmlEncode(FormatSlotTitle(slot)));
            sb.Append("</span><span class=\"small muted\">");
            sb.Append(WebUtility.HtmlEncode(FormatSlotSubtitle(slot)));
            sb.Append("</span></div></button>");
        }

        sb.Append("</div>");
        AppendFacts(sb, culture, thread);

        if (!string.IsNullOrWhiteSpace(thread.Note))
        {
            sb.Append("<div class=\"note warm\" style=\"margin-top:14px\">");
            sb.Append(ChatBubbleIcon);
            sb.Append("<span>\"");
            sb.Append(WebUtility.HtmlEncode(thread.Note));
            sb.Append("\"</span></div>");
        }

        sb.Append("<div class=\"row wrapflex ndm-pick-actions\" style=\"margin-top:16px\"><button type=\"button\" class=\"btn\" data-ndm-confirm-slot");
        if (data.SlotsOrdered.Count == 0)
        {
            sb.Append(" disabled");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen04.ChooseSlot"]));
        sb.Append("</button><button type=\"button\" class=\"lk\" data-ndm-scroll-propose style=\"background:none;border:0;padding:0;font:inherit;cursor:pointer\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen04.NoSlotFits"]));
        sb.Append("</button></div>");
        sb.Append("<p class=\"state-message\" role=\"alert\" data-ndm-pick-slot-error hidden style=\"margin-top:12px\">");
        sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
        sb.Append("</p>");

        AppendProposeForm(sb, culture, applicationId);
        sb.Append("</div>");
    }

    private static void AppendTimeline(StringBuilder sb, CultureState culture, string yesDate)
    {
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(yesDate));
        sb.Append("</small></div><div class=\"s now\"><i>");
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

    private static void AppendFacts(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        var formatLabel = thread.Format switch
        {
            InterviewFormat.Online => culture["AfterMatch.Screen03.Format.Online"],
            InterviewFormat.Phone => culture["AfterMatch.Screen03.Format.Phone"],
            _ => culture["AfterMatch.Pick.OnSiteTitle"]
        };

        var locationLine = string.IsNullOrWhiteSpace(thread.LocationOrLink)
            ? thread.CompanyName
            : thread.LocationOrLink!;
        var withWhom = string.IsNullOrWhiteSpace(thread.WithWhom) ? thread.CompanyName : thread.WithWhom!;
        var withHint = string.Format(culture["AfterMatch.Pick.WithWhomHint"], thread.CompanyName);
        var travelTitle = string.IsNullOrWhiteSpace(thread.TravelLabel)
            ? culture["AfterMatch.Pick.TravelFallbackTitle"]
            : thread.TravelLabel!;
        var travelHint = culture["AfterMatch.Pick.TravelHint"];

        const string mapPin =
            """<svg style="width:15px;height:15px;vertical-align:-2px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 21s-6-5.5-6-11a6 6 0 0 1 12 0c0 5.5-6 11-6 11z"/><circle cx="12" cy="10" r="2"/></svg> """;

        sb.Append("<div class=\"what\" style=\"grid-template-columns:repeat(3,minmax(0,1fr))\"><div><b>");
        if (thread.Format == InterviewFormat.OnSite)
        {
            sb.Append(mapPin);
        }

        sb.Append(WebUtility.HtmlEncode(formatLabel));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(locationLine));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(withWhom));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(withHint));
        sb.Append("</p></div><div><b>");
        sb.Append(WebUtility.HtmlEncode(travelTitle));
        sb.Append("</b><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(travelHint));
        sb.Append("</p></div></div>");
    }

    private static void AppendProposeForm(StringBuilder sb, CultureState culture, Guid applicationId)
    {
        sb.Append("<div class=\"card ndm-propose-slot\" id=\"ndm-propose-form\" style=\"margin-top:14px;background:#fbf6ef;border-style:dashed\"><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeFormTitle"]));
        sb.Append("</b><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeLead"]));
        sb.Append("</p><div class=\"g2b ndm-propose-fields\" style=\"gap:10px;margin-top:10px\"><label class=\"inp\" style=\"flex:1\"><span class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeDateLabel"]));
        sb.Append("</span>");
        sb.Append(NdMIcons.Calendar);
        sb.Append("<input type=\"text\" name=\"proposeDate\" data-ndm-propose-date required autocomplete=\"off\" style=\"border:0;background:transparent;flex:1;font:inherit;padding:0;color:var(--text)\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeDatePlaceholder"]));
        sb.Append("\" /></label><label class=\"inp\" style=\"flex:1\"><span class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeTimeLabel"]));
        sb.Append("</span>");
        sb.Append("""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/></svg>""");
        sb.Append("<input type=\"text\" name=\"proposeTime\" data-ndm-propose-time required autocomplete=\"off\" style=\"border:0;background:transparent;flex:1;font:inherit;padding:0;color:var(--text)\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeTimePlaceholder"]));
        sb.Append("\" /></label></div>");
        sb.Append("<textarea class=\"inp area\" data-ndm-propose-note required style=\"width:100%;margin-top:10px;min-height:56px;font:inherit;color:var(--text)\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeNotePlaceholder"]));
        sb.Append("\"></textarea><div style=\"margin-top:10px\"><button type=\"button\" class=\"btn line sm\" data-ndm-propose-submit data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.ProposeSubmitShort"]));
        sb.Append("</button></div></div>");
    }

    private static void AppendAside(StringBuilder sb, CultureState culture)
    {
        sb.Append("<aside class=\"card\"><h2 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.GoodToKnow.Title"]));
        sb.Append("</h2><div class=\"list\" style=\"margin-top:10px\"><div><span class=\"ico\">");
        sb.Append("""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/></svg>""");
        sb.Append("</span><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.GoodToKnow.Reminders"]));
        sb.Append("</p></div><div><span class=\"ico\">");
        sb.Append("""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/></svg>""");
        sb.Append("</span><p class=\"small\"><a class=\"lk\" href=\"/how-it-works\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.GoodToKnow.TipsLink"]));
        sb.Append("</a>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.GoodToKnow.TipsSuffix"]));
        sb.Append("</p></div><div><span class=\"ico\">");
        sb.Append("""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V3h6v1M9 11l2 2 4-4"/></svg>""");
        sb.Append("</span><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.GoodToKnow.Uwv"]));
        sb.Append("</p></div></div></aside>");
    }

    private static void AppendChatDock(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"chatdock\"><button type=\"button\" class=\"askl\" data-ndm-ask-lobsy><img src=\"/brand/svg/lobsy-merkteken.svg\" alt=\"\">");
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

    private const string ChatBubbleIcon =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 18 3 21V6a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2z"/></svg>""";

    private static string VacancySubtitle(AfterMatchThreadDto t) =>
        string.Join(" · ", new[] { t.CompanyName, t.LocationOrLink, t.TravelLabel }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string CompanyInitials(string company)
    {
        var parts = company.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : company.Length >= 2 ? company[..2].ToUpperInvariant() : company.ToUpperInvariant();
    }

    private static string FormatSlotTitle(AfterMatchSlotDto slot)
    {
        var start = AfterMatchMailSsrTime.ToAmsterdam(slot.StartUtc);
        var text = start.ToString("dddd d MMMM", Nl);
        return char.ToUpper(text[0], Nl) + text[1..];
    }

    private static string FormatSlotSubtitle(AfterMatchSlotDto slot)
    {
        var start = AfterMatchMailSsrTime.ToAmsterdam(slot.StartUtc);
        var end = AfterMatchMailSsrTime.ToAmsterdam(slot.EndUtc);
        return $"{start:HH:mm} tot {end:HH:mm}";
    }
}
