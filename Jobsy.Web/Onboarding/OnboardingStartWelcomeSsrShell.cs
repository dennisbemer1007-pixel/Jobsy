using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingStartWelcomeSsrShell
{
    internal static string Build(CultureState culture, string? firstName, string antiforgeryToken, string? errorMessage)
    {
        var title = string.IsNullOrWhiteSpace(firstName)
            ? culture["Onboarding.Welcome.TitleNoName"]
            : culture.Format("Onboarding.Welcome.Title", firstName.Trim());

        var sb = new StringBuilder();
        sb.Append("<section class=\"ob-wizard__welcome\" data-testid=\"onboarding-start-screen\"><form method=\"post\" action=\"/candidate/start\" class=\"ob-wizard ob-wizard--richting-a ob-wizard--rustig\" data-enhance=\"false\" data-enhance-nav=\"false\">");
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
        sb.Append("<div class=\"ob-rustig__layout\"><aside class=\"ob-rustig__sidebar ob-wizard__sidebar\" aria-label=\"Stappen\">");
        sb.Append("<img class=\"ob-rustig__logo-e\" src=\"");
        sb.Append(BrandImages.AppIconSvg);
        sb.Append("\" width=\"56\" height=\"56\" alt=\"Lobsy\" decoding=\"async\" data-logo-fallback=\"off\" draggable=\"false\" />");
        foreach (var item in OnboardingRustigWelcomeContent.SidebarNav)
        {
            var current = item.Number == "1";
            sb.Append("<div class=\"ob-wizard__side-item");
            if (current)
            {
                sb.Append(" is-current");
            }

            sb.Append("\"><span>");
            sb.Append(WebUtility.HtmlEncode(item.Number));
            sb.Append("</span><div><strong>");
            sb.Append(WebUtility.HtmlEncode(item.Title));
            sb.Append("</strong>");
            if (!string.IsNullOrWhiteSpace(item.Sub))
            {
                sb.Append("<small>");
                sb.Append(WebUtility.HtmlEncode(item.Sub));
                sb.Append("</small>");
            }

            sb.Append("</div></div>");
        }

        sb.Append("</aside><div class=\"ob-rustig__main\">");
        AppendChrome(sb, culture);
        AppendHero(sb);
        sb.Append("<div class=\"ob-rustig__page-head\"><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h1><p class=\"ob-rustig__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.WelcomeLead"]));
        sb.Append("</p></div><div class=\"ob-rustig__body\"><div class=\"ob-rustig__canvas\"><div class=\"ob-rustig__phase-cards\">");
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

        sb.Append("</div><p class=\"ob-wizard__hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.Hint"]));
        sb.Append("</p><div class=\"ob-rustig__footer-actions\"><button type=\"submit\" name=\"obNav\" value=\"begin\" class=\"ob-rustig__btn ob-rustig__btn--primary\" data-testid=\"onboarding-begin\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.CtaRustig"]));
        sb.Append("</button></div></div></div></div></div></div></form></section>");
        return sb.ToString();
    }

    private static void AppendChrome(StringBuilder sb, CultureState culture)
    {
        sb.Append("<header class=\"ob-rustig__chrome\"><div class=\"ob-rustig__mobile-brand\"><img class=\"ob-rustig__logo-e\" src=\"");
        sb.Append(BrandImages.AppIconSvg);
        sb.Append("\" width=\"40\" height=\"40\" alt=\"\" decoding=\"async\" data-logo-fallback=\"off\" draggable=\"false\" /></div>");
        sb.Append("<div class=\"ob-rustig__chrome-row\"><span class=\"ob-rustig__link ob-rustig__link--spacer\" aria-hidden=\"true\">‹</span>");
        sb.Append("<div class=\"ob-rustig__segments\" role=\"progressbar\" aria-valuenow=\"0\" aria-valuemin=\"0\" aria-valuemax=\"10\">");
        for (var i = 0; i < 4; i++)
        {
            sb.Append("<span><i style=\"--w:0%\"></i></span>");
        }

        sb.Append("</div><div class=\"ob-rustig__actions\"><a class=\"ob-rustig__link\" href=\"/candidate/profile\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Later"]));
        sb.Append("</a><a class=\"ob-rustig__link ob-rustig__link--underline\" href=\"/privacy/data\" title=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AccountPrivacyLink"]));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AccountPrivacyLink"]));
        sb.Append("</a></div></div><div class=\"ob-rustig__meta\"><span><strong>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.StartMeta"]));
        sb.Append("</strong> · ± ");
        sb.Append(OnboardingWizardCatalog.V3TotalMinutesEstimate);
        sb.Append(' ');
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.MinutesShort"]));
        sb.Append("</span></div></header>");
    }

    private static void AppendHero(StringBuilder sb)
    {
        sb.Append("<div class=\"ob-rustig__hero-band\" aria-hidden=\"true\" data-testid=\"ob-rustig-hero\">");
        sb.Append("<img class=\"ob-rustig__hero-img ob-rustig__hero-img--desk\" src=\"");
        sb.Append(OnboardingRustigPanoramaAssets.StartDesktop);
        sb.Append("\" width=\"2280\" height=\"340\" alt=\"\" loading=\"eager\" decoding=\"sync\" fetchpriority=\"high\" data-testid=\"ob-rustig-hero-desk\" />");
        sb.Append("<img class=\"ob-rustig__hero-img ob-rustig__hero-img--mob\" src=\"");
        sb.Append(OnboardingRustigPanoramaAssets.StartMobile);
        sb.Append("\" width=\"780\" height=\"184\" alt=\"\" loading=\"eager\" decoding=\"sync\" fetchpriority=\"high\" data-testid=\"ob-rustig-hero-mob\" /></div>");
    }
}
