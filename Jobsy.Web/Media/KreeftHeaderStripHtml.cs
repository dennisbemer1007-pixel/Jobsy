using System.Net;
using System.Text;

namespace Jobsy.Web.Media;

internal static class KreeftHeaderStripHtml
{
    internal static void Append(StringBuilder sb, string deskUrl, string mobUrl, string? stripKey = null, string cssClass = "kreeft-header-strip")
    {
        var key = stripKey ?? KreeftHeaderStripAssets.KeyFromDeskUrl(deskUrl);
        var pos = KreeftHeaderStripObjectPosition.ForKey(key);
        var style = $"--kreeft-strip-pos:{pos}";
        sb.Append("<div class=\"");
        sb.Append(cssClass);
        sb.Append(" sollicitaties-hero-strip\" aria-hidden=\"true\" data-testid=\"kreeft-header-strip\" data-kreeft-strip=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\"><div class=\"sollicitaties-hero-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(deskUrl));
        sb.Append("\" alt=\"\" width=\"1440\" height=\"225\" loading=\"lazy\" decoding=\"async\" style=\"");
        sb.Append(style);
        sb.Append("\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(mobUrl));
        sb.Append("\" alt=\"\" width=\"780\" height=\"105\" loading=\"lazy\" decoding=\"async\" style=\"");
        sb.Append(style);
        sb.Append("\" />");
        sb.Append("</div></div>");
    }

    internal static void Append(StringBuilder sb, string stripKey, string cssClass = "kreeft-header-strip")
    {
        var key = KreeftHeaderStripAssets.NormalizeKey(stripKey);
        Append(sb, KreeftHeaderStripAssets.Desk(key), KreeftHeaderStripAssets.Mob(key), key, cssClass);
    }
}
