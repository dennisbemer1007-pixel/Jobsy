using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingJourneyConsentSsrMarkup
{
    private static readonly (string TitleKey, string BenefitKey)[] Stations =
    [
        ("Onboarding.Test.Competency", "Onboarding.Journey.Benefit.Work"),
        ("Onboarding.Test.Career", "Onboarding.Journey.Benefit.Fun"),
        ("Onboarding.Test.Culture", "Onboarding.Journey.Benefit.Vibe"),
        ("Onboarding.Test.Values", "Onboarding.Journey.Benefit.Values"),
    ];

    public static string Build(CultureState culture, bool consentChecked)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-v3-consent\" data-testid=\"test-journey-consent\" data-ob-rustig-screen=\"step-07\">");
        sb.Append("<ol class=\"ob-v3-consent__grid\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Journey.StationsAria"]));
        sb.Append("\">");
        var index = 0;
        foreach (var (titleKey, benefitKey) in Stations)
        {
            index++;
            var stateClass = index == 1 ? "is-next" : "is-todo";
            sb.Append("<li class=\"ob-v3-consent__card ");
            sb.Append(stateClass);
            sb.Append("\"><p class=\"ob-v3-consent__name\">");
            sb.Append(WebUtility.HtmlEncode(culture[titleKey]));
            sb.Append("</p><p class=\"ob-v3-consent__benefit\">");
            sb.Append(WebUtility.HtmlEncode(culture[benefitKey]));
            sb.Append("</p></li>");
        }

        sb.Append("</ol>");
        sb.Append("<label class=\"ob-wizard__consent ob-v3-consent__check\"><input type=\"checkbox\" name=\"consentRequested\" value=\"true\" data-testid=\"onboarding-consent\"");
        if (consentChecked)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Consent.Check"]));
        sb.Append("</label></div>");
        return sb.ToString();
    }
}
