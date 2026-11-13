using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingStartWelcomeSsrShell
{
    private static readonly (string TitleKey, string LeadKey, string TimeKey)[] WelcomeSteps =
    [
        ("Onboarding.Phase.OverJou", "Onboarding.Welcome.OverJou", "Onboarding.Welcome.Time2"),
        ("Onboarding.Welcome.Background", "Onboarding.Welcome.BackgroundLead", "Onboarding.Welcome.Time1"),
        ("Onboarding.Welcome.Dream", "Onboarding.Welcome.DreamLead", "Onboarding.Welcome.TimeHalf"),
        ("Onboarding.Phase.WieBenJij", "Onboarding.Welcome.TestsLead", "Onboarding.Welcome.Time2"),
    ];

    internal static string Build(CultureState culture, string? firstName, string antiforgeryToken, string? errorMessage)
    {
        var title = string.IsNullOrWhiteSpace(firstName)
            ? culture["Onboarding.Welcome.TitleNoName"]
            : culture.Format("Onboarding.Welcome.Title", firstName.Trim());

        var sb = new StringBuilder();
        sb.Append("<section class=\"ob-wizard__welcome ob-v3-welcome\" data-ob-rustig-screen=\"start\" data-testid=\"onboarding-start-screen\">");
        sb.Append("<form method=\"post\" action=\"/candidate/start\" class=\"ob-wizard ob-wizard--richting-a ob-wizard--rustig ob-wizard--v3\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" />");
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(errorMessage));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"ob-v3-welcome__content\">");
        sb.Append("<h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h1><p class=\"ob-v3-welcome__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.Lead"]));
        sb.Append("</p><div class=\"ob-v3-welcome__steps\">");
        for (var i = 0; i < WelcomeSteps.Length; i++)
        {
            var (titleKey, leadKey, timeKey) = WelcomeSteps[i];
            sb.Append("<div class=\"ob-v3-welcome__step-card\"><em aria-hidden=\"true\">");
            sb.Append(i + 1);
            sb.Append("</em><div><b>");
            sb.Append(WebUtility.HtmlEncode(culture[titleKey]));
            sb.Append("</b><small>");
            sb.Append(WebUtility.HtmlEncode(culture[leadKey]));
            sb.Append("</small></div><span class=\"ob-v3-welcome__time\">");
            sb.Append(WebUtility.HtmlEncode(culture[timeKey]));
            sb.Append("</span></div>");
        }

        sb.Append("</div><p class=\"ob-v3-welcome__hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.Hint"]));
        sb.Append("</p></div><div class=\"ob-v3-welcome__foot\">");
        sb.Append("<button type=\"submit\" name=\"obNav\" value=\"begin\" class=\"ob-v3-welcome__cta ob-rustig__btn ob-rustig__btn--primary\" data-testid=\"onboarding-begin\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.Cta"]));
        sb.Append("</button></div></form></section>");
        sb.Append(KreeftReiskaartMarkup.ForScreen(culture, 3));
        return sb.ToString();
    }
}
