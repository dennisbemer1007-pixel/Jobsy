using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Onboarding;

internal static class KreeftReiskaartMarkup
{
    internal static string ForScreen(CultureState culture, int screenNumber)
    {
        var sb = new StringBuilder();
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(screenNumber));
        return sb.ToString();
    }

    internal static string ForOnboardingStep(CultureState culture, int step)
        => ForScreen(culture, KreeftReiskaartCatalog.ScreenForOnboardingStep(step));
}
