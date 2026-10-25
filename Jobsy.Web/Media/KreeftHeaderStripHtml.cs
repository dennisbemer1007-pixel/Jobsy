using System.Net;
using System.Text;

namespace Jobsy.Web.Media;

internal static class KreeftHeaderStripHtml
{
    internal static void Append(StringBuilder sb, string deskUrl, string mobUrl, string cssClass = "kreeft-header-strip")
    {
        sb.Append("<div class=\"");
        sb.Append(cssClass);
        sb.Append(" sollicitaties-hero-strip\" aria-hidden=\"true\" data-testid=\"kreeft-header-strip\"><div class=\"sollicitaties-hero-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(deskUrl));
        sb.Append("\" alt=\"\" width=\"1440\" height=\"225\" loading=\"lazy\" decoding=\"async\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(mobUrl));
        sb.Append("\" alt=\"\" width=\"780\" height=\"105\" loading=\"lazy\" decoding=\"async\" />");
        sb.Append("</div></div>");
    }

    internal static void Append(StringBuilder sb, string stripKey, string cssClass = "kreeft-header-strip")
        => Append(sb, KreeftHeaderStripAssets.Desk(stripKey), KreeftHeaderStripAssets.Mob(stripKey), cssClass);
}
