using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilMboHboSsrHtml
{
    private static readonly IReadOnlyList<PupilMboHboBlock> BlockOrder = PupilMboHboCatalog.AllBlocks;

    public static string Uitleg(string antiforgery, string postPath, string? classLabel)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-wrap mhbo-wrap--welcome" data-testid="mhbo-welcome"><div class="mhbo-hero"><span class="mhbo-eyebrow">Welkom</span><h1>Ontdek wat bij je past</h1>""");
        if (!string.IsNullOrWhiteSpace(classLabel))
        {
            sb.Append("<p class=\"mhbo-sub\">").Append(PupilLeerlingSsrParts.E(classLabel)).Append("</p>");
        }

        sb.Append("<p class=\"mhbo-sub\">").Append(PupilLeerlingSsrParts.E(PupilSessionDuration.MboHbo)).Append("</p>");
        sb.Append("</div><div class=\"mhbo-grid mhbo-welcome-blocks\">");

        var n = 1;
        foreach (var block in BlockOrder)
        {
            var meta = PupilMboHboCopy.BlockMeta(block);
            var minutes = EstimatedMinutes(meta.QuestionCount);
            sb.Append("<div class=\"mhbo-card mhbo-welcome-block\"><span class=\"mhbo-part-no\">").Append(n.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span><h2>").Append(PupilLeerlingSsrParts.E(meta.Title)).Append("</h2>");
            sb.Append("<p>").Append(meta.QuestionCount.ToString(CultureInfo.InvariantCulture)).Append(" vragen · ± ");
            sb.Append(minutes.ToString(CultureInfo.InvariantCulture)).Append(" min</p></div>");
            n++;
        }

        sb.Append("</div><div class=\"mhbo-grid mhbo-welcome-hints\">");
        sb.Append("""<div class="mhbo-card mhbo-hint"><h3>Geen goed of fout</h3><p>Er is geen goed of fout antwoord. Het gaat om wat bij jou past.</p></div>""");
        sb.Append("""<div class="mhbo-card mhbo-hint"><h3>Pauzeren mag</h3><p>Je kunt altijd pauzeren en later verder. Je voortgang blijft bewaard.</p></div>""");
        sb.Append("""<div class="mhbo-card mhbo-hint"><h3>Daarna je paspoort</h3><p>Na de vijf onderdelen maak je je Lobsy-paspoort met jouw uitslagen.</p></div>""");
        sb.Append("</div><form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(postPath)).Append("\" class=\"mhbo-start-form\">");
        sb.Append(antiforgery);
        sb.Append("""<button type="submit" class="mhbo-btn mhbo-btn--gold" data-testid="mhbo-start-vakken">Start met vakkenpakket</button></form></div>""");
        return sb.ToString();
    }

    public static string Overzicht(PupilMboHboOverviewDto model)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-wrap mhbo-wrap--overview"><h1>Jouw onderdelen</h1><div class="mhbo-grid mhbo-overview-grid">""");
        foreach (var block in model.Blocks)
        {
            var tileClass = block.Status switch
            {
                PupilMboHboTileStatus.Completed => "mhbo-tile grn",
                PupilMboHboTileStatus.InProgress => "mhbo-tile yel",
                _ => "mhbo-tile grey",
            };
            sb.Append("<div class=\"").Append(tileClass).Append("\"><span class=\"st\">").Append(StatusLabel(block.Status)).Append("</span>");
            sb.Append("<h3>").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</h3>");
            sb.Append("<p>").Append(PupilLeerlingSsrParts.E(block.Description)).Append("</p>");
            var pct = block.QuestionCount > 0
                ? Math.Clamp(block.AnsweredCount * 100 / block.QuestionCount, 0, 100)
                : 0;
            sb.Append("<div class=\"mhbo-bar\"><i style=\"width:").Append(pct.ToString(CultureInfo.InvariantCulture)).Append("%\"></i></div>");
            sb.Append("<p class=\"mhbo-meta\">").Append(block.AnsweredCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(" / ").Append(block.QuestionCount.ToString(CultureInfo.InvariantCulture)).Append(" vragen</p>");
            sb.Append("<a class=\"mhbo-btn\" href=\"/leerling/mbo-hbo/test/").Append(PupilLeerlingSsrParts.E(block.Key)).Append("\">");
            sb.Append(block.Status == PupilMboHboTileStatus.NotStarted ? "Start" : "Verder").Append("</a></div>");
        }

        if (model.PassportUnlocked)
        {
            sb.Append("""<div class="mhbo-tile mhbo-tile--passport"><span class="st">Klaar</span><h3>Jouw Lobsy-paspoort</h3><p>Al je onderdelen zijn af. Bekijk je paspoort.</p><a class="mhbo-btn" href="/leerling/mbo-hbo/paspoort">Open je paspoort</a></div>""");
        }
        else if (model.CompletedBlocks > 0)
        {
            sb.Append("""<div class="mhbo-tile mhbo-tile--passport"><h3>Jouw Lobsy-paspoort</h3><p>Nog niet alles af. Je kunt alvast je voortgang bekijken.</p><a class="mhbo-btn mhbo-btn--ghost" href="/leerling/mbo-hbo/paspoort">Bekijk voortgang</a></div>""");
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    public static string TestQuestion(
        PupilMboHboQuestionDto q,
        string blockKey,
        IReadOnlyList<PupilMboHboBlockStatusDto> blocks,
        CultureState culture,
        string antiforgery,
        string postPath,
        string? error)
    {
        var blockMeta = PupilMboHboCopy.BlockMeta(q.Block);
        var pct = q.Total > 0 ? Math.Clamp(q.Index * 100 / q.Total, 0, 100) : 0;
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-layout" data-testid="mhbo-test-question"><aside class="mhbo-aside mhbo-aside--left" aria-label="Jouw onderdelen">""");
        sb.Append("<h2 class=\"mhbo-aside__title\">Jouw onderdelen</h2><ol class=\"mhbo-parts\">");
        var idx = 1;
        foreach (var block in blocks)
        {
            var active = string.Equals(block.Key, blockKey, StringComparison.OrdinalIgnoreCase);
            sb.Append("<li class=\"mhbo-part");
            if (active) sb.Append(" is-active");
            if (block.Status == PupilMboHboTileStatus.Completed) sb.Append(" is-done");
            sb.Append("\"><span class=\"mhbo-part__no\">").Append(idx.ToString(CultureInfo.InvariantCulture)).Append("</span>");
            sb.Append("<div class=\"mhbo-part__body\"><strong>").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</strong>");
            sb.Append("<span class=\"mhbo-part__st\">");
            if (active)
            {
                sb.Append("Vraag ").Append(q.Index.ToString(CultureInfo.InvariantCulture)).Append(" van ")
                    .Append(q.Total.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(PartSideStatus(block));
            }

            sb.Append("</span></div></li>");
            idx++;
        }

        sb.Append("</ol><div class=\"mhbo-card mhbo-tip\"><h3>Tip</h3><p>");
        sb.Append(PupilLeerlingSsrParts.E(blockMeta.Description)).Append("</p></div></aside>");
        sb.Append("<div class=\"mhbo-main\"><div class=\"mhbo-banner\" role=\"note\"><p>");
        sb.Append(PupilLeerlingSsrParts.E(blockMeta.Description)).Append("</p></div>");
        sb.Append("<article class=\"mhbo-qcard\"><header class=\"mhbo-qcard__head\"><span class=\"mhbo-qcard__block\">");
        sb.Append(PupilLeerlingSsrParts.E(blockMeta.Title)).Append("</span><span class=\"mhbo-qcard__prog\">Vraag ");
        sb.Append(q.Index.ToString(CultureInfo.InvariantCulture)).Append(" van ").Append(q.Total.ToString(CultureInfo.InvariantCulture));
        sb.Append("</span><div class=\"mhbo-bar mhbo-qcard__bar\"><i style=\"width:").Append(pct.ToString(CultureInfo.InvariantCulture)).Append("%\"></i></div></header>");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(postPath)).Append("\" data-mhbo-scale-form=\"1\" class=\"mhbo-qform\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"").Append(PupilLeerlingSsrParts.E(q.ItemId)).Append("\" />");
        sb.Append("<h1 id=\"mhbo-q-title\" class=\"mhbo-qcard__title\">").Append(PupilLeerlingSsrParts.E(q.Question)).Append("</h1>");
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
            sb.Append("<div class=\"mhbo-vak-row\"><span class=\"mhbo-vak-label\">Leuk</span>").Append(ScaleRow("Leuk", q.CurrentLeuk)).Append("</div>");
            sb.Append("<div class=\"mhbo-vak-row\"><span class=\"mhbo-vak-label\">Goed in</span>").Append(ScaleRow("Goed", q.CurrentGoed)).Append("</div>");
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

        var nextLabel = q.Index >= q.Total ? "Afronden" : "Volgende";
        sb.Append("<footer class=\"mhbo-pagefoot mhbo-pagefoot--in-form\" data-testid=\"mhbo-test-nav\">");
        sb.Append(PrevControl(blockKey, q, culture));
        sb.Append("""<a class="mhbo-btn mhbo-btn--ghost" href="/leerling/mbo-hbo/overzicht" data-testid="mhbo-pause">Pauzeren en naar overzicht</a>""");
        sb.Append("<button type=\"submit\" class=\"mhbo-btn mhbo-btn--gold\" data-testid=\"mhbo-next\" disabled>")
            .Append(nextLabel).Append("</button></footer>");
        sb.Append("</form></article></div>");
        sb.Append("<aside class=\"mhbo-aside mhbo-aside--right\" aria-label=\"Uitleg en voortgang\">");
        sb.Append(ScaleLegendCard(q.Scale));
        sb.Append(ProgressAsideCard(blocks));
        sb.Append("</aside></div>");

        return sb.ToString();
    }

    public static string Uitkomst(PupilMboHboOutcomeDto model, IReadOnlyList<PupilMboHboBlockStatusDto> blocks)
    {
        var next = NextBlock(model.Block);
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-wrap mhbo-wrap--transition" data-testid="mhbo-uitkomst"><div class="mhbo-card mhbo-transition-done"><h1>""");
        sb.Append(PupilLeerlingSsrParts.E(model.Title)).Append(" af</h1><p class=\"mhbo-sub\">");
        sb.Append(PupilLeerlingSsrParts.E(model.Summary)).Append("</p></div>");

        if (next is not null)
        {
            var meta = PupilMboHboCopy.BlockMeta(next.Value);
            sb.Append("<div class=\"mhbo-card mhbo-transition-next\"><h2>Volgende stap</h2><p><strong>");
            sb.Append(PupilLeerlingSsrParts.E(meta.Title)).Append("</strong> — ");
            sb.Append(PupilLeerlingSsrParts.E(meta.Description)).Append("</p>");
            sb.Append("<a class=\"mhbo-btn mhbo-btn--gold\" href=\"/leerling/mbo-hbo/test/");
            sb.Append(PupilLeerlingSsrParts.E(PupilMboHboProgressState.BlockKey(next.Value))).Append("\">Start ");
            sb.Append(PupilLeerlingSsrParts.E(meta.Title)).Append("</a></div>");
        }
        else
        {
            sb.Append("""<div class="mhbo-card mhbo-transition-next"><h2>Alle onderdelen af</h2><p>Bekijk je Lobsy-paspoort.</p><a class="mhbo-btn mhbo-btn--gold" href="/leerling/mbo-hbo/paspoort">Naar je paspoort</a></div>""");
        }

        sb.Append("<div class=\"mhbo-card\"><h2>Voortgang</h2><ul class=\"mhbo-status-list\">");
        foreach (var block in blocks)
        {
            sb.Append("<li><span>").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</span><em>");
            sb.Append(StatusLabel(block.Status)).Append("</em></li>");
        }

        sb.Append("</ul><a class=\"mhbo-lnk\" href=\"/leerling/mbo-hbo/overzicht\">Naar overzicht</a></div></div>");
        return sb.ToString();
    }

    public static string Paspoort(
        PupilMboHboPassportDto model,
        CultureState culture,
        string pdfHref,
        string? pdfNotice)
    {
        var sb = new StringBuilder("""<div class="mhbo-wrap mhbo-wrap--paspoort" data-testid="mhbo-paspoort"><header class="mhbo-paspoort-head"><h1>Jouw Lobsy-paspoort</h1>""");
        if (!string.IsNullOrWhiteSpace(model.SchoolName) || !string.IsNullOrWhiteSpace(model.ClassLabel))
        {
            sb.Append("<p class=\"mhbo-sub\">").Append(PupilLeerlingSsrParts.E(model.SchoolName));
            if (!string.IsNullOrWhiteSpace(model.ClassLabel))
            {
                sb.Append(" · ").Append(PupilLeerlingSsrParts.E(model.ClassLabel));
            }

            sb.Append("</p>");
        }

        sb.Append("</header>");

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
            sb.Append("<div class=\"mhbo-grid mhbo-paspoort-sections\">");
            foreach (var sec in model.Sections)
            {
                sb.Append("<div class=\"mhbo-card mhbo-pass-section\"><h2>").Append(PupilLeerlingSsrParts.E(sec.Title)).Append("</h2>");
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

    private static string ScaleLegendCard(PupilMboHboScaleKind kind)
    {
        var labels = kind switch
        {
            PupilMboHboScaleKind.Appeal => PupilMboHboScaleLabels.Appeal,
            PupilMboHboScaleKind.SubjectLeukGoed => null,
            _ => PupilMboHboScaleLabels.Agreement,
        };

        var sb = new StringBuilder("""<div class="mhbo-card mhbo-legend"><h2>Wat betekenen de cijfers?</h2>""");
        if (labels is null)
        {
            sb.Append("<p>1 = helemaal niet · 5 = heel erg. Geef bij elk vak twee scores: hoe leuk je het vindt en hoe goed je erin bent.</p>");
        }
        else
        {
            sb.Append("<ol class=\"mhbo-legend-list\">");
            for (var i = 0; i < labels.Length; i++)
            {
                sb.Append("<li><span>").Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("</span> ");
                sb.Append(PupilLeerlingSsrParts.E(labels[i])).Append("</li>");
            }

            sb.Append("</ol>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string ProgressAsideCard(IReadOnlyList<PupilMboHboBlockStatusDto> blocks)
    {
        var sb = new StringBuilder("""<div class="mhbo-card mhbo-progress-card"><h2>Voortgang</h2>""");
        foreach (var block in blocks)
        {
            var pct = block.QuestionCount > 0
                ? Math.Clamp(block.AnsweredCount * 100 / block.QuestionCount, 0, 100)
                : 0;
            sb.Append("<div class=\"mhbo-hb\"><span>").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</span>");
            sb.Append("<div class=\"mhbo-bar\"><i style=\"width:").Append(pct.ToString(CultureInfo.InvariantCulture)).Append("%\"></i></div>");
            sb.Append("<b>").Append(pct.ToString(CultureInfo.InvariantCulture)).Append("%</b></div>");
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

        return $"""<span class="mhbo-btn mhbo-btn--ghost is-disabled" aria-disabled="true" data-testid="mhbo-prev">{PupilGroep78SsrParts.BackSvg}{label}</span>""";
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
            sb.Append("\" data-mhbo-scale-field=\"").Append(field).Append("\" data-mhbo-scale-value=\"").Append(v).Append("\">");
            sb.Append(v.ToString(CultureInfo.InvariantCulture));
            sb.Append("</button>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string StatusLabel(PupilMboHboTileStatus status)
        => status switch
        {
            PupilMboHboTileStatus.Completed => "Afgerond",
            PupilMboHboTileStatus.InProgress => "Bezig",
            _ => "Nog te doen",
        };

    private static string PartSideStatus(PupilMboHboBlockStatusDto block)
    {
        if (block.Status == PupilMboHboTileStatus.Completed)
        {
            return "Afgerond";
        }

        if (block.Status == PupilMboHboTileStatus.InProgress && block.AnsweredCount > 0)
        {
            return $"Vraag {block.AnsweredCount + 1} van {block.QuestionCount}";
        }

        return "Nog te doen";
    }

    private static PupilMboHboBlock? NextBlock(PupilMboHboBlock completed)
    {
        var found = false;
        foreach (var block in BlockOrder)
        {
            if (found)
            {
                return block;
            }

            if (block == completed)
            {
                found = true;
            }
        }

        return null;
    }

    private static int EstimatedMinutes(int questionCount)
        => Math.Max(5, (int)Math.Round(questionCount * 0.75, MidpointRounding.AwayFromZero));
}
