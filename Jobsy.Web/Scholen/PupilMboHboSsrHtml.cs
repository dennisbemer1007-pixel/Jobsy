using System.Globalization;
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
            sb.Append("""<div class="mhbo-tile mhbo-tile--passport"><a class="mhbo-btn" href="/leerling/mbo-hbo/paspoort">Open je paspoort</a></div>""");
        }
        else if (model.CompletedBlocks > 0)
        {
            sb.Append("""<div class="mhbo-tile mhbo-tile--passport"><a class="mhbo-btn mhbo-btn--ghost" href="/leerling/mbo-hbo/paspoort">Bekijk voortgang</a></div>""");
        }
        sb.Append("</div></div>");
        return sb.ToString();
    }

    public static string TestQuestion(
        PupilMboHboQuestionDto q,
        string blockKey,
        CultureState culture,
        string antiforgery,
        string postPath,
        string? error)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"mhbo-wrap\" data-testid=\"mhbo-test-question\">");
        sb.Append("<p class=\"mhbo-meta\">Vraag ").Append(q.Index.ToString(CultureInfo.InvariantCulture));
        sb.Append(" van ").Append(q.Total.ToString(CultureInfo.InvariantCulture)).Append("</p>");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(postPath)).Append("\" data-mhbo-scale-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"").Append(PupilLeerlingSsrParts.E(q.ItemId)).Append("\" />");
        sb.Append("<h1 id=\"mhbo-q-title\">").Append(PupilLeerlingSsrParts.E(q.Question)).Append("</h1>");
        sb.Append(SpeakRow(q.Question, $"mhbo-q-{q.ItemId}", culture));
        if (!string.IsNullOrWhiteSpace(q.Scenario))
        {
            sb.Append("<p class=\"mhbo-scenario\">").Append(PupilLeerlingSsrParts.E(q.Scenario)).Append("</p>");
        }

        if (q.Scale == PupilMboHboScaleKind.SubjectLeukGoed)
        {
            sb.Append("<input type=\"hidden\" name=\"Leuk\" value=\"").Append(q.CurrentLeuk ?? 0).Append("\" />");
            sb.Append("<input type=\"hidden\" name=\"Goed\" value=\"").Append(q.CurrentGoed ?? 0).Append("\" />");
            sb.Append("<div class=\"mhbo-vak-scales\">");
            sb.Append("<div class=\"mhbo-vak-row\"><span>Leuk</span>").Append(ScaleRow("Leuk", q.CurrentLeuk)).Append("</div>");
            sb.Append("<div class=\"mhbo-vak-row\"><span>Goed in</span>").Append(ScaleRow("Goed", q.CurrentGoed)).Append("</div>");
            sb.Append("</div>");
        }
        else
        {
            sb.Append("<input type=\"hidden\" name=\"Value\" value=\"").Append(q.CurrentValue ?? 0).Append("\" />");
            sb.Append(ScaleRow("Value", q.CurrentValue));
        }

        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"mhbo-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
            sb.Append("<p><a class=\"mhbo-btn mhbo-btn--ghost\" href=\"");
            sb.Append(PupilLeerlingSsrParts.E(TestPath(blockKey, q.ItemId)));
            sb.Append("\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Retry"])).Append("</a></p>");
        }

        sb.Append("<div class=\"mhbo-qfoot\" data-testid=\"mhbo-test-nav\">");
        sb.Append(PrevControl(blockKey, q, culture));
        var nextLabel = q.Index >= q.Total ? "Afronden →" : "Volgende →";
        sb.Append("<button type=\"submit\" class=\"mhbo-btn\" data-testid=\"mhbo-next\" disabled>").Append(nextLabel).Append("</button></div></form></div>");
        return sb.ToString();
    }

    public static string Uitkomst(PupilMboHboOutcomeDto model)
        => $"""<div class="mhbo-wrap"><h1>Uitkomst {PupilLeerlingSsrParts.E(model.Title)}</h1><p class="mhbo-sub">{PupilLeerlingSsrParts.E(model.Summary)}</p><a class="mhbo-lnk" href="/leerling/mbo-hbo/overzicht">← Terug</a></div>""";

    public static string Paspoort(
        PupilMboHboPassportDto model,
        CultureState culture,
        string pdfHref,
        string? pdfNotice)
    {
        var sb = new StringBuilder("""<div class="mhbo-wrap" data-testid="mhbo-paspoort"><h1>Jouw Lobsy-paspoort</h1>""");
        if (!string.IsNullOrEmpty(pdfNotice))
        {
            sb.Append("<p class=\"mhbo-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(pdfNotice)).Append("</p>");
        }

        if (!model.Unlocked && !string.IsNullOrEmpty(model.IncompleteHint))
        {
            sb.Append("<p class=\"mhbo-sub\" data-testid=\"mhbo-paspoort-incomplete\" role=\"status\">");
            sb.Append(PupilLeerlingSsrParts.E(model.IncompleteHint)).Append("</p>");
        }

        if (model.MissingBlockTitles is { Count: > 0 })
        {
            sb.Append("<p class=\"mhbo-meta\" data-testid=\"mhbo-paspoort-missing\">Nog niet af: ");
            sb.Append(PupilLeerlingSsrParts.E(string.Join(" · ", model.MissingBlockTitles))).Append("</p>");
            sb.Append("<p><a class=\"mhbo-btn mhbo-btn--ghost\" href=\"/leerling/mbo-hbo/overzicht\">Ga verder met je testen</a></p>");
        }

        sb.Append("""<nav class="mhbo-paspoort-actions" data-testid="mhbo-paspoort-actions">""");
        sb.Append("<a class=\"mhbo-btn mhbo-btn--ghost\" href=\"/leerling/mbo-hbo/overzicht\">Terug naar overzicht</a>");
        if (model.Unlocked)
        {
            sb.Append("<a class=\"mhbo-btn\" href=\"").Append(PupilLeerlingSsrParts.E(pdfHref)).Append("\" data-enhance-nav=\"false\" data-testid=\"mhbo-download-pdf\">");
            sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Pdf"])).Append("</a>");
        }

        sb.Append("</nav>");
        if (model.Sections.Count == 0)
        {
            sb.Append("<div class=\"mhbo-card\"><p>Je hebt nog geen test afgerond. Start bij je overzicht.</p></div>");
        }
        else
        {
            sb.Append("<div class=\"mhbo-grid\">");
            foreach (var sec in model.Sections)
            {
                sb.Append("<div class=\"mhbo-card\"><h2>").Append(PupilLeerlingSsrParts.E(sec.Title)).Append("</h2>");
                sb.Append(sec.BodyHtml).Append("</div>");
            }

            sb.Append("</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.FooterNote))
        {
            sb.Append("<p class=\"mhbo-foot\">").Append(PupilLeerlingSsrParts.E(model.FooterNote)).Append("</p>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string SpeakRow(string text, string speechKey, CultureState culture)
        => $"""
            <div class="mhbo-speak-row" data-testid="mhbo-speak-row">{PupilGroep78SsrParts.Groep78SpeakButton(text, speechKey, culture)}<span class="speak-label">{PupilLeerlingSsrParts.E(culture["Leerling.G78.ReadQuestion"])}</span></div>
            """;

    private static string PrevControl(string blockKey, PupilMboHboQuestionDto q, CultureState culture)
    {
        var label = PupilLeerlingSsrParts.E(culture["Leerling.Reis.Prev"]);
        if (!string.IsNullOrEmpty(q.PreviousItemId))
        {
            var href = TestPath(blockKey, q.PreviousItemId);
            return $"""<a class="mhbo-btn mhbo-btn--ghost" href="{PupilLeerlingSsrParts.E(href)}" data-testid="mhbo-prev">{PupilGroep78SsrParts.BackSvg}{label}</a>""";
        }

        return $"""<button type="button" class="mhbo-btn mhbo-btn--ghost" disabled data-testid="mhbo-prev">{PupilGroep78SsrParts.BackSvg}{label}</button>""";
    }

    private static string TestPath(string blockKey, string? itemId)
    {
        var path = $"/leerling/mbo-hbo/test/{Uri.EscapeDataString(blockKey)}";
        return string.IsNullOrEmpty(itemId)
            ? path
            : $"{path}?itemId={Uri.EscapeDataString(itemId)}";
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
}
