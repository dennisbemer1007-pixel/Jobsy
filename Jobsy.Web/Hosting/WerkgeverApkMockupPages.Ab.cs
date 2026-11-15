using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkPakketMockupPages
{
    private const string AbFormId = "wa-ab-form";

    public static string AbQuestion(WerkgeverApkEmployeeQuestionDto q, string antiforgery, string postPath, string? error)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("13-ab-vraag.html", ChromeTokens(q));
        html = InjectCoachButton(html);
        var pct = q.Total > 0 ? (int)Math.Round(100.0 * q.Index / q.Total) : 0;
        var blockLabel = DisplayBlockLabel(q.DisplayBlock, q.DisplayBlockCount, q.Package);
        var progress = $"Vraag {q.Index} van ongeveer {q.Total}";
        var mob = $"{blockLabel} · {q.Index} / ongeveer {q.Total}";

        html = ReplaceBetweenMarkers(
            html,
            "<section class=\"card gold-top qcard\">",
            "<div class=\"wa-ab-contrast__options\"",
            BuildAbCardInner(q, blockLabel, progress, pct, antiforgery, postPath, error));

        html = html.Replace("Vraag 48 van ongeveer 200", progress, StringComparison.Ordinal);
        html = html.Replace("Sterke kanten · Blok 2 van 6", blockLabel, StringComparison.Ordinal);
        html = html.Replace("Blok 2 · 48 / ongeveer 200", mob, StringComparison.Ordinal);
        html = html.Replace(
            """<button class="btn primary" type="button">Volgende →</button>""",
            $"""<button class="btn primary" type="submit" form="{AbFormId}">Volgende →</button>""",
            StringComparison.Ordinal);

        return html;
    }

    public static string AbPause(WerkgeverApkPauseDto pause, string continuePath)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("13b-ab-pauze.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoachButton(html);
        html = html.Replace("Blok 2 van 6 is af.", $"Blok {pause.CompletedDisplayBlock} van {pause.DisplayBlockCount} is af.", StringComparison.Ordinal);
        html = html.Replace("Pauze · blok 2 van 6", $"Pauze · blok {pause.CompletedDisplayBlock} van {pause.DisplayBlockCount}", StringComparison.Ordinal);
        html = html.Replace(
            """<button class="btn primary" type="button">Verder met blok 3</button>""",
            $"""<a class="btn primary" href="{E(continuePath)}">Verder met blok {pause.CompletedDisplayBlock + 1}</a>""",
            StringComparison.Ordinal);
        return html;
    }

    public static string Preview(string template) => WerkgeverApkSsrHtml.RenderTemplate(template, new Dictionary<string, string>(StringComparer.Ordinal));

    private static Dictionary<string, string> ChromeTokens(WerkgeverApkEmployeeQuestionDto _)
        => new(StringComparer.Ordinal)
        {
            ["company"] = "",
            ["team"] = "",
            ["codeDisplay"] = "Code VOOR-BEELD",
        };

    private static string BuildAbCardInner(
        WerkgeverApkEmployeeQuestionDto q,
        string blockLabel,
        string progress,
        int pct,
        string antiforgery,
        string postPath,
        string? error)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"q-progress\"><span class=\"q-part\">").Append(E(blockLabel));
        sb.Append("</span><span>").Append(E(progress)).Append("</span></div>");
        sb.Append("<div class=\"bar\"><i style=\"width:").Append(pct).Append("%\"></i></div>");
        sb.Append("<form id=\"").Append(AbFormId).Append("\" method=\"post\" action=\"").Append(E(postPath)).Append("\" data-wa-ab-scale=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"").Append(E(q.ItemId)).Append("\" />");
        sb.Append("<input type=\"hidden\" name=\"Value\" value=\"");
        if (q.Value is int existing)
        {
            sb.Append(existing.ToString(CultureInfo.InvariantCulture));
        }

        sb.Append("\" />");
        sb.Append("<h1>").Append(E(q.Heading ?? q.Question)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(q.Scenario))
        {
            sb.Append("<p class=\"scene\">").Append(E(q.Scenario)).Append("</p>");
        }

        if (q.ItemKind == WerkgeverApkAbItemKind.Check)
        {
            sb.Append("<p class=\"scene\">").Append(E(q.Question)).Append("</p>");
            sb.Append("<div class=\"scale-block\"><div class=\"lab\"><b>Herken je dit bij jezelf?</b></div><div class=\"scale\" role=\"radiogroup\">");
            var labels = new[] { ("1", "Ja"), ("2", "Een beetje"), ("3", "Nog niet") };
            foreach (var (val, lab) in labels)
            {
                var chk = q.Value?.ToString() == val ? " checked" : "";
                var on = chk.Length > 0 ? " on" : "";
                sb.Append("<label class=\"scale-opt").Append(on).Append("\"><input type=\"radio\" name=\"Value\" value=\"").Append(val);
                sb.Append('"').Append(chk).Append(" required><span>").Append(E(lab)).Append("</span></label>");
            }

            sb.Append("</div></div>");
        }
        else
        {
            sb.Append(WerkgeverApkAbAnswerScaleHtml.RenderContrastCards(q.OptionA ?? "", q.OptionB ?? ""));
            sb.Append(WerkgeverApkAbAnswerScaleHtml.RenderScale(q.Value));
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            sb.Append("<p class=\"notice warn\" role=\"alert\">").Append(E(error)).Append("</p>");
        }

        sb.Append("</form>");
        return sb.ToString();
    }

    private static string DisplayBlockLabel(int block, int totalBlocks, WerkgeverApkPackageKind package)
    {
        if (package == WerkgeverApkPackageKind.Basis)
        {
            return block switch
            {
                1 => "Interesses",
                2 => "Sterke kanten",
                _ => "Wat je belangrijk vindt",
            };
        }

        return block switch
        {
            1 => "Blok 1, interesses",
            2 => "Blok 2, sterke kanten",
            3 => "Blok 3, werkwaarden",
            _ => $"Blok {block} van {totalBlocks}",
        };
    }

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
            i1 = html.IndexOf("<aside class=\"side\"", openEnd, StringComparison.Ordinal);
        }

        if (i1 < 0)
        {
            return html;
        }

        return html[..openEnd] + inner + html[i1..];
    }

    private static string InjectCoachButton(string html)
        => html.Replace(
            "<div class=\"coach\">",
            "<button type=\"button\" class=\"coach\" data-ssr-coach-open aria-label=\"Vraag hulp aan Lobsy\">",
            StringComparison.Ordinal)
            .Replace(
                "</div>\n<div class=\"wm\">",
                "</button>\n<script src=\"/js/lobsy-coach-ssr.js\" defer></script>\n<script src=\"/werkgever-apk/js/wa-ab-scale.js\" defer></script>\n<div class=\"wm\">",
                StringComparison.Ordinal);

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}