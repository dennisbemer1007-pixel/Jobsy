using System.Net;
using System.Text;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

/// <summary>Fill static mockup HTML (templates 02–07) with live SSR data.</summary>
public static class WerkgeverApkMockupPages
{
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
        html = html.Replace("Code VOOR-BEELD", "", StringComparison.Ordinal);
        html = InjectCoachButton(html);
        var pct = q.Total > 0 ? (int)Math.Round(100.0 * q.Index / q.Total) : 0;
        html = ReplaceFirst(html, "<h1>Ik help collega’s ook als het niet bij mijn taak hoort</h1>",
            $"<h1>{E(q.Question)}</h1>");
        if (!string.IsNullOrWhiteSpace(q.Scenario))
        {
            html = ReplaceFirst(html,
                "<p class=\"scene\">Denk aan een gewone werkdag in het magazijn of op de vloer. Hoe goed past deze zin bij jou?</p>",
                $"<p class=\"scene\">{E(q.Scenario)}</p>");
        }

        html = ReplaceFirst(html, "Vraag 12 van 40", $"Vraag {q.Index} van {q.Total}");
        html = ReplaceFirst(html, "style=\"width:30%\"", $"style=\"width:{pct}%\"");
        var form = new StringBuilder();
        form.Append("<form method=\"post\" action=\"").Append(E(postPath)).Append("\">");
        form.Append(antiforgery);
        form.Append("<input type=\"hidden\" name=\"ItemId\" value=\"").Append(E(q.ItemId)).Append("\" />");
        form.Append("<div class=\"scale-block\"><div class=\"lab\"><b>Hoe goed past dit bij jou?</b><small>1 is bijna niet, 5 is heel duidelijk</small></div>");
        form.Append("<div class=\"scale\" role=\"radiogroup\" aria-label=\"Past bij mij, 1 tot 5\">");
        for (var v = 1; v <= 5; v++)
        {
            var chk = q.Value == v ? " checked" : "";
            form.Append("<label class=\"scale-opt\"><input type=\"radio\" name=\"Value\" value=\"").Append(v);
            form.Append('"').Append(chk).Append(" required><span>").Append(v).Append("</span></label>");
        }

        form.Append("</div><div class=\"ends\"><span>Past bijna niet</span><span>Past heel duidelijk</span></div></div>");
        if (!string.IsNullOrWhiteSpace(error))
        {
            form.Append("<p class=\"notice warn\" role=\"alert\">").Append(E(error)).Append("</p>");
        }

        form.Append("<div class=\"foot\"><button class=\"btn primary\" type=\"submit\">");
        form.Append(q.Index >= q.Total ? "Volgende →" : "Volgende →");
        form.Append("</button></div></form>");
        html = ReplaceScaleBlock(html, form.ToString());
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

    private static string InjectCoachButton(string html)
        => html.Replace(
            "<div class=\"coach\">",
            "<button type=\"button\" class=\"coach\" data-ssr-coach-open aria-label=\"Vraag hulp aan Lobsy\">",
            StringComparison.Ordinal)
            .Replace(
                "</div>\n<div class=\"wm\">",
                "</button>\n<script src=\"/js/lobsy-coach-ssr.js\" defer></script>\n<div class=\"wm\">",
                StringComparison.Ordinal);

    private static string ReplaceScaleBlock(string html, string formInner)
    {
        const string start = "<div class=\"scale-block\">";
        var i0 = html.IndexOf(start, StringComparison.Ordinal);
        if (i0 < 0)
        {
            return html;
        }

        var i1 = html.IndexOf("</section>", i0, StringComparison.Ordinal);
        if (i1 < 0)
        {
            return html;
        }

        var sectionStart = html.LastIndexOf("<section", i0, StringComparison.Ordinal);
        return html[..sectionStart] + "<section class=\"card gold-top qcard\">" + formInner + "</section>" + html[(i1 + "</section>".Length)..];
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