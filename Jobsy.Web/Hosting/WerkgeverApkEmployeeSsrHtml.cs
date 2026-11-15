using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkEmployeeSsrHtml
{
    public static string RenderDocument(string title, string bodyHtml, IReadOnlyDictionary<string, string>? chrome = null)
    {
        var company = chrome?.GetValueOrDefault("company") ?? "";
        var team = chrome?.GetValueOrDefault("team") ?? "";
        var code = chrome?.GetValueOrDefault("code") ?? "";
        return $"""
<!doctype html>
<html lang="nl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{WebUtility.HtmlEncode(title)} — Lobsy Loopbaan-APK</title>
<link rel="stylesheet" href="/werkgever-apk/css/shared.css">
<link rel="stylesheet" href="/werkgever-apk/css/extra.css">
</head>
<body>
<div class="app">
  <header class="topbar">
    <a class="brand" href="/loopbaan-apk/welkom">
      <span class="logo-tile"><img src="/werkgever-apk/img/lobsy-64.png" alt=""></span>
      <span class="brand-name">Lobsy</span>
    </a>
    <span class="school">{WebUtility.HtmlEncode(company)} · {WebUtility.HtmlEncode(team)}</span>
    <span class="sp"></span>
    <span class="chip chip-code">{WebUtility.HtmlEncode(code)}</span>
  </header>
  {bodyHtml}
  <button type="button" class="coach" data-ssr-coach-open aria-label="Vraag hulp aan Lobsy">
    <span class="bubble">Waar kan ik je bij helpen?</span>
    <img class="full" src="/werkgever-apk/img/lobsy-coach-b2.png" alt="">
  </button>
</div>
<script src="/js/lobsy-coach-ssr.js" defer></script>
</body>
</html>
""";
    }

    public static string TestQuestion(
        WerkgeverApkEmployeeQuestionDto q,
        string antiforgery,
        string postPath,
        string? error)
    {
        var blockTitle = BlockTitle(q.Block);
        var pct = q.Total > 0 ? (int)Math.Round(100.0 * q.Index / q.Total) : 0;
        var sb = new StringBuilder();
        sb.Append("""<div class="shell"><main class="main"><section class="card gold-top qcard">""");
        sb.Append("<div class=\"q-progress\"><span>").Append(E(blockTitle));
        sb.Append("</span> Vraag ").Append(q.Index.ToString(CultureInfo.InvariantCulture));
        sb.Append(" van ").Append(q.Total.ToString(CultureInfo.InvariantCulture)).Append("</div>");
        sb.Append("<div class=\"bar\"><i style=\"width:").Append(pct.ToString(CultureInfo.InvariantCulture));
        sb.Append("%\"></i></div>");
        sb.Append("<form method=\"post\" action=\"").Append(E(postPath)).Append("\">");
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
            var on = q.Value == v ? " checked" : "";
            sb.Append("<label class=\"scale-opt\"><input type=\"radio\" name=\"Value\" value=\"").Append(v);
            sb.Append('"').Append(on).Append(" required><span>").Append(v).Append("</span></label>");
        }

        sb.Append("</div></div>");
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"notice warn\" role=\"alert\">").Append(E(error)).Append("</p>");
        }

        sb.Append("<div class=\"foot\"><button class=\"btn primary\" type=\"submit\">");
        sb.Append(q.Index >= q.Total ? "Afronden →" : "Volgende →").Append("</button></div></form></section></main></div>");
        return sb.ToString();
    }

    public static string DelenPage(WerkgeverApkSharePreviewDto preview, string antiforgery, string postPath)
    {
        var checkedAttr = preview.SharedWithManager ? " checked" : "";
        var bullets = string.Join("", preview.ManagerSeesBullets.Select(b => $"<li><span class=\"dot\"></span>{E(b)}</li>"));
        return $"""
<div class="shell"><main class="main share-layout">
  <h1>Wil je jouw beeld delen met je leidinggevende?</h1>
  <p class="lead">Alleen als jij dat wilt. Dit is bedoeld voor een ontwikkelgesprek, niet voor een beoordeling.</p>
  <section class="card gold gold-top choice">
    <form method="post" action="{E(postPath)}">
      {antiforgery}
      <label class="toggle">
        <input type="checkbox" name="share" value="true"{checkedAttr} />
        <span><b>Delen met {E(preview.ManagerLabel)}</b><p>Je kunt dit later weer uitzetten.</p></span>
      </label>
      <ul class="see-list">{bullets}</ul>
      <button class="btn primary" type="submit">Opslaan</button>
    </form>
  </section>
</main></div>
""";
    }

    public static string ComparisonPage(WerkgeverApkEmployeeComparisonResponse model)
    {
        var agree = string.Join("", model.Comparison.Agree.Select(a => $"<li>{E(a.Label)}: {E(a.Sentence)}</li>"));
        var differ = string.Join("", model.Comparison.Differ.Select(a => $"<li>{E(a.Label)}: {E(a.Sentence)}</li>"));
        var questions = string.Join("", model.Comparison.ConversationQuestions.Select(q => $"<li>{E(q)}</li>"));
        return $"""
<div class="shell"><main class="main">
  <h1>Zo kijken jullie naast elkaar</h1>
  <section class="card"><h2>Waar jullie het eens zijn</h2><ul>{agree}</ul></section>
  <section class="card"><h2>Waar jullie anders kijken</h2><ul>{differ}</ul></section>
  <section class="card"><h2>Gespreksvragen</h2><ul>{questions}</ul></section>
</main></div>
""";
    }

    private static string BlockTitle(PupilMboHboBlock block) => block switch
    {
        PupilMboHboBlock.Interesse => "Interessetest",
        PupilMboHboBlock.Competentie => "Competentietest",
        PupilMboHboBlock.Werkwaarden => "Wat je belangrijk vindt",
        _ => "Test"
    };

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}
