using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingStartWelcomeSsrShell
{
    internal static string Build(CultureState culture, string? firstName, string antiforgeryToken, string? errorMessage)
    {
        var title = string.IsNullOrWhiteSpace(firstName)
            ? culture["Onboarding.Welcome.JourneyTitleNoName"]
            : culture.Format("Onboarding.Welcome.JourneyTitle", firstName.Trim());

        var sb = new StringBuilder();
        sb.Append("<section class=\"ob-wizard__welcome\" data-testid=\"onboarding-start-screen\"><form method=\"post\" action=\"/candidate/start\" class=\"ob-wizard ob-wizard--richting-a ob-wizard--rustig ob-rustig--start-strand\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" />");
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(errorMessage));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"ob-rustig\" data-ob-rustig-screen=\"start\">");
        AppendHero(sb);
        sb.Append("<div class=\"ob-rustig__layout\">");
        OnboardingKreeftZoneSidebarHtml.AppendAside(sb, culture, OnboardingKreeftZoneSidebarNav.ForWelcome());
        sb.Append("<div class=\"ob-rustig__main\"><div class=\"ob-rustig__page-head\"><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h1><p class=\"ob-rustig__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.WelcomeLead"]));
        sb.Append("</p></div><div class=\"ob-rustig__body ob-rustig__body--with-reiskaart\">");
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(3));
        sb.Append("<div class=\"ob-rustig__body-main\"><div class=\"ob-rustig__canvas\"><div class=\"ob-rustig__phase-cards\">");
        foreach (var phase in OnboardingRustigWelcomeContent.JourneyPhases)
        {
            sb.Append("<article class=\"ob-rustig__phase-card\"><span>");
            sb.Append(WebUtility.HtmlEncode(phase.Number));
            sb.Append("</span><div><strong>");
            sb.Append(WebUtility.HtmlEncode(phase.Title));
            sb.Append("</strong><small>");
            sb.Append(WebUtility.HtmlEncode(phase.Sub));
            sb.Append("</small></div>");
            if (!string.IsNullOrWhiteSpace(phase.Meta))
            {
                sb.Append("<small>");
                sb.Append(WebUtility.HtmlEncode(phase.Meta));
                sb.Append("</small>");
            }

            sb.Append("</article>");
        }

        sb.Append("</div><section class=\"ob-rustig__before-start\" aria-labelledby=\"ob-before-start-title\"><h2 id=\"ob-before-start-title\" class=\"ob-rustig__before-start-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.BeforeStartTitle"]));
        sb.Append("</h2><p class=\"ob-rustig__before-start-body\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.BeforeStartBody"]));
        sb.Append("</p></section><div class=\"ob-rustig__footer-actions\"><button type=\"submit\" name=\"obNav\" value=\"begin\" class=\"ob-rustig__btn ob-rustig__btn--primary\" data-testid=\"onboarding-begin\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.CtaRustig"]));
        sb.Append("</button></div></div></div></div></div></div></div></form></section>");
        return sb.ToString();
    }

    private static void AppendHero(StringBuilder sb)
    {
        var heroDesk = OnboardingRustigPanoramaAssets.StartDesktop;
        var heroMob = OnboardingRustigPanoramaAssets.StartMobile;
        sb.Append("<div class=\"ob-rustig__hero-band ob-start-strand-hero\" aria-hidden=\"true\" data-testid=\"ob-rustig-hero\">");
        sb.Append("<img class=\"ob-rustig__hero-img ob-rustig__hero-img--desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(heroDesk));
        sb.Append("\" width=\"1440\" height=\"225\" alt=\"\" loading=\"eager\" decoding=\"sync\" fetchpriority=\"high\" data-testid=\"ob-rustig-hero-desk\" />");
        sb.Append("<img class=\"ob-rustig__hero-img ob-rustig__hero-img--mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(heroMob));
        sb.Append("\" width=\"780\" height=\"105\" alt=\"\" loading=\"eager\" decoding=\"sync\" fetchpriority=\"high\" data-testid=\"ob-rustig-hero-mob\" /></div>");
    }
}
