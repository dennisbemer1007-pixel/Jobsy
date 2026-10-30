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
        sb.Append("<div class=\"ob-kreeft-start-grid\">");
        sb.Append("<div class=\"ob-kreeft-start-main\"><p class=\"ob-kreeft-zonetag\"><b>");
        sb.Append(WebUtility.HtmlEncode(culture["Reiskaart.Zone.Strand"]));
        sb.Append("</b>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.ZoneTagSub"]));
        sb.Append("</p><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h1><p class=\"ob-rustig__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.Lead"]));
        sb.Append("</p><ol class=\"ob-kreeft-zone-list\">");
        foreach (var item in OnboardingKreeftWelcomeContent.ZoneCards)
        {
            sb.Append("<li><b>");
            sb.Append(WebUtility.HtmlEncode(culture[item.TitleKey]));
            sb.Append("</b><span>");
            sb.Append(WebUtility.HtmlEncode(culture[item.BodyKey]));
            sb.Append("</span></li>");
        }

        sb.Append("</ol><div class=\"ob-kreeft-before-card\"><h3>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.BeforeStartTitle"]));
        sb.Append("</h3><div class=\"ob-kreeft-consent-opt ob-kreeft-consent-opt--on\"><i aria-hidden=\"true\"></i><div><strong>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.ConsentRequired"]));
        sb.Append("</strong><small>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.ConsentRequiredSub"]));
        sb.Append("</small></div></div><div class=\"ob-kreeft-consent-opt\"><i aria-hidden=\"true\"></i><div><strong>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.ConsentEmail"]));
        sb.Append("</strong><small>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.ConsentEmailSub"]));
        sb.Append("</small></div></div><div class=\"ob-rustig__footer-actions ob-kreeft-before-actions\"><a class=\"ob-rustig__link ob-rustig__link--underline\" href=\"/privacy/data\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.PrivacyLink"]));
        sb.Append("</a><button type=\"submit\" name=\"obNav\" value=\"begin\" class=\"ob-rustig__btn ob-rustig__btn--primary\" data-testid=\"onboarding-begin\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Kreeft.Welcome.CtaCoast"]));
        sb.Append("</button></div></div></div>");
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(3));
        sb.Append("</div></div></form></section>");
        return sb.ToString();
    }
}
