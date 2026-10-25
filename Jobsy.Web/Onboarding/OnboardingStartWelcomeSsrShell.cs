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
        sb.Append("<section class=\"ob-wizard__welcome\" data-testid=\"onboarding-start-screen\"><form method=\"post\" action=\"/candidate/start\" class=\"ob-wizard ob-wizard--richting-a ob-wizard--rustig ob-rustig--start-journey\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" />");
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(errorMessage));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"ob-rustig\" data-ob-rustig-screen=\"start-journey\">");
        sb.Append("<div class=\"ob-rustig__layout\"><aside class=\"ob-rustig__sidebar ob-wizard__sidebar ob-rustig__sidebar--no-logo\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.IllustratedSidebar.SubstepsAria"]));
        sb.Append("\">");
        foreach (var item in OnboardingStartJourneyContent.SidebarNav(culture))
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
        sb.Append("<div class=\"ob-rustig__page-head ob-rustig__page-head--compact\"><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h1><p class=\"ob-rustig__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.WelcomeLead"]));
        sb.Append("</p></div><div class=\"ob-rustig__body ob-rustig__body--journey-start\"><div class=\"ob-rustig__canvas\">");
        AppendJourneyRoute(sb, culture);
        sb.Append("</div><div class=\"ob-rustig__footer-actions ob-rustig__footer-actions--above-fold\"><button type=\"submit\" name=\"obNav\" value=\"begin\" class=\"ob-rustig__btn ob-rustig__btn--primary\" data-testid=\"onboarding-begin\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Welcome.CtaRustig"]));
        sb.Append("</button></div><p class=\"ob-wizard__hint ob-wizard__hint--compact\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.StartJourney.Hint"]));
        sb.Append("</p></div></div></div></div></form></section>");
        return sb.ToString();
    }

    private static void AppendJourneyRoute(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"ob-start-journey-route\" data-testid=\"onboarding-start-journey-route\">");
        sb.Append("<svg class=\"ob-start-journey-route__svg ob-start-journey-route__svg--horizontal\" viewBox=\"0 0 1000 240\" preserveAspectRatio=\"none\" aria-hidden=\"true\">");
        sb.Append("<defs><linearGradient id=\"obStartJourneyStrokeH\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#c4a574\" /><stop offset=\"45%\" stop-color=\"#8fb8a8\" /><stop offset=\"100%\" stop-color=\"#d4b878\" />");
        sb.Append("</linearGradient></defs>");
        sb.Append("<path d=\"M 90 122 C 175 122, 220 68, 270 68 S 395 127, 490 127 S 615 62, 710 62 S 820 113, 910 113\" fill=\"none\" stroke=\"url(#obStartJourneyStrokeH)\" stroke-width=\"13\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.62\" /></svg>");
        sb.Append("<svg class=\"ob-start-journey-route__svg ob-start-journey-route__svg--vertical\" viewBox=\"0 0 100 460\" preserveAspectRatio=\"none\" aria-hidden=\"true\">");
        sb.Append("<defs><linearGradient id=\"obStartJourneyStrokeV\" x1=\"0%\" y1=\"0%\" x2=\"0%\" y2=\"100%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#c4a574\" /><stop offset=\"50%\" stop-color=\"#8fb8a8\" /><stop offset=\"100%\" stop-color=\"#d4b878\" />");
        sb.Append("</linearGradient></defs>");
        sb.Append("<path d=\"M 60 46 C 60 92, 40 92, 40 138 C 40 184, 60 184, 60 230 C 60 276, 40 276, 40 322 C 40 368, 60 368, 60 414\" fill=\"none\" stroke=\"url(#obStartJourneyStrokeV)\" stroke-width=\"7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.65\" /></svg>");
        sb.Append("<ol class=\"ob-start-journey-route__stops\">");
        foreach (var stop in OnboardingStartJourneyContent.Stops(culture))
        {
            var isCurrent = stop.Number == "1";
            var isSideA = int.Parse(stop.Number) % 2 == 1;
            sb.Append("<li class=\"ob-start-journey-route__stop ob-start-journey-route__stop--");
            sb.Append(WebUtility.HtmlEncode(stop.Number));
            if (isCurrent)
            {
                sb.Append(" is-current");
            }

            sb.Append(isSideA ? " is-side-a" : " is-side-b");
            sb.Append("\"><div class=\"ob-start-journey-route__stop-inner\">");
            if (isCurrent)
            {
                sb.Append("<span class=\"ob-start-journey-route__here\">");
                sb.Append(WebUtility.HtmlEncode(OnboardingStartJourneyContent.HereMarker(culture)));
                sb.Append("</span>");
            }

            sb.Append("<div class=\"ob-start-journey-route__anchor\"><div class=\"ob-start-journey-route__vignette");
            if (isCurrent)
            {
                sb.Append(" is-current");
            }

            sb.Append("\"><img src=\"");
            sb.Append(WebUtility.HtmlEncode(stop.VignettePath));
            sb.Append("\" alt=\"\" width=\"96\" height=\"96\" loading=\"eager\" decoding=\"async\" /></div></div>");
            sb.Append("<div class=\"ob-start-journey-route__copy\"><div class=\"ob-start-journey-route__label\">");
            sb.Append("<p class=\"ob-start-journey-route__title\">");
            sb.Append(WebUtility.HtmlEncode(stop.Title));
            sb.Append("</p><p class=\"ob-start-journey-route__sub\">");
            sb.Append(WebUtility.HtmlEncode(stop.Subtitle));
            sb.Append("</p></div></div></div></li>");
        }

        sb.Append("</ol></div>");
    }

    private static void AppendChrome(StringBuilder sb, CultureState culture)
    {
        sb.Append("<header class=\"ob-rustig__chrome ob-rustig__chrome--compact\"><div class=\"ob-rustig__mobile-brand\"><img class=\"ob-rustig__logo-e\" src=\"");
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
        var heroDesk = OnboardingRustigPanoramaAssets.StartDesktop;
        var heroMob = OnboardingRustigPanoramaAssets.StartMobile;
        sb.Append("<div class=\"ob-rustig__hero-band ob-start-journey-hero\" style=\"--ob-start-journey-hero-mob: url('");
        sb.Append(WebUtility.HtmlEncode(heroMob));
        sb.Append("');\" aria-hidden=\"true\" data-testid=\"ob-rustig-hero\">");
        sb.Append("<img class=\"ob-rustig__hero-img ob-rustig__hero-img--desk ob-start-journey-hero__img ob-start-journey-hero__img--shared\" src=\"");
        sb.Append(WebUtility.HtmlEncode(heroDesk));
        sb.Append("\" width=\"1440\" height=\"225\" alt=\"\" loading=\"eager\" decoding=\"sync\" fetchpriority=\"high\" data-testid=\"ob-start-journey-hero-desk\" />");
        sb.Append("<img class=\"ob-rustig__hero-img ob-rustig__hero-img--mob ob-start-journey-hero__img ob-start-journey-hero__img--shared\" src=\"");
        sb.Append(WebUtility.HtmlEncode(heroMob));
        sb.Append("\" width=\"780\" height=\"105\" alt=\"\" loading=\"eager\" decoding=\"sync\" fetchpriority=\"high\" /></div>");
    }
}
