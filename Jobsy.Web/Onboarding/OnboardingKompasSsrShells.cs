using System.Net;
using System.Text;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingKompasSsrShells
{
    internal sealed record ReadyModel(
        string FirstName,
        OnboardingImpression? Impression,
        string HubUrl,
        string FullTestsUrl);

    internal sealed record HubModel(
        string FirstName,
        bool PassportEnabled,
        string ProfileUrl,
        string PassportUrl,
        string DnaTabUrl,
        string TestsTabUrl,
        string FitTabUrl);

    public static string BuildReadyBody(CultureState culture, ReadyModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-rustig__canvas ob-rustig__kompas\">");
        sb.Append("<div class=\"ob-rustig__kompas-finale\" data-testid=\"onboarding-rustig-kompas\">");
        sb.Append("<span class=\"ob-rustig__kompas-badge\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.Badge"]));
        sb.Append("</span>");

        sb.Append("<section class=\"ob-finale ob-finale--no-hero\" data-testid=\"onboarding-finale-v2\">");
        sb.Append("<div class=\"ob-finale__wrap\">");
        sb.Append("<div class=\"ob-finale__ctas ob-finale__ctas--heroless\" data-testid=\"onboarding-finale-heroless-ctas\">");
        sb.Append("<a class=\"ob-finale__btn\" href=\"");
        sb.Append(WebUtility.HtmlEncode(model.HubUrl));
        sb.Append("\" data-testid=\"onboarding-kompas-complete\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Finish.Cta"]));
        sb.Append("</a>");
        sb.Append("<a class=\"ob-finale__link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(model.FullTestsUrl));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.FullTests"]));
        sb.Append("</a></div>");

        AppendTestSummaries(sb, culture, model.Impression);
        AppendDirections(sb, culture);

        sb.Append("</div></section></div></div>");
        return sb.ToString();
    }

    public static string BuildHubBody(CultureState culture, HubModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<section class=\"kompas kompas--ssr\" aria-labelledby=\"kompas-title\" data-testid=\"candidate-kompas-ssr\">");
        sb.Append("<header class=\"kompas__intro\">");
        sb.Append("<h2 id=\"kompas-title\" class=\"kompas__heading\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Title"]));
        sb.Append("</h2>");
        sb.Append("<p class=\"ob-wizard__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Ssr.Lead"]));
        sb.Append("</p></header>");

        sb.Append("<nav class=\"kompas-tabs admin-sublinks\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.TabsLabel"]));
        sb.Append("\">");
        AppendTabLink(sb, culture["Kompas.TabDna"], model.DnaTabUrl);
        AppendTabLink(sb, culture["Kompas.TabProfile"], model.ProfileUrl + "?tab=" + CandidateKompasTabs.Profile);
        AppendTabLink(sb, culture["Kompas.TabTests"], model.TestsTabUrl);
        AppendTabLink(sb, culture["Kompas.TabFit"], model.FitTabUrl);
        sb.Append("</nav>");

        sb.Append("<div class=\"kompas__ssr-actions\">");
        sb.Append("<a class=\"ob-rustig__btn ob-rustig__btn--primary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(model.ProfileUrl));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Ssr.OpenProfile"]));
        sb.Append("</a>");

        if (model.PassportEnabled)
        {
            sb.Append("<a class=\"ob-rustig__btn\" href=\"");
            sb.Append(WebUtility.HtmlEncode(model.PassportUrl));
            sb.Append("\" data-testid=\"kompas-go-passport\">");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.Do.Passport.Title"]));
            sb.Append("</a>");
        }

        sb.Append("</div></section>");
        return sb.ToString();
    }

    private static void AppendTabLink(StringBuilder sb, string label, string href)
    {
        sb.Append("<a class=\"admin-sublink\" href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</a>");
    }

    private static void AppendTestSummaries(StringBuilder sb, CultureState culture, OnboardingImpression? impression)
    {
        var summaries = OnboardingRustigKompasContent.TestSummaries(impression);
        if (summaries.Count == 0)
        {
            return;
        }

        sb.Append("<section class=\"ob-rustig__kompas-tests\" aria-labelledby=\"kompas-tests-h\">");
        sb.Append("<h2 id=\"kompas-tests-h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.TestsTitle"]));
        sb.Append("</h2><p class=\"ob-rustig__kompas-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.TestsLead"]));
        sb.Append("</p><div class=\"ob-rustig__kompas-test-grid\">");
        foreach (var card in summaries)
        {
            sb.Append("<article class=\"ob-rustig__kompas-test-card\"><small>");
            sb.Append(WebUtility.HtmlEncode(culture[card.TabKey]));
            sb.Append("</small>");
            if (!string.IsNullOrWhiteSpace(card.Title))
            {
                sb.Append("<h3>");
                sb.Append(WebUtility.HtmlEncode(card.Title));
                sb.Append("</h3>");
            }

            if (!string.IsNullOrWhiteSpace(card.Summary))
            {
                sb.Append("<p>");
                sb.Append(WebUtility.HtmlEncode(card.Summary));
                sb.Append("</p>");
            }

            sb.Append("</article>");
        }

        sb.Append("</div></section>");
    }

    private static void AppendDirections(StringBuilder sb, CultureState culture)
    {
        sb.Append("<section class=\"ob-rustig__kompas-directions\" aria-labelledby=\"kompas-dir-h\">");
        sb.Append("<h2 id=\"kompas-dir-h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.DirectionsTitle"]));
        sb.Append("</h2><p class=\"ob-rustig__kompas-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.DirectionsLead"]));
        sb.Append("</p><div class=\"ob-rustig__kompas-dir-grid\">");
        foreach (var card in OnboardingRustigKompasContent.Directions)
        {
            sb.Append("<article class=\"ob-rustig__kompas-dir-card\"><img src=\"");
            sb.Append(WebUtility.HtmlEncode(card.ImageUrl));
            sb.Append("\" alt=\"\" width=\"480\" height=\"360\" decoding=\"async\" /><div><h3>");
            sb.Append(WebUtility.HtmlEncode(culture[card.TitleKey]));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(culture[card.BodyKey]));
            sb.Append("</p></div></article>");
        }

        sb.Append("</div></section>");
    }
}
