using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingKreeftRailHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, IReadOnlyList<OnboardingKreeftRailCatalog.RailZone> zones)
    {
        sb.Append("<aside class=\"ob-kreeft-rail\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Reiskaart.Aria"]));
        sb.Append("\"><ol>");
        foreach (var zone in zones)
        {
            sb.Append("<li");
            if (zone.IsCurrent)
            {
                sb.Append(" class=\"is-current\"");
            }
            else if (zone.IsDone)
            {
                sb.Append(" class=\"is-done\"");
            }

            sb.Append("><i aria-hidden=\"true\"></i><div><b>");
            sb.Append(WebUtility.HtmlEncode(culture[zone.TitleKey]));
            sb.Append("</b><small>");
            sb.Append(WebUtility.HtmlEncode(zone.ActiveSub ?? culture[zone.DefaultSubKey]));
            sb.Append("</small></div></li>");
        }

        sb.Append("</ol></aside>");
    }
}
