using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Match;

internal static class MatchSsrTravelHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, int? minutes, string? transport, bool toBureau)
    {
        if (minutes is not int mins || mins <= 0)
        {
            return;
        }

        var mode = string.IsNullOrWhiteSpace(transport) ? TransportLabels.Bike : transport;
        var verb = KbLabels.TransportVerb(culture, mode);
        sb.Append("<span class=\"kb-travel");
        if (toBureau)
        {
            sb.Append(" kb-travel--bureau");
        }

        sb.Append("\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Kb.Travel.Aria"], mins, verb)));
        sb.Append("\"><span class=\"kb-travel__icon\" aria-hidden=\"true\">");
        sb.Append(KbIcons.Transport(TransportLabels.Canonical(mode)).Value);
        sb.Append("</span><span class=\"kb-travel__text\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Kb.Travel.Minutes"], mins, verb)));
        sb.Append("</span>");
        if (toBureau)
        {
            sb.Append("<span class=\"kb-travel__bureau\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Travel.ToBureauSimple"]));
            sb.Append("</span>");
        }

        sb.Append("</span>");
    }
}
