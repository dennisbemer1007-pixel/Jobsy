using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Scholen;

public static class PupilMboHboSsrHtml
{
    private static readonly IReadOnlyList<PupilMboHboBlock> BlockOrder = PupilMboHboCatalog.AllBlocks;

    public static IReadOnlyList<PupilMboHboBlockStatusDto> DefaultWelcomeBlocks()
    {
        var list = new List<PupilMboHboBlockStatusDto>();
        foreach (var block in BlockOrder)
        {
            var meta = PupilMboHboCopy.BlockMeta(block);
            list.Add(new PupilMboHboBlockStatusDto(
                block,
                PupilMboHboProgressState.BlockKey(block),
                meta.Title,
                meta.Description,
                meta.QuestionCount,
                0,
                PupilMboHboTileStatus.NotStarted));
        }

        return list;
    }

    public static string Uitleg(string antiforgery, string postPath, IReadOnlyList<PupilMboHboBlockStatusDto> blocks)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-layout mhbo-layout--welcome" data-testid="mhbo-welcome">""");
        sb.Append(RenderLeftAside(blocks, PupilMboHboBlock.Vakken, welcomeMode: true));
        sb.Append("""<div class="mhbo-main mhbo-main--welcome">""");
        sb.Append("""<div class="mhbo-hero"><span class="mhbo-eyebrow">Welkom</span><h1>Ontdek wat bij je past</h1>""");
        sb.Append("<p class=\"mhbo-sub mhbo-intro\">Met Lobsy kijk je stap voor stap naar je interesses, sterke kanten en wat je belangrijk vindt. Er zijn geen goede of foute antwoorden.</p>");
        sb.Append("</div><div class=\"mhbo-grid mhbo-welcome-blocks\">");

        var n = 1;
        foreach (var block in BlockOrder)
        {
            var meta = PupilMboHboCopy.BlockMeta(block);
            var minutes = PupilMboHboMockupCopy.EstimatedMinutes(meta.QuestionCount);
            var isFirst = block == PupilMboHboBlock.Vakken;
            sb.Append("<div class=\"mhbo-card mhbo-welcome-block");
            if (isFirst) sb.Append(" is-featured");
            sb.Append("\"><span class=\"mhbo-part-no\">").Append(n.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span><h2>").Append(PupilLeerlingSsrParts.E(meta.Title)).Append("</h2>");
            sb.Append("<p class=\"mhbo-welcome-block__desc\">").Append(PupilLeerlingSsrParts.E(meta.Description)).Append("</p>");
            sb.Append("<p class=\"mhbo-meta\">").Append(meta.QuestionCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(" vragen · ± ").Append(minutes.ToString(CultureInfo.InvariantCulture)).Append(" min</p></div>");
            n++;
        }

        sb.Append("</div><div class=\"mhbo-grid mhbo-welcome-hints\">");
        sb.Append("""<div class="mhbo-card mhbo-hint"><h3>Geen goed of fout</h3><p>Kies steeds wat het beste bij je past. Er is geen goed of fout antwoord.</p></div>""");
        sb.Append("""<div class="mhbo-card mhbo-hint"><h3>Pauzeren mag</h3><p>Stop wanneer je wilt. Je voortgang blijft bewaard en je gaat later verder.</p></div>""");
        sb.Append("""<div class="mhbo-card mhbo-hint"><h3>Daarna je paspoort</h3><p>Als alle vijf onderdelen af zijn, maak je je Lobsy-paspoort met jouw uitslagen.</p></div>""");
        sb.Append("</div>");
        sb.Append("<footer class=\"mhbo-welcome-foot\">");
        sb.Append("<p class=\"mhbo-welcome-foot__note\">Je doet mee met alleen je code. We vragen geen naam of andere gegevens.</p>");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(postPath)).Append("\" class=\"mhbo-start-form\">");
        sb.Append(antiforgery);
        sb.Append("""<button type="submit" class="mhbo-btn" data-testid="mhbo-start-vakken">Start met vakkenpakket →</button></form></footer></div></div>""");
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
        sb.Append("""<div class="mhbo-layout mhbo-layout--question" data-testid="mhbo-test-question">""");
        sb.Append(RenderMobileProgress(blockMeta.Title, q.Index, q.Total, blocks));
        sb.Append(RenderLeftAside(blocks, q.Block, welcomeMode: false, questionIndex: q.Index, questionTotal: q.Total));
        sb.Append("<div class=\"mhbo-main\"><div class=\"mhbo-banner\" role=\"note\"><span class=\"mhbo-banner__icon\" aria-hidden=\"true\">i</span><p>");
        if (q.Scale == PupilMboHboScaleKind.SubjectLeukGoed)
        {
            sb.Append("Bij elk vak geef je twee dingen aan: hoe leuk je het vindt, en hoe goed je erin bent. De vakjes zijn steeds even groot. 1 is weinig, 5 is veel.");
        }
        else
        {
            sb.Append(PupilLeerlingSsrParts.E(blockMeta.Description));
        }

        sb.Append("</p></div>");
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
            sb.Append("<div class=\"mhbo-vak-row\">").Append(VakScaleRow("Leuk", "Hoe leuk lijkt dit vak je?", "Niet leuk", "Heel leuk", q.CurrentLeuk)).Append("</div>");
            sb.Append("<div class=\"mhbo-vak-row\">").Append(VakScaleRow("Goed", "Hoe goed ben je hierin, denk je?", "Nog weinig", "Hier ben ik goed in", q.CurrentGoed)).Append("</div>");
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

        var nextLabel = q.Index >= q.Total ? "Afronden →" : "Volgende →";
        sb.Append("<footer class=\"mhbo-pagefoot mhbo-pagefoot--in-form\" data-testid=\"mhbo-test-nav\">");
        sb.Append(PrevControl(blockKey, q, culture));
        sb.Append("""<a class="mhbo-btn mhbo-btn--ghost mhbo-btn--pause" href="/leerling/mbo-hbo/overzicht" data-testid="mhbo-pause">Pauzeren en naar overzicht</a>""");
        sb.Append("<button type=\"submit\" class=\"mhbo-btn mhbo-btn--next\" data-testid=\"mhbo-next\" disabled>")
            .Append(nextLabel).Append("</button>");
        sb.Append(MobileFootCoachChip()).Append("</footer>");
        sb.Append("</form></article></div>");
        sb.Append("<aside class=\"mhbo-aside mhbo-aside--right\" aria-label=\"Uitleg en voortgang\">");
        sb.Append(ScaleLegendCard(q.Scale, q.CurrentLeuk, q.CurrentGoed));
        sb.Append(ProgressAsideCard(blocks));
        sb.Append("</aside></div>");

        return sb.ToString();
    }

    public static string Uitkomst(PupilMboHboOutcomeDto model, IReadOnlyList<PupilMboHboBlockStatusDto> blocks)
    {
        var next = NextBlock(model.Block);
        var completedCount = blocks.Count(BlockDisplayComplete);
        var finishedRow = blocks.FirstOrDefault(b => b.Block == model.Block);
        var answeredDisplay = finishedRow is not null
            ? DisplayAnsweredCount(finishedRow)
            : Math.Max(model.AnsweredCount, model.QuestionCount);
        var partIndex = BlockOrder.ToList().FindIndex(b => b == model.Block) + 1;
        var sb = new StringBuilder();
        sb.Append("""<div class="mhbo-layout mhbo-layout--transition" data-testid="mhbo-uitkomst">""");
        sb.Append(RenderTransitionAside(blocks, model.Block, next, completedCount));
        sb.Append("<div class=\"mhbo-main\">");
        sb.Append(RenderTransitionMobileProgress(blocks, completedCount));
        sb.Append("<div class=\"mhbo-transition-hero\">");
        sb.Append("""<span class="mhbo-transition-check" aria-hidden="true">✓</span><div class="mhbo-transition-hero__copy">""");
        sb.Append("<p class=\"mhbo-eyebrow\">Onderdeel ").Append(partIndex.ToString(CultureInfo.InvariantCulture)).Append(" van 5</p>");
        sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(PupilMboHboMockupCopy.ShortBlockTitle(model.Block))).Append(" is af</h1>");
        sb.Append("<p class=\"mhbo-sub\">Je hebt ").Append(answeredDisplay.ToString(CultureInfo.InvariantCulture));
        sb.Append(" vragen over ").Append(PupilLeerlingSsrParts.E(TransitionTopicSlug(model.Block)));
        sb.Append(" ingevuld. Je antwoorden zijn bewaard. Je kunt later nog iets aanpassen.</p></div></div>");

        if (next is not null)
        {
            var meta = PupilMboHboCopy.BlockMeta(next.Value);
            var mins = PupilMboHboMockupCopy.EstimatedMinutes(meta.QuestionCount);
            var shortTitle = PupilMboHboMockupCopy.ShortBlockTitle(next.Value);
            var nextKey = PupilMboHboProgressState.BlockKey(next.Value);
            sb.Append("<h2 class=\"mhbo-transition-volgende\">Volgende</h2>");
            sb.Append("<div class=\"mhbo-card mhbo-transition-next-card\"><div class=\"mhbo-transition-next-card__body\"><span class=\"mhbo-eyebrow\">Volgende</span><h2>");
            sb.Append(PupilLeerlingSsrParts.E(shortTitle)).Append("</h2><p class=\"mhbo-sub\">");
            sb.Append(PupilLeerlingSsrParts.E(meta.Description)).Append(' ');
            sb.Append(meta.QuestionCount.ToString(CultureInfo.InvariantCulture)).Append(" vragen, ongeveer ");
            sb.Append(mins.ToString(CultureInfo.InvariantCulture)).Append(" minuten. Er is geen goed of fout.</p></div>");
            sb.Append("<a class=\"mhbo-btn mhbo-btn--next-card\" href=\"/leerling/mbo-hbo/test/");
            sb.Append(PupilLeerlingSsrParts.E(nextKey)).Append("\">Start ");
            sb.Append(PupilLeerlingSsrParts.E(shortTitle)).Append(" →</a></div>");
        }

        sb.Append("""<div class="mhbo-transition-tiles" aria-label="Status per onderdeel">""");
        foreach (var block in blocks)
        {
            var tile = TransitionTileState(block, next);
            var shortTitle = PupilMboHboMockupCopy.ShortBlockTitle(block.Block);
            sb.Append("<div class=\"mhbo-transition-tile ").Append(tile.Css).Append("\"><span class=\"mhbo-transition-tile__tag\">");
            sb.Append(PupilLeerlingSsrParts.E(tile.Tag)).Append("</span><h3>").Append(PupilLeerlingSsrParts.E(shortTitle)).Append("</h3><p>");
            sb.Append(PupilLeerlingSsrParts.E(tile.Body)).Append("</p></div>");
        }

        sb.Append("</div>");
        var remaining = Math.Max(0, 5 - completedCount);
        if (remaining > 0)
        {
            sb.Append("<p class=\"mhbo-transition-meta\">Nog ").Append(remaining.ToString(CultureInfo.InvariantCulture));
            sb.Append(" onderdelen. Als die ook af zijn, zie je je Lobsy-paspoort met een overzicht van je uitkomsten.</p>");
        }

        sb.Append("<footer class=\"mhbo-transition-foot mhbo-pagefoot--in-form\">");
        if (next is not null)
        {
            var shortTitle = PupilMboHboMockupCopy.ShortBlockTitle(next.Value);
            sb.Append("<a class=\"mhbo-btn mhbo-transition-start\" href=\"/leerling/mbo-hbo/test/");
            sb.Append(PupilLeerlingSsrParts.E(PupilMboHboProgressState.BlockKey(next.Value))).Append("\"><span class=\"mhbo-btn__label-full\">Start ");
            sb.Append(PupilLeerlingSsrParts.E(shortTitle)).Append(" →</span><span class=\"mhbo-btn__label-short\">Start →</span></a>");
        }
        else
        {
            sb.Append("""<a class="mhbo-btn mhbo-transition-start" href="/leerling/mbo-hbo/paspoort"><span class="mhbo-btn__label-full">Naar je paspoort →</span><span class="mhbo-btn__label-short">Start →</span></a>""");
        }

        sb.Append("""<a class="mhbo-btn mhbo-btn--ghost mhbo-transition-back" href="/leerling/mbo-hbo/overzicht">Terug naar overzicht</a>""");

        sb.Append(MobileFootCoachChip()).Append("</footer></div></div>");
        return sb.ToString();
    }

    public static string Paspoort(
        PupilMboHboPassportDto model,
        CultureState culture,
        string pdfHref,
        string? pdfNotice,
        DateOnly? generatedOn = null)
    {
        generatedOn ??= DateOnly.FromDateTime(DateTime.Now);
        var sb = new StringBuilder("""<div class="mhbo-wrap mhbo-wrap--paspoort" data-testid="mhbo-paspoort">""");
        if (!model.Unlocked && !string.IsNullOrEmpty(model.IncompleteHint))
        {
            sb.Append("<div class=\"mhbo-banner\" data-testid=\"mhbo-paspoort-incomplete\" role=\"status\"><span class=\"mhbo-banner__icon\" aria-hidden=\"true\">i</span><p>");
            sb.Append(PupilLeerlingSsrParts.E(model.IncompleteHint)).Append("</p></div>");
        }
        else if (!string.IsNullOrEmpty(pdfNotice))
        {
            sb.Append("<div class=\"mhbo-banner\" role=\"alert\"><span class=\"mhbo-banner__icon\" aria-hidden=\"true\">!</span><p>");
            sb.Append(PupilLeerlingSsrParts.E(pdfNotice)).Append("</p></div>");
        }

        sb.Append("<header class=\"mhbo-paspoort-head\">");
        if (model.Unlocked)
        {
            sb.Append("<p class=\"mhbo-eyebrow\">Alle onderdelen af</p>");
        }

        sb.Append("<div class=\"mhbo-paspoort-head__row\"><div><h1>Jouw Lobsy-paspoort</h1>");
        sb.Append("<p class=\"mhbo-sub mhbo-paspoort-meta\">");
        if (!string.IsNullOrWhiteSpace(model.SchoolName))
        {
            sb.Append(PupilLeerlingSsrParts.E(model.SchoolName)).Append(" · ");
        }

        if (!string.IsNullOrWhiteSpace(model.ClassLabel))
        {
            sb.Append(PupilLeerlingSsrParts.E(model.ClassLabel)).Append(" · ");
        }

        sb.Append(generatedOn.Value.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("nl-NL")));
        sb.Append("</p></div>");
        if (model.Unlocked)
        {
            sb.Append("<a class=\"mhbo-btn mhbo-btn--head\" href=\"").Append(PupilLeerlingSsrParts.E(pdfHref));
            sb.Append("\" data-enhance-nav=\"false\" data-testid=\"mhbo-download-pdf\">");
            sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Pdf"])).Append("</a>");
        }

        sb.Append("</div></header>");
        sb.Append("""<div class="mhbo-grid mhbo-paspoort-sections">""");
        foreach (var block in PupilMboHboMockupCopy.PassportCardOrder)
        {
            var meta = PupilMboHboCopy.BlockMeta(block);
            var cardTitle = PupilMboHboMockupCopy.PassportCardTitle(block);
            var sec = model.Sections.FirstOrDefault(s => string.Equals(s.Title, meta.Title, StringComparison.Ordinal));
            sb.Append("<div class=\"mhbo-card mhbo-pass-section\">");
            sb.Append("<h2>").Append(PupilLeerlingSsrParts.E(cardTitle)).Append("</h2>");
            if (sec is not null)
            {
                sb.Append(sec.BodyHtml);
            }
            else if (model.MissingBlockTitles is not null && model.MissingBlockTitles.Contains(meta.Title))
            {
                sb.Append(PupilMboHboPassportSectionHtml.TodoPlaceholder(block));
            }
            else
            {
                sb.Append(PupilMboHboPassportSectionHtml.TodoPlaceholder(block));
            }

            sb.Append("</div>");
        }

        sb.Append("<div class=\"mhbo-card mhbo-pass-section mhbo-pass-section--step\"><h2>");
        sb.Append(PupilLeerlingSsrParts.E(PupilMboHboMockupCopy.PassportVolgendeStapTitle)).Append("</h2>");
        sb.Append(PupilMboHboPassportSectionHtml.VolgendeStapCard()).Append("</div>");
        sb.Append("</div>");
        if (model.MissingBlockTitles is { Count: > 0 })
        {
            sb.Append("<p class=\"mhbo-meta\" data-testid=\"mhbo-paspoort-missing\" hidden>Nog niet af: ");
            sb.Append(PupilLeerlingSsrParts.E(string.Join(" · ", model.MissingBlockTitles))).Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(model.FooterNote))
        {
            sb.Append("<p class=\"mhbo-foot mhbo-paspoort-disclaimer\">").Append(PupilLeerlingSsrParts.E(model.FooterNote)).Append("</p>");
        }

        sb.Append("""<footer class="mhbo-paspoort-foot mhbo-pagefoot--in-form"><a class="mhbo-btn mhbo-btn--ghost" href="/leerling/mbo-hbo/overzicht">Terug naar overzicht</a>""");
        if (model.Unlocked)
        {
            sb.Append("<a class=\"mhbo-btn mhbo-btn--next\" href=\"").Append(PupilLeerlingSsrParts.E(pdfHref));
            sb.Append("\" data-enhance-nav=\"false\">").Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Pdf"])).Append("</a>");
        }
        else
        {
            sb.Append("""<a class="mhbo-btn mhbo-btn--next" href="/leerling/mbo-hbo/overzicht">Verder met testen →</a>""");
        }

        sb.Append(MobileFootCoachChip()).Append("</footer>");
        sb.Append("""<nav class="mhbo-paspoort-actions" data-testid="mhbo-paspoort-actions" hidden></nav>""");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderLeftAside(
        IReadOnlyList<PupilMboHboBlockStatusDto> blocks,
        PupilMboHboBlock activeBlock,
        bool welcomeMode,
        int? questionIndex = null,
        int? questionTotal = null)
    {
        var sb = new StringBuilder("""<aside class="mhbo-aside mhbo-aside--left" aria-label="Jouw onderdelen">""");
        sb.Append("<h2 class=\"mhbo-aside__title\">Jouw onderdelen</h2><ol class=\"mhbo-parts\">");
        var idx = 1;
        foreach (var block in blocks)
        {
            var active = block.Block == activeBlock;
            sb.Append("<li class=\"mhbo-part");
            if (active) sb.Append(" is-active");
            if (BlockDisplayComplete(block)) sb.Append(" is-done");
            sb.Append("\"><span class=\"mhbo-part__no\">").Append(idx.ToString(CultureInfo.InvariantCulture)).Append("</span>");
            sb.Append("<div class=\"mhbo-part__body\"><strong>").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</strong>");
            sb.Append("<span class=\"mhbo-part__st\">");
            if (welcomeMode && active)
            {
                sb.Append("Eerst dit · ").Append(block.QuestionCount.ToString(CultureInfo.InvariantCulture)).Append(" vragen");
            }
            else if (active && questionIndex is not null && questionTotal is not null)
            {
                sb.Append("Vraag ").Append(questionIndex.Value.ToString(CultureInfo.InvariantCulture)).Append(" van ")
                    .Append(questionTotal.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(PartSideStatus(block));
            }

            sb.Append("</span></div></li>");
            idx++;
        }

        sb.Append("</ol>");
        if (welcomeMode)
        {
            sb.Append("""<div class="mhbo-card mhbo-pause-card"><h3>Je mag pauzeren</h3><p>Je antwoorden worden bewaard. Je gaat later verder waar je was.</p></div>""");
        }
        else
        {
            sb.Append("<div class=\"mhbo-card mhbo-tip\"><h3>Tip</h3><p>1 is weinig, 5 is veel. Kies wat het beste bij je past.</p></div>");
        }

        sb.Append("</aside>");
        return sb.ToString();
    }

    private static string RenderMobileProgress(
        string blockTitle,
        int index,
        int total,
        IReadOnlyList<PupilMboHboBlockStatusDto> blocks)
    {
        var sb = new StringBuilder("""<div class="mhbo-mobile-progress" aria-hidden="true"><div class="mhbo-mobile-progress__segments">""");
        var i = 0;
        foreach (var _ in blocks)
        {
            sb.Append("<span class=\"mhbo-mobile-progress__seg");
            if (i == 0) sb.Append(" is-on");
            sb.Append("\"></span>");
            i++;
        }

        sb.Append("</div><p class=\"mhbo-mobile-progress__label\">");
        sb.Append(PupilLeerlingSsrParts.E(blockTitle)).Append(" · ").Append(index.ToString(CultureInfo.InvariantCulture));
        sb.Append(" / ").Append(total.ToString(CultureInfo.InvariantCulture)).Append("</p></div>");
        return sb.ToString();
    }

    private static string ScaleLegendCard(PupilMboHboScaleKind kind, int? currentLeuk, int? currentGoed)
    {
        var labels = kind switch
        {
            PupilMboHboScaleKind.Appeal => PupilMboHboScaleLabels.Appeal,
            PupilMboHboScaleKind.SubjectLeukGoed => PupilMboHboScaleLabels.SubjectVakFit,
            _ => PupilMboHboScaleLabels.Agreement,
        };

        var highlight = kind == PupilMboHboScaleKind.SubjectLeukGoed
            ? currentGoed ?? currentLeuk
            : null;

        var sb = new StringBuilder("""<div class="mhbo-card mhbo-legend"><h2>Wat betekenen de cijfers?</h2><ol class="mhbo-legend-boxes">""");
        for (var i = 0; i < labels.Length; i++)
        {
            var n = i + 1;
            sb.Append("<li");
            if (highlight == n) sb.Append(" class=\"is-active\"");
            sb.Append("><span class=\"mhbo-legend-box");
            if (highlight == n) sb.Append(" is-on");
            sb.Append("\">").Append(n.ToString(CultureInfo.InvariantCulture)).Append("</span> ");
            sb.Append(PupilLeerlingSsrParts.E(labels[i])).Append("</li>");
        }

        sb.Append("</ol></div>");
        return sb.ToString();
    }

    private static string MobileFootCoachChip()
        => $"""<span class="mhbo-foot-coach" aria-hidden="true"><img src="{BrandImages.AbsoluteCoachB}" alt="" width="48" height="72" decoding="async" /></span>""";

    private static string ProgressAsideCard(IReadOnlyList<PupilMboHboBlockStatusDto> blocks)
    {
        var sb = new StringBuilder("""<div class="mhbo-card mhbo-progress-card"><h2>Voortgang</h2><ul class="mhbo-progress-badges">""");
        foreach (var block in blocks)
        {
            sb.Append("<li><span class=\"mhbo-progress-badges__title\">").Append(PupilLeerlingSsrParts.E(block.Title)).Append("</span>");
            sb.Append("<span class=\"mhbo-badge\">").Append(ProgressBadge(block)).Append("</span></li>");
        }

        sb.Append("</ul></div>");
        return sb.ToString();
    }

    private static string ProgressBadge(PupilMboHboBlockStatusDto block)
    {
        if (BlockDisplayComplete(block))
        {
            return $"{block.QuestionCount} / {block.QuestionCount}";
        }

        if (block.AnsweredCount > 0)
        {
            return $"{block.AnsweredCount} / {block.QuestionCount}";
        }

        return "Nog te doen";
    }

    private static bool BlockDisplayComplete(PupilMboHboBlockStatusDto block)
        => block.Status == PupilMboHboTileStatus.Completed;

    private static int DisplayAnsweredCount(PupilMboHboBlockStatusDto block)
    {
        if (block.Status == PupilMboHboTileStatus.Completed && block.QuestionCount > 0)
        {
            return Math.Max(block.AnsweredCount, block.QuestionCount);
        }

        return block.AnsweredCount;
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
        if (BlockDisplayComplete(block))
        {
            return "Afgerond";
        }

        if (block.AnsweredCount > 0)
        {
            return $"{block.AnsweredCount} / {block.QuestionCount} vragen";
        }

        if (block.Status == PupilMboHboTileStatus.InProgress)
        {
            return "Bezig";
        }

        return $"{block.QuestionCount} vragen";
    }

    private static string VakScaleRow(string field, string hint, string low, string high, int? selected)
    {
        var sb = new StringBuilder("<div class=\"mhbo-vak-scale-head\"><span class=\"mhbo-vak-label\">");
        sb.Append(field == "Goed" ? "Goed in" : "Leuk").Append("</span><span class=\"mhbo-vak-hint\">");
        sb.Append(PupilLeerlingSsrParts.E(hint)).Append("</span></div>");
        sb.Append(ScaleRow(field, selected));
        sb.Append("<div class=\"mhbo-scale-ends\"><span>").Append(PupilLeerlingSsrParts.E(low));
        sb.Append("</span><span>").Append(PupilLeerlingSsrParts.E(high)).Append("</span></div>");
        return sb.ToString();
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

    private static string TransitionTopicSlug(PupilMboHboBlock block)
        => block == PupilMboHboBlock.Vakken ? "vakken" : PupilMboHboMockupCopy.ShortBlockTitle(block).ToLowerInvariant();

    private sealed record TransitionTile(string Tag, string Css, string Body);

    private static TransitionTile TransitionTileState(
        PupilMboHboBlockStatusDto block,
        PupilMboHboBlock? next)
    {
        var blurb = PupilMboHboMockupCopy.TransitionStatusBlurb(block.Block);
        if (BlockDisplayComplete(block))
        {
            var answered = DisplayAnsweredCount(block);
            var saved = $"{answered} van {block.QuestionCount} vragen zijn bewaard. Je kunt later nog iets aanpassen.";
            return new TransitionTile("Af", "is-af", saved);
        }

        if (next is not null && block.Block == next)
        {
            return new TransitionTile("Nu jij", "is-now", blurb);
        }

        return new TransitionTile("Later", "is-later", blurb);
    }

    private static string RenderTransitionAside(
        IReadOnlyList<PupilMboHboBlockStatusDto> blocks,
        PupilMboHboBlock completedBlock,
        PupilMboHboBlock? nextBlock,
        int completedCount)
    {
        _ = completedBlock;
        var sb = new StringBuilder("""<aside class="mhbo-aside mhbo-aside--left" aria-label="Jouw onderdelen">""");
        sb.Append("<h2 class=\"mhbo-aside__title\">Jouw onderdelen</h2><ol class=\"mhbo-parts\">");
        var idx = 1;
        foreach (var block in blocks)
        {
            var done = BlockDisplayComplete(block);
            var isNext = nextBlock is not null && block.Block == nextBlock;
            sb.Append("<li class=\"mhbo-part");
            if (isNext) sb.Append(" is-active");
            if (done) sb.Append(" is-done");
            sb.Append("\"><span class=\"mhbo-part__no\" aria-hidden=\"true\">");
            if (done) sb.Append('\u2713');
            else sb.Append(idx.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span><div class=\"mhbo-part__body\"><strong>").Append(PupilLeerlingSsrParts.E(PupilMboHboMockupCopy.ShortBlockTitle(block.Block))).Append("</strong>");
            sb.Append("<span class=\"mhbo-part__st\">");
            if (done)
            {
                var answered = DisplayAnsweredCount(block);
                sb.Append("Af · ").Append(answered.ToString(CultureInfo.InvariantCulture)).Append(" van ").Append(block.QuestionCount.ToString(CultureInfo.InvariantCulture));
            }
            else if (isNext)
            {
                sb.Append("Nu jij · ").Append(block.QuestionCount.ToString(CultureInfo.InvariantCulture)).Append(" vragen");
            }
            else
            {
                sb.Append("Nog te doen");
            }

            sb.Append("</span></div></li>");
            idx++;
        }

        sb.Append("</ol><div class=\"mhbo-card mhbo-pause-card\"><h3>").Append(completedCount.ToString(CultureInfo.InvariantCulture)).Append(" van 5 is af</h3>");
        sb.Append("<p>Je antwoorden zijn bewaard. Je kunt later nog iets aanpassen.</p></div></aside>");
        return sb.ToString();
    }

    private static string RenderTransitionMobileProgress(IReadOnlyList<PupilMboHboBlockStatusDto> blocks, int completedCount)
    {
        var nextOpen = blocks.FirstOrDefault(b => !BlockDisplayComplete(b))?.Block;
        var sb = new StringBuilder("""<div class="mhbo-mobile-progress mhbo-mobile-progress--transition" aria-hidden="true"><div class="mhbo-mobile-progress__segments">""");
        foreach (var block in blocks)
        {
            sb.Append("<span class=\"mhbo-mobile-progress__seg");
            if (BlockDisplayComplete(block)) sb.Append(" is-done");
            else if (nextOpen == block.Block) sb.Append(" is-on");
            sb.Append("\"></span>");
        }

        sb.Append("</div><p class=\"mhbo-mobile-progress__label\">").Append(completedCount.ToString(CultureInfo.InvariantCulture)).Append(" van 5 af</p></div>");
        return sb.ToString();
    }
}
