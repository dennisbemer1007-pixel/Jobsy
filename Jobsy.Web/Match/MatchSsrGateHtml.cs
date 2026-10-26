using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;

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
            sb.Append("</a></div><p class=\"match-gate__hint\"><a href=\"");
            sb.Append(KbRoutes.Map);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
            sb.Append("</a></p></div>");
            return;
        }

        if (!gate.IsAuthenticated)
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
            sb.Append("</a></div><p class=\"match-gate__hint\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
            sb.Append("</p></div>");
            return;
        }

        sb.Append("<h2 class=\"match-gate__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.ProfileTitle"]));
        sb.Append("</h2><p class=\"match-gate__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Gate.ProfileLead"]));
        sb.Append("</p><p class=\"match-gate__progress\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Match.Gate.Progress"], gate.CompletedCount, gate.RequiredCount)));
        sb.Append("</p><ol class=\"whoami-checklist match-gate__checklist\">");
        AppendCheck(sb, gate.ProfileBasicsFilled, culture["MatchUnlock.ContinueStart"], onboardingStartPath);
        AppendCheck(sb, gate.HasEducationLevel, culture["MatchUnlock.ContinueStart"], onboardingStartPath);
        AppendCheck(sb, gate.WizardCompleted, culture["MatchUnlock.StartOrResume"], onboardingStartPath);
        sb.Append("</ol><p class=\"match-gate__hint\"><a href=\"");
        sb.Append(KbRoutes.Map);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
        sb.Append("</a></p></div>");
    }

    private static void AppendCheck(StringBuilder sb, bool done, string linkLabel, string href)
    {
        sb.Append("<li");
        if (done)
        {
            sb.Append(" class=\"is-done\"");
        }

        sb.Append("><span class=\"whoami-check\" aria-hidden=\"true\">");
        sb.Append(done ? "✓" : "");
        sb.Append("</span><div>");
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
}
