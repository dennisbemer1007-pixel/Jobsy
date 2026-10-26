using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Match;

internal static class MatchSsrGateHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, MatchProfileGateViewModel gate, string onboardingStartPath, bool fitGateOnly)
    {
        sb.Append("<div class=\"match-gate\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Fit.Gate"]));
        sb.Append("\">");
        if (fitGateOnly)
        {
            AppendFitGateOnly(sb, culture);
            return;
        }

        if (!gate.IsAuthenticated)
        {
            AppendAuthGate(sb, culture);
            return;
        }

        AppendProfileGate(sb, culture, gate, onboardingStartPath);
        sb.Append("</div>");
    }

    private static void AppendFitGateOnly(StringBuilder sb, CultureState culture)
    {
        sb.Append("<h2 class=\"match-gate__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Fit.Gate"]));
        sb.Append("</h2><p class=\"match-gate__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Fit.GateMatch"]));
        sb.Append("</p><div class=\"match-gate__actions\"><a class=\"match-page__cta\" href=\"");
        sb.Append(KbRoutes.CultureTest);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Dna.Culture"]));
        sb.Append("</a><a class=\"match-page__cta match-page__cta--ghost\" href=\"");
        sb.Append(KbRoutes.ValuesTest);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Dna.Values"]));
        sb.Append("</a></div>");
        AppendMapHint(sb, culture);
        sb.Append("</div>");
    }

    private static void AppendAuthGate(StringBuilder sb, CultureState culture)
    {
        var returnUrl = Uri.EscapeDataString("/candidate/match");
        sb.Append("<h2 class=\"match-gate__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.AuthTitle"]));
        sb.Append("</h2><p class=\"match-gate__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.AuthLead"]));
        sb.Append("</p><div class=\"match-gate__actions\"><a class=\"match-page__cta\" href=\"/login?returnUrl=");
        sb.Append(returnUrl);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Login"]));
        sb.Append("</a><a class=\"match-page__cta match-page__cta--ghost\" href=\"");
        sb.Append(KbRoutes.Map);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.BackToMap"]));
        sb.Append("</a></div>");
        AppendMapHint(sb, culture);
    }

    private static void AppendProfileGate(StringBuilder sb, CultureState culture, MatchProfileGateViewModel gate, string onboardingStartPath)
    {
        sb.Append("<h2 class=\"match-gate__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.ProfileTitle"]));
        sb.Append("</h2><p class=\"match-gate__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.ProfileLead"]));
        sb.Append("</p><p class=\"match-gate__progress\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Match.Gate.Progress"], gate.CompletedCount, gate.RequiredCount)));
        sb.Append("</p><ol class=\"whoami-checklist match-gate__checklist\">");
        AppendCheckItem(
            sb,
            gate.ProfileBasicsFilled,
            culture["Match.Gate.Check.BasicsTitle"],
            culture["Match.Gate.Check.BasicsLead"],
            culture["MatchUnlock.ContinueStart"],
            onboardingStartPath);
        AppendCheckItem(
            sb,
            gate.HasEducationLevel,
            culture["Match.Gate.Check.EducationTitle"],
            culture["Match.Gate.Check.EducationLead"],
            culture["MatchUnlock.ContinueStart"],
            onboardingStartPath);
        AppendCheckItem(
            sb,
            gate.WizardCompleted,
            culture["Match.Gate.Check.WizardTitle"],
            culture["Match.Gate.Check.WizardLead"],
            culture["MatchUnlock.StartOrResume"],
            onboardingStartPath);
        sb.Append("</ol>");
        AppendMapHint(sb, culture);
    }

    private static void AppendCheckItem(
        StringBuilder sb,
        bool done,
        string title,
        string lead,
        string linkLabel,
        string href)
    {
        sb.Append("<li");
        if (done)
        {
            sb.Append(" class=\"is-done\"");
        }

        sb.Append("><span class=\"whoami-check\" aria-hidden=\"true\">");
        sb.Append(done ? "✓" : "");
        sb.Append("</span><div><strong>");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</strong><p>");
        sb.Append(WebUtility.HtmlEncode(lead));
        sb.Append("</p>");
        if (!done)
        {
            sb.Append("<a class=\"engagement-btn\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(linkLabel));
            sb.Append("</a>");
        }

        sb.Append("</div></li>");
    }

    private static void AppendMapHint(StringBuilder sb, CultureState culture)
    {
        sb.Append("<p class=\"match-gate__hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.BroaderSearch"]));
        sb.Append(" <a href=\"");
        sb.Append(KbRoutes.Map);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
        sb.Append("</a>.</p>");
    }
}
