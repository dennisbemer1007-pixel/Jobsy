using System.Net;
using System.Text;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Career;

internal static class CareerSsrSectorIcon
{
    internal static void Append(StringBuilder sb, string kind, string cssClass)
    {
        sb.Append("<span class=\"uwv-sector-icon ");
        sb.Append(WebUtility.HtmlEncode(cssClass));
        sb.Append("\" aria-hidden=\"true\">");
        sb.Append(SvgFor(kind));
        sb.Append("</span>");
    }

    private static string SvgFor(string kind)
        => kind switch
        {
            "logistiek" or "techniek" or "bouw" or "career" => NavIcons.Career,
            "zorg" => NavIcons.Profile,
            "it" or "kantoor" => NavIcons.Search,
            "onderwijs" or "compass" => NavIcons.Compass,
            _ => NavIcons.Career
        };
}
