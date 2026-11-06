using System.Net;
using System.Text;

namespace Jobsy.Web.Hosting;

/// <summary>Embed JSON in <c>&lt;script type="application/json"&gt;</c> without breaking parse (no HTML entity encoding).</summary>
internal static class JsonScriptEmbed
{
    internal static void Append(StringBuilder sb, string elementId, string json)
    {
        sb.Append("<script type=\"application/json\" id=\"");
        sb.Append(WebUtility.HtmlEncode(elementId));
        sb.Append("\">");
        foreach (var ch in json)
        {
            if (ch == '<')
            {
                sb.Append("\\u003c");
            }
            else
            {
                sb.Append(ch);
            }
        }

        sb.Append("</script>");
    }
}
