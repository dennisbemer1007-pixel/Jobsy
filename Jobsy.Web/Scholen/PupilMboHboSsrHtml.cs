using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilMboHboSsrHtml
{
    public static string Uitleg(string antiforgery, string postPath)
        => $"""
            <div class="mhbo-wrap" style="max-width:860px;padding-top:56px"><div class="mhbo-card" style="padding:40px">
            <span class="mhbo-chip">Welkom</span><h1 style="margin-top:14px">Ontdek wat bij je past</h1>
            <form method="post" action="{PupilLeerlingSsrParts.E(postPath)}">{antiforgery}
            <button type="submit" class="mhbo-btn">Naar mijn testen →</button></form></div></div>
            """;

    public static string Overzicht(PupilMboHboOverviewDto model)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-wrap"><h1>Jouw testen</h1><div class="mhbo-grid mhbo-overview-grid">""");
        foreach (var block in model.Blocks)
        {
            sb.Append("<div class=\"mhbo-tile\"><h3>").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</h3>");
            sb.Append("<a class=\"mhbo-btn\" href=\"/leerling/mbo-hbo/test/").Append(PupilLeerlingSsrParts.E(block.Key)).Append("\">Open</a></div>");
        }
        if (model.PassportUnlocked)
        {
            sb.Append("""<div class="mhbo-tile mhbo-tile--passport"><a class="mhbo-btn" href="/leerling/mbo-hbo/paspoort">Paspoort</a></div>""");
        }
        sb.Append("</div></div>");
        return sb.ToString();
    }

    public static string TestQuestion(PupilMboHboQuestionDto q, string _, string antiforgery, string postPath)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"mhbo-wrap\"><form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(postPath)).Append("\" data-mhbo-scale-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"").Append(PupilLeerlingSsrParts.E(q.ItemId)).Append("\" />");
        sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(q.Question)).Append("</h1>");
        if (q.Scale == PupilMboHboScaleKind.SubjectLeukGoed)
        {
            sb.Append("<input type=\"hidden\" name=\"Leuk\" value=\"").Append(q.CurrentLeuk ?? 0).Append("\" />");
            sb.Append("<input type=\"hidden\" name=\"Goed\" value=\"").Append(q.CurrentGoed ?? 0).Append("\" />");
            sb.Append(ScaleRow("Leuk", q.CurrentLeuk));
            sb.Append(ScaleRow("Goed", q.CurrentGoed));
        }
        else
        {
            sb.Append("<input type=\"hidden\" name=\"Value\" value=\"").Append(q.CurrentValue ?? 0).Append("\" />");
            sb.Append(ScaleRow("Value", q.CurrentValue));
        }
        sb.Append("<button type=\"submit\" class=\"mhbo-btn\">Volgende →</button></form></div>");
        return sb.ToString();
    }

    private static string ScaleRow(string field, int? selected)
    {
        var sb = new StringBuilder("<div class=\"mhbo-scale\">");
        for (var v = 1; v <= 5; v++)
        {
            sb.Append("<button type=\"button\" class=\"mhbo-scale-opt");
            if (selected == v) sb.Append(" is-on");
            sb.Append("\" data-mhbo-scale-field=\"").Append(field).Append("\" data-mhbo-scale-value=\"").Append(v).Append("\">").Append(v).Append("</button>");
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    public static string Uitkomst(PupilMboHboOutcomeDto model)
        => $"""<div class="mhbo-wrap"><h1>Uitkomst {PupilLeerlingSsrParts.E(model.Title)}</h1><p class="mhbo-sub">{PupilLeerlingSsrParts.E(model.Summary)}</p><a class="mhbo-lnk" href="/leerling/mbo-hbo/overzicht">← Terug</a></div>""";

    public static string Paspoort(PupilMboHboPassportDto model)
    {
        var sb = new StringBuilder("""<div class="mhbo-wrap"><h1>Jouw Lobsy-paspoort</h1><div class="mhbo-grid">""");
        foreach (var sec in model.Sections)
        {
            sb.Append("<div class=\"mhbo-card\"><h2>").Append(PupilLeerlingSsrParts.E(sec.Title)).Append("</h2>");
            sb.Append(sec.BodyHtml).Append("</div>");
        }
        sb.Append("</div></div>");
        return sb.ToString();
    }
}
