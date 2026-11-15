using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkFunctieMockupPages
{
    public static string FunctieKiezen(
        WerkgeverApkFunctiePickDto pick,
        string teamName,
        string companyName,
        string postPath,
        string searchPath,
        string antiforgery)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("08-functie-kiezen.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoach(html);
        html = html.Replace("Demo Logistiek B.V. · Team Logistiek", E($"{companyName} · {teamName}"), StringComparison.Ordinal);

        var who = pick.EmployeeLabel ?? "Medewerker";
        var initials = string.Concat(who.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w[0])).ToUpperInvariant();
        if (initials.Length > 3)
        {
            initials = initials[..3];
        }

        html = html.Replace("<b>Sara de Wit</b>", $"<b>{E(who)}</b>", StringComparison.Ordinal);
        html = html.Replace(">SdW<", $">{E(initials)}<", StringComparison.Ordinal);

        var query = pick.Query ?? pick.SelectedOccupationName ?? "";
        html = ReplaceFirst(html, "<div class=\"q\">Orderp<span class=\"caret\"></span></div>",
            $"""<form method="get" action="{E(searchPath)}" style="flex:1;display:flex"><input name="q" class="q" style="border:0;width:100%;font:inherit;font-weight:600" value="{E(query)}" autocomplete="off" /></form>""");

        var sugHtml = new StringBuilder();
        sugHtml.Append("<div class=\"k\">Suggesties uit de officiële beroepenlijst</div>");
        foreach (var hit in pick.Suggestions)
        {
            var on = string.Equals(hit.EscoOccupationId, pick.SelectedEscoOccupationId, StringComparison.OrdinalIgnoreCase)
                ? " on"
                : "";
            sugHtml.Append(
                $"""<form method="post" action="{E(postPath)}" class="sug{on}" style="margin:0"><input type="hidden" name="escoOccupationId" value="{E(hit.EscoOccupationId)}" />{antiforgery}<button type="submit" style="all:unset;display:flex;align-items:center;gap:10px;width:100%;cursor:pointer"><span class="mark"></span><b>{E(hit.Name)}</b></button></form>""");
        }

        const string suggestThroughChosen = "</div>\n            <div class=\"chosen\">";
        html = ReplaceBetween(
            html,
            "<div class=\"suggest\">",
            suggestThroughChosen,
            sugHtml.ToString(),
            keepOpen: true,
            keepClose: true);

        if (!string.IsNullOrWhiteSpace(pick.SelectedOccupationName))
        {
            html = html.Replace(
                "<b>Gekozen: Orderpicker.</b>",
                $"<b>Gekozen: {E(pick.SelectedOccupationName)}.</b>",
                StringComparison.Ordinal);
        }

        var rows = new StringBuilder();
        rows.Append("""<div class="frow head"><span>In dit team</span><span>Officiële functie</span></div>""");
        foreach (var row in pick.TeamRows)
        {
            var name = row.Label ?? $"Code {row.CodeDisplay}";
            var fn = string.IsNullOrWhiteSpace(row.OfficialOccupationName)
                ? """<span class="fn empty">Nog kiezen</span>"""
                : $"""<span class="fn">{E(row.OfficialOccupationName)}</span>""";
            rows.Append($"""<div class="frow"><span class="who">{E(name)}</span>{fn}</div>""");
        }

        if (pick.TeamRows.Count > 5)
        {
            rows.Append(
                $"""<p class="list-more" role="note">Alle {pick.TeamRows.Count} medewerkers in dit team (scroll in de lijst).</p>""");
        }

        html = ReplaceBetween(html, "<section class=\"card list-card\">", "</section>", rows.ToString(), keepOpen: true, keepClose: true);

        html = html.Replace(
            """<button class="btn primary" type="button"><span class="full-label">Functie opslaan</span>""",
            $"""<form method="post" action="{E(postPath)}" style="display:inline">{antiforgery}<input type="hidden" name="escoOccupationId" value="{E(pick.SelectedEscoOccupationId ?? "")}" /><button class="btn primary" type="submit"><span class="full-label">Functie opslaan</span>""",
            StringComparison.Ordinal);
        html = html.Replace(
            """<span class="short-label">Opslaan</span></button>""",
            """<span class="short-label">Opslaan</span></button></form>""",
            StringComparison.Ordinal);

        return html;
    }

    public static string MijnFunctie(WerkgeverApkFunctieComparisonDto comparison, IReadOnlyDictionary<string, string> chrome)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("09-mijn-functie.html", chrome);
        html = InjectCoach(html);
        html = html.Replace("Jouw functie: Orderpicker", $"Jouw functie: {E(comparison.OccupationTitle)}", StringComparison.Ordinal);

        var strong = new StringBuilder();
        foreach (var s in comparison.Strengths)
        {
            strong.Append($"""<div class="comp-item"><b>{E(s.Label)}</b><p>{E(s.Sentence)}</p></div>""");
        }

        html = ReplaceBetween(
            html,
            "<h2>Dit heb je al sterk voor je functie</h2>",
            "</section>",
            $"""<h2>Dit heb je al sterk voor je functie</h2><p class="sub">Punten uit het officiële profiel die jij in je antwoorden duidelijk herkent.</p>{strong}""",
            keepOpen: false,
            keepClose: true);

        var growth = new StringBuilder();
        var first = true;
        foreach (var g in comparison.GrowthAreas)
        {
            var open = first ? " open" : "";
            first = false;
            growth.Append(
                $"""<div class="grow-item{open}"><div class="grow-head"><b>{E(g.Label)}</b><span class="info" aria-label="Uitleg">i</span></div><div class="tip-pop">{E(g.Tooltip)}</div></div>""");
        }

        html = ReplaceBetween(
            html,
            "<h2>Hier kun je nog in groeien</h2>",
            "</section>",
            $"""<h2>Hier kun je nog in groeien</h2><p class="sub">Hoogstens vijf punten, het laagst herkende punt staat bovenaan. Tik op de i voor een korte uitleg bij jouw antwoorden.</p>{growth}""",
            keepOpen: false,
            keepClose: true);

        html = html.Replace(
            """<button class="btn primary" type="button"><span class="full-label">Zelf delen kiezen</span>""",
            """<a class="btn primary" href="/loopbaan-apk/delen"><span class="full-label">Zelf delen kiezen</span>""",
            StringComparison.Ordinal);
        html = html.Replace("""<span class="short-label">Delen</span></button>""", """<span class="short-label">Delen</span></a>""", StringComparison.Ordinal);
        html = html.Replace(
            """<button class="btn ghost" type="button"><span class="full-label">Naar mijn overzicht</span>""",
            """<a class="btn ghost" href="/loopbaan-apk/delen"><span class="full-label">Naar mijn overzicht</span>""",
            StringComparison.Ordinal);
        html = html.Replace("""<span class="short-label">Overzicht</span></button>""", """<span class="short-label">Overzicht</span></a>""", StringComparison.Ordinal);

        return html;
    }

    public static string Totaalbeeld(WerkgeverApkFunctieGroupInsightsDto insights, string companyName, string teamName)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("10-totaalbeeld-functie.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoach(html);
        html = html.Replace("Demo Logistiek B.V. · Team Logistiek", E($"{companyName} · {teamName}"), StringComparison.Ordinal);
        var title = $"{insights.SelectedTitle} · {insights.SharedCompletedCount} deelnemers";
        html = html.Replace("Orderpickers · 6 deelnemers", E(title), StringComparison.Ordinal);

        var chips = new StringBuilder();
        foreach (var tab in insights.Tabs)
        {
            var on = string.Equals(tab.EscoOccupationId, insights.SelectedEscoOccupationId, StringComparison.OrdinalIgnoreCase)
                ? " on"
                : tab.Visible ? "" : " lock";
            var label = tab.Visible ? $"{tab.SharedCount} · zichtbaar" : $"{tab.SharedCount} · vanaf 5";
            chips.Append(
                $"""<a class="fn-chip{on}" href="?esco={Uri.EscapeDataString(tab.EscoOccupationId)}">{E(tab.TabTitle)} <small>{E(label)}</small></a>""");
        }

        html = ReplaceBetween(html, "<div class=\"fn-switch\">", "</div>", chips.ToString(), keepOpen: true, keepClose: true);

        var bars = new StringBuilder();
        bars.Append(
            """<h2>Competenties uit het officiële profiel</h2><p class="sub">Hoeveel van de """ + insights.SharedCompletedCount +
            """ dit al sterk herkennen, of als ruimte om te groeien zien. Geen cijfer, bedoeld voor scholing in het team.</p><div class="dual-legend"><span><em class="sterk"></em> Herkennen dit al sterk</span><span><em class="groei"></em> Zien hier ruimte om te groeien</span></div>""");
        foreach (var bar in insights.Bars)
        {
            var total = Math.Max(1, bar.Total);
            var sPct = 100.0 * bar.StrongCount / total;
            var gPct = 100.0 * bar.GrowthCount / total;
            bars.Append(
                $"""<div class="comp-row"><div class="lab">{E(bar.Label)}</div><div class="split"><i class="sterk" style="width:{sPct:0.###}%"></i><i class="groei" style="width:{gPct:0.###}%"></i></div><div class="counts"><span class="s">{bar.StrongCount} van {bar.Total} al sterk</span><span class="g">{bar.GrowthCount} van {bar.Total} groei</span></div></div>""");
        }

        html = ReplaceBetween(
            html,
            "<section class=\"card gold-top comp-card\">",
            "</section>",
            bars.ToString(),
            keepOpen: true,
            keepClose: true);

        var train = new StringBuilder();
        train.Append(
            """<h2>Scholingskansen voor dit team</h2><p class="sub" style="font-size:13px;color:var(--muted);margin:0 0 6px">Een opleidingsrichting per punt waarop de groep nog kan groeien. Geen oordeel over één persoon.</p><ul class="school-list">""");
        foreach (var t in insights.Training)
        {
            train.Append($"""<li><b>{E(t.Label)}</b><p>{E(t.Hint)}</p></li>""");
        }

        train.Append(
            """</ul><div class="slim-note"><b>Past bij subsidies zoals SLIM</b><span class="slim-body"> Dit soort scholing in het mkb kan passen bij regelingen zoals SLIM. Lobsy geeft zelf geen subsidie-advies.</span></div>""");
        html = ReplaceBetween(html, "<section class=\"card later\">", "</section>", train.ToString(), keepOpen: true, keepClose: true);

        return html;
    }

    public static string TeWeinig(WerkgeverApkFunctieGroupInsightsDto insights, string companyName, string teamName, Guid teamId)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate("10b-te-weinig.html", new Dictionary<string, string>(StringComparer.Ordinal));
        html = InjectCoach(html);
        html = html.Replace("Demo Logistiek B.V. · Team Logistiek", E($"{companyName} · {teamName}"), StringComparison.Ordinal);
        var title = $"{insights.SelectedTitle} · {insights.SharedCompletedCount} deelnemers";
        html = html.Replace("Magazijnmedewerkers · 3 deelnemers", E(title), StringComparison.Ordinal);

        var chips = new StringBuilder();
        foreach (var tab in insights.Tabs)
        {
            var on = string.Equals(tab.EscoOccupationId, insights.SelectedEscoOccupationId, StringComparison.OrdinalIgnoreCase)
                ? " on"
                : tab.Visible ? "" : " lock";
            var label = tab.Visible ? $"{tab.SharedCount} · zichtbaar" : $"{tab.SharedCount} · vanaf 5";
            chips.Append(
                $"""<a class="fn-chip{on}" href="?esco={Uri.EscapeDataString(tab.EscoOccupationId)}">{E(tab.TabTitle)} <small>{E(label)}</small></a>""");
        }

        html = ReplaceBetween(html, "<div class=\"fn-switch\">", "</div>", chips.ToString(), keepOpen: true, keepClose: true);

        var need = WerkgeverApkAnonymity.MinGroupSize;
        var pct = 100.0 * insights.SharedCompletedCount / need;
        html = html.Replace("3 van 5", $"{insights.SharedCompletedCount} van {need}", StringComparison.Ordinal);
        html = html.Replace("width:60%", $"width:{pct:0.#}%", StringComparison.Ordinal);

        var visible = insights.Tabs.FirstOrDefault(t => t.Visible);
        if (visible is not null)
        {
            html = html.Replace(
                """<b>Orderpicker is al zichtbaar</b>""",
                $"""<b>{E(visible.TabTitle)} is al zichtbaar</b>""",
                StringComparison.Ordinal);
            html = html.Replace(
                """<button class="btn primary" type="button"><span class="full-label">Naar Orderpickers</span>""",
                $"""<a class="btn primary" href="/werkgever/loopbaan-apk/teams/{teamId:D}/functie-totaal?esco={Uri.EscapeDataString(visible.EscoOccupationId)}"><span class="full-label">Naar {E(visible.TabTitle)}</span>""",
                StringComparison.Ordinal);
            html = html.Replace("""<span class="short-label">Groep</span></button>""", """<span class="short-label">Groep</span></a>""", StringComparison.Ordinal);
        }

        html = html.Replace(
            """<button class="btn ghost" type="button"><span class="full-label">Naar teamoverzicht</span>""",
            $"""<a class="btn ghost" href="/werkgever/loopbaan-apk/teams/{teamId:D}"><span class="full-label">Naar teamoverzicht</span>""",
            StringComparison.Ordinal);
        html = html.Replace("""<span class="short-label">Team</span></button>""", """<span class="short-label">Team</span></a>""", StringComparison.Ordinal);

        return html;
    }

    private static string InjectCoach(string html)
    {
        html = html.Replace("<div class=\"coach\">", "<button type=\"button\" class=\"coach\" data-ssr-coach-open>", StringComparison.Ordinal);
        html = Regex.Replace(
            html,
            "(<button type=\"button\" class=\"coach\"[^>]*>[\\s\\S]*?<img class=\"full\"[^>]*>)\\s*</div>",
            "$1</button>",
            RegexOptions.Singleline);
        html = html.Replace(
            "</div>\n<div class=\"wm\">",
            "</div>\n<script src=\"/js/lobsy-coach-ssr.js\" defer></script>\n<div class=\"wm\">",
            StringComparison.Ordinal);
        return html;
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

        var openTagEnd = html.IndexOf('>', i0) + 1;
        var sb = new StringBuilder();
        if (keepOpen)
        {
            sb.Append(html.AsSpan(i0, openTagEnd - i0));
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