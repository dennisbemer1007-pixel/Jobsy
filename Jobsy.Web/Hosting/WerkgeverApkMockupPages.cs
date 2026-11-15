using System.Net;
using System.Text;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

/// <summary>Fill static mockup HTML (templates 02–07) with live SSR data.</summary>
public static class WerkgeverApkMockupPages
{
    private const string AnswerFormId = "wa-answer-form";

    public static string TestQuestion(WerkgeverApkEmployeeQuestionDto q, string antiforgery, string postPath, string? error)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate(
            "02-vraag.html",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["codeDisplay"] = "",
                ["company"] = "",
                ["team"] = "",
            });
        html = InjectCoachButton(html);
        var blockTitle = BlockTitle(q.Block);
        var progress = $"Vraag {q.Index} van {q.Total}";
        var mob = $"{blockTitle} · {q.Index} / {q.Total}";
        var sideNow = $"{q.Index} / {q.Total}";

        html = ApplyBlockRail(html, q.Block, progress, mob, sideNow, blockTitle);
        html = ReplaceBetweenMarkers(
            html,
            "<section class=\"card gold-top qcard\">",
            "<div class=\"speak\">",
            BuildQuestionCardInner(q, blockTitle, antiforgery, postPath, error));
        html = html.Replace(
            """<button class="btn primary" type="button">Volgende →</button>""",
            $"""<button class="btn primary" type="submit" form="{AnswerFormId}">Volgende →</button>""",
            StringComparison.Ordinal);
        return html;
    }

    public static string Delen(WerkgeverApkSharePreviewDto preview, string antiforgery, string postPath)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("03-delen.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoachButton(html);
        html = html.Replace("Mark Jansen", E(preview.ManagerLabel), StringComparison.Ordinal);
        html = html.Replace("Wat Mark te zien krijgt", $"Wat {E(preview.ManagerLabel)} te zien krijgt", StringComparison.Ordinal);
        var checkedAttr = preview.SharedWithManager ? " checked" : "";
        var seeList = string.Join(
            "",
            preview.ManagerSeesBullets.Select(b => $"<li><span class=\"dot\"></span>{E(b)}</li>"));
        html = ReplaceBetween(
            html,
            "<ul class=\"see-list\">",
            "</ul>",
            seeList,
            keepOpen: true,
            keepClose: true);
        html = ReplaceFirst(
            html,
            "<body class=\"share\">",
            $"<body class=\"share\"><form method=\"post\" action=\"{E(postPath)}\">{antiforgery}");
        html = ReplaceFirst(
            html,
            """<span class="sw" aria-hidden="true">""",
            $"""<input type="checkbox" name="share" value="true" style="margin-right:8px"{checkedAttr} /><span class="sw" aria-hidden="true">""");
        html = html.Replace(
            """<button class="btn primary" type="button">Delen met mijn leidinggevende</button>""",
            """<button class="btn primary" type="submit">Delen met mijn leidinggevende</button>""",
            StringComparison.Ordinal);
        html = html.Replace("</body>", "</form></body>", StringComparison.Ordinal);
        return html;
    }

    public static string Vergelijking(WerkgeverApkEmployeeComparisonResponse model, string managerName)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("06-vergelijking.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoachButton(html);
        html = html.Replace("Mark", E(managerName.Split(' ').FirstOrDefault() ?? managerName), StringComparison.Ordinal);
        var agree = string.Join("", model.Comparison.Agree.Select(a => $"""
<div class="block-item"><b>{E(a.Label)}</b><p>{E(a.Sentence)}</p></div>
"""));
        var differ = string.Join("", model.Comparison.Differ.Select(a => $"""
<div class="block-item"><b>{E(a.Label)}</b><p>{E(a.Sentence)}</p></div>
"""));
        html = ReplaceBetween(
            html,
            "<section class=\"card gold-top agree\">",
            "</section>",
            $"<h2>Hier zijn jullie het over eens</h2>{agree}",
            keepOpen: true,
            keepClose: true);
        html = ReplaceBetween(
            html,
            "<section class=\"card differ\">",
            "</section>",
            $"<h2>Hier kijken jullie anders</h2>{differ}",
            keepOpen: true,
            keepClose: true);
        return html;
    }

    public static string ManagerVragen(
        WerkgeverApkManagerSheetDto sheet,
        IReadOnlyList<WerkgeverApkManagerQuestionDto> catalog,
        int displayIndex)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("05-vragen.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoachButton(html);
        var label = string.IsNullOrWhiteSpace(sheet.EmployeeLabel) ? "deze medewerker" : sheet.EmployeeLabel!;
        html = html.Replace("Sara de Wit", E(label), StringComparison.Ordinal);
        html = html.Replace("Sara", E(label.Split(' ').FirstOrDefault() ?? label), StringComparison.Ordinal);
        var q = catalog[Math.Clamp(displayIndex - 1, 0, catalog.Count - 1)];
        html = ReplaceFirst(html, "Nieuwe dingen proberen", E(q.Label));
        html = ReplaceFirst(html, "Vraag 4 van 10", $"Vraag {displayIndex} van {catalog.Count}");
        return html;
    }

    private static string BuildQuestionCardInner(
        WerkgeverApkEmployeeQuestionDto q,
        string blockTitle,
        string antiforgery,
        string postPath,
        string? error)
    {
        var pct = q.Total > 0 ? (int)Math.Round(100.0 * q.Index / q.Total) : 0;
        var sb = new StringBuilder();
        sb.Append("<div class=\"q-progress\"><span>").Append(E(blockTitle));
        sb.Append("</span> Vraag ").Append(q.Index).Append(" van ").Append(q.Total).Append("</div>");
        sb.Append("<div class=\"bar\"><i style=\"width:").Append(pct).Append("%\"></i></div>");
        sb.Append("<form id=\"").Append(AnswerFormId).Append("\" method=\"post\" action=\"").Append(E(postPath)).Append("\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"").Append(E(q.ItemId)).Append("\" />");
        sb.Append("<h1>").Append(E(q.Question)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(q.Scenario))
        {
            sb.Append("<p class=\"scene\">").Append(E(q.Scenario)).Append("</p>");
        }

        sb.Append("<div class=\"scale-block\"><div class=\"lab\"><b>Hoe goed past dit bij jou?</b><small>1 is bijna niet, 5 is heel duidelijk</small></div>");
        sb.Append("<div class=\"scale\" role=\"radiogroup\" aria-label=\"Past bij mij, 1 tot 5\">");
        for (var v = 1; v <= 5; v++)
        {
            var chk = q.Value == v ? " checked" : "";
            var on = q.Value == v ? " on" : "";
            sb.Append("<label class=\"scale-opt").Append(on).Append("\"><input type=\"radio\" name=\"Value\" value=\"").Append(v);
            sb.Append('"').Append(chk).Append(" required><span>").Append(v).Append("</span></label>");
        }

        sb.Append("</div><div class=\"ends\"><span>Past bijna niet</span><span>Past heel duidelijk</span></div></div>");
        if (!string.IsNullOrWhiteSpace(error))
        {
            sb.Append("<p class=\"notice warn\" role=\"alert\">").Append(E(error)).Append("</p>");
        }

        sb.Append("</form>");
        return sb.ToString();
    }

    private static string ApplyBlockRail(
        string html,
        PupilMboHboBlock block,
        string progress,
        string mob,
        string sideNow,
        string blockTitle)
    {
        const string interesseDone =
            """<div class="step done"><span class="num">2</span><div><div class="t">Interessetest</div><div class="s">Af</div></div></div>""";
        const string competentieOn =
            """<div class="step on"><span class="num">3</span><div><div class="t">Competentietest</div><div class="s">Vraag 12 van 40</div></div></div>""";

        var interesseOn =
            $"""<div class="step on"><span class="num">2</span><div><div class="t">Interessetest</div><div class="s">{E(progress)}</div></div></div>""";
        var competentiePending =
            """<div class="step"><span class="num">3</span><div><div class="t">Competentietest</div><div class="s">Nog te doen</div></div></div>""";
        var competentieOnLive =
            $"""<div class="step on"><span class="num">3</span><div><div class="t">Competentietest</div><div class="s">{E(progress)}</div></div></div>""";
        var interesseDoneLive =
            """<div class="step done"><span class="num">2</span><div><div class="t">Interessetest</div><div class="s">Af</div></div></div>""";
        var werkOnLive =
            $"""<div class="step on"><span class="num">4</span><div><div class="t">Wat je belangrijk vindt</div><div class="s">{E(progress)}</div></div></div>""";

        if (block == PupilMboHboBlock.Interesse)
        {
            html = html.Replace(interesseDone, interesseOn, StringComparison.Ordinal);
            html = html.Replace(competentieOn, competentiePending, StringComparison.Ordinal);
            html = html.Replace(
                """<div class="m"><b>Interessetest</b><span class="pill shared">Af</span></div>""",
                $"""<div class="m"><b>Interessetest</b><span class="pill now">{E(sideNow)}</span></div>""",
                StringComparison.Ordinal);
            html = html.Replace(
                """<div class="m"><b>Competentietest</b><span class="pill now">12 / 40</span></div>""",
                """<div class="m"><b>Competentietest</b><span class="pill">Nog te doen</span></div>""",
                StringComparison.Ordinal);
        }
        else if (block == PupilMboHboBlock.Competentie)
        {
            html = html.Replace(competentieOn, competentieOnLive, StringComparison.Ordinal);
            html = html.Replace(
                """<div class="m"><b>Competentietest</b><span class="pill now">12 / 40</span></div>""",
                $"""<div class="m"><b>Competentietest</b><span class="pill now">{E(sideNow)}</span></div>""",
                StringComparison.Ordinal);
        }
        else if (block == PupilMboHboBlock.Werkwaarden)
        {
            html = html.Replace(interesseDone, interesseDoneLive, StringComparison.Ordinal);
            html = html.Replace(competentieOn, """<div class="step done"><span class="num">3</span><div><div class="t">Competentietest</div><div class="s">Af</div></div></div>""", StringComparison.Ordinal);
            html = html.Replace(
                """<div class="step"><span class="num">4</span><div><div class="t">Wat je belangrijk vindt</div><div class="s">Nog te doen</div></div></div>""",
                werkOnLive,
                StringComparison.Ordinal);
            html = html.Replace(
                """<div class="m"><b>Wat je belangrijk vindt</b><span class="pill">Nog te doen</span></div>""",
                $"""<div class="m"><b>Wat je belangrijk vindt</b><span class="pill now">{E(sideNow)}</span></div>""",
                StringComparison.Ordinal);
        }

        // mob dots: mark first blocks done when past interesse
        if (block != PupilMboHboBlock.Interesse)
        {
            html = html.Replace(
                """<div class="mob-progress"><div class="dots"><i class="done"></i><i class="done"></i><i class="on"></i>""",
                """<div class="mob-progress"><div class="dots"><i class="done"></i><i class="done"></i><i class="on"></i>""",
                StringComparison.Ordinal);
        }
        else
        {
            html = html.Replace(
                """<div class="mob-progress"><div class="dots"><i class="done"></i><i class="done"></i><i class="on"></i>""",
                """<div class="mob-progress"><div class="dots"><i class="done"></i><i class="on"></i><i></i>""",
                StringComparison.Ordinal);
        }

        html = html.Replace("Competentietest · 12 / 40", mob, StringComparison.Ordinal);
        html = html.Replace($"{blockTitle} · 12 / 40", mob, StringComparison.Ordinal);
        html = html.Replace("Vraag 12 van 40", progress, StringComparison.Ordinal);
        html = html.Replace("<span class=\"pill now\">12 / 40</span>", $"<span class=\"pill now\">{sideNow}</span>", StringComparison.Ordinal);

        return html;
    }

    private static string BlockTitle(PupilMboHboBlock block) => block switch
    {
        PupilMboHboBlock.Interesse => "Interessetest",
        PupilMboHboBlock.Competentie => "Competentietest",
        PupilMboHboBlock.Werkwaarden => "Wat je belangrijk vindt",
        _ => "Test"
    };

    private static string InjectCoachButton(string html)
        => html.Replace(
            "<div class=\"coach\">",
            "<button type=\"button\" class=\"coach\" data-ssr-coach-open aria-label=\"Vraag hulp aan Lobsy\">",
            StringComparison.Ordinal)
            .Replace(
                "</div>\n<div class=\"wm\">",
                "</button>\n<script src=\"/js/lobsy-coach-ssr.js\" defer></script>\n<div class=\"wm\">",
                StringComparison.Ordinal);

    private static string ReplaceBetweenMarkers(string html, string sectionOpen, string nextMarker, string inner)
    {
        var i0 = html.IndexOf(sectionOpen, StringComparison.Ordinal);
        if (i0 < 0)
        {
            return html;
        }

        var openEnd = html.IndexOf('>', i0) + 1;
        var i1 = html.IndexOf(nextMarker, openEnd, StringComparison.Ordinal);
        if (i1 < 0)
        {
            return html;
        }

        return html[..openEnd] + inner + html[i1..];
    }

    private static string ReplaceFirst(string html, string old, string @new)
    {
        var i = html.IndexOf(old, StringComparison.Ordinal);
        return i < 0 ? html : html[..i] + @new + html[(i + old.Length)..];
    }

    private static string ReplaceBetween(
        string html,
        string sectionOpen,
        string sectionClose,
        string inner,
        bool keepOpen,
        bool keepClose)
    {
        var i0 = html.IndexOf(sectionOpen, StringComparison.Ordinal);
        if (i0 < 0)
        {
            return html;
        }

        var i1 = html.IndexOf(sectionClose, i0, StringComparison.Ordinal);
        if (i1 < 0)
        {
            return html;
        }

        var openEnd = html.IndexOf('>', i0) + 1;
        var sb = new StringBuilder();
        if (keepOpen)
        {
            sb.Append(html.AsSpan(i0, openEnd - i0));
        }

        sb.Append(inner);
        if (keepClose)
        {
            sb.Append(sectionClose);
        }

        return html[..i0] + sb + html[(i1 + sectionClose.Length)..];
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}
