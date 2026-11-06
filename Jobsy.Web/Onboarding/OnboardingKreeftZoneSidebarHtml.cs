using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingKreeftZoneSidebarHtml
{
    internal static void AppendAside(StringBuilder sb, CultureState culture, IReadOnlyList<OnboardingKreeftZoneSidebarNav.ZoneItem> zones)
    {
        sb.Append("<aside class=\"ob-rustig__sidebar ob-wizard__sidebar ob-rustig__sidebar--zones ob-rustig__sidebar--no-logo\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Reiskaart.Aria"]));
        sb.Append("\">");
        foreach (var zone in zones)
        {
            sb.Append("<div class=\"ob-wizard__side-item ob-wizard__side-item--zone");
            if (zone.IsCurrent)
            {
                sb.Append(" is-current");
            }

            if (zone.IsDone)
            {
                sb.Append(" is-done");
            }

            sb.Append("\"><span class=\"ob-wizard__zone-mark\" aria-hidden=\"true\"></span><div><strong>");
            sb.Append(WebUtility.HtmlEncode(culture[zone.LabelKey]));
            sb.Append("</strong></div></div>");
        }

        sb.Append("</aside>");
    }
}
