using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Onboarding;

internal static class KreeftReiskaartMarkup
{
    internal static string ForLandingHero(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"pub-landing__reiskaart-stack\" data-testid=\"landing-reiskaart-stack\">");
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(1));
        sb.Append("<p class=\"pub-landing__why-lobsy\">");
        sb.Append(WebUtility.HtmlEncode(culture["Landing.Hero.WhyLobsy.Body"]));
        sb.Append(' ');
        sb.Append("<a class=\"pub-landing__why-lobsy-link\" href=\"");
        sb.Append(PublicRoutes.About);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Landing.Hero.WhyLobsy.Link"]));
        sb.Append("</a></p></div>");
        return sb.ToString();
    }

    internal static string ForScreen(CultureState culture, int screenNumber)
    {
        var sb = new StringBuilder();
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(screenNumber));
        return sb.ToString();
    }

    internal static string ForOnboardingStep(CultureState culture, int step)
        => ForScreen(culture, KreeftReiskaartCatalog.ScreenForOnboardingStep(step));
}
