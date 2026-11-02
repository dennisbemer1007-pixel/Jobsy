using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilVakkenSsrHtml
{
    public static string BuildBody(
        PupilProgressStateDto _,
        CultureState culture,
        int page,
        IReadOnlyDictionary<string, (int Leuk, int Goed)> scores,
        string antiforgery,
        string savePath,
        string? error,
        bool groep78)
    {
        var pageKeys = PupilVakScores.KeysForPage(page);
        var sb = new StringBuilder();
        if (groep78)
        {
            sb.Append("""<div class="ll-vt" data-testid="leerling-vakken"><main class="ll-vt-main">""");
        }
        else
        {
            sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Vakken.Kicker"]));
            sb.Append("""<div class="ll-reis ll-vakken" data-testid="leerling-vakken">""");
        }

        sb.Append(groep78 ? """<section class="ll-vt-grid">""" : """<section class="ll-card ll-vakken-card">""");
        sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Heading"])).Append("</h1>");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-vakken-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Page\" value=\"").Append(page).Append("\" />");
        foreach (var key in pageKeys)
        {
            scores.TryGetValue(key, out var p);
            sb.Append("<article class=\"ll-vt-card\"><h2>").Append(PupilLeerlingSsrParts.E(culture[PupilVakSubjectLabels.UiKey(key)])).Append("</h2>");
            sb.Append("<input type=\"hidden\" name=\"Leuk.").Append(key).Append("\" value=\"").Append(p.Leuk).Append("\" />");
            sb.Append("<input type=\"hidden\" name=\"Goed.").Append(key).Append("\" value=\"").Append(p.Goed).Append("\" />");
            AppendScale(sb, key, "Leuk", p.Leuk, culture["Leerling.Vakken.Leuk"]);
            AppendScale(sb, key, "Goed", p.Goed, culture["Leerling.Vakken.Goed"]);
            sb.Append("</article>");
        }

        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("<div class=\"ll-question__nav\"><a class=\"btn-secondary\" href=\"");
        sb.Append(page == 2 ? "/leerling/vakken" : "/leerling/reis").Append("\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Back"])).Append("</a>");
        sb.Append("<button type=\"submit\" class=\"btn-primary\" data-testid=\"ll-vt-next\">");
        sb.Append(PupilLeerlingSsrParts.E(culture[page == 1 ? "Leerling.Vakken.Next" : "Leerling.Vakken.Finish"])).Append("</button></div></form></section></div>");
        return sb.ToString();
    }

    private static void AppendScale(StringBuilder sb, string subject, string field, int value, string label)
    {
        sb.Append("<div class=\"ll-vt-q\"><div class=\"ll-vt-ql\">").Append(PupilLeerlingSsrParts.E(label)).Append("</div><div class=\"ll-vt-sc\">");
        for (var v = 1; v <= 5; v++)
        {
            sb.Append("<button type=\"button\" class=\"ll-vt-scale-btn");
            if (value == v)
            {
                sb.Append(" is-on");
            }

            sb.Append("\" data-ll-vt-subject=\"").Append(subject).Append("\" data-ll-vt-field=\"").Append(field);
            sb.Append("\" data-ll-vt-scale=\"").Append(v).Append("\">").Append(v).Append("</button>");
        }

        sb.Append("</div></div>");
    }
}
