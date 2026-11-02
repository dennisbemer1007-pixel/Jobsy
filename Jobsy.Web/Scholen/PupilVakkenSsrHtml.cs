using System.Globalization;
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
        bool groep78,
        string? themeKey = null)
    {
        var pageKeys = PupilVakScores.KeysForPage(page);
        var sb = new StringBuilder();
        if (groep78)
        {
            AppendGroep78Body(sb, culture, page, pageKeys, scores, antiforgery, savePath, error, themeKey);
            return sb.ToString();
        }

        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Vakken.Kicker"]));
        sb.Append("""<div class="ll-reis ll-vakken" data-testid="leerling-vakken">""");
        sb.Append("""<section class="ll-card ll-vakken-card">""");
        sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Heading"])).Append("</h1>");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-vakken-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Page\" value=\"").Append(page).Append("\" />");
        foreach (var key in pageKeys)
        {
            scores.TryGetValue(key, out var p);
            AppendLegacySubject(sb, culture, key, p.Leuk, p.Goed);
        }

        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("<div class=\"ll-question__nav\"><a class=\"btn-secondary\" href=\"");
        sb.Append(page == 2 ? "/leerling/vakken" : "/leerling/reis").Append("\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Back"])).Append("</a>");
        sb.Append("<button type=\"submit\" class=\"btn-primary\">");
        sb.Append(PupilLeerlingSsrParts.E(culture[page == 1 ? "Leerling.Vakken.Next" : "Leerling.Vakken.Finish"])).Append("</button></div></form></section></div>");
        return sb.ToString();
    }

    private static void AppendGroep78Body(
        StringBuilder sb,
        CultureState culture,
        int page,
        IReadOnlyList<string> pageKeys,
        IReadOnlyDictionary<string, (int Leuk, int Goed)> scores,
        string antiforgery,
        string savePath,
        string? error,
        string? themeKey)
    {
        var theme = PupilJourneyTheme.NormalizeOrDefault(themeKey);
        var doneOnPage = DoneCountOnPage(pageKeys, scores);
        var totalOnPage = pageKeys.Count;
        var remainingOnPage = totalOnPage - doneOnPage;
        var pageProgressPct = totalOnPage == 0 ? 0 : doneOnPage * 100.0 / totalOnPage;
        var pageComplete = doneOnPage == totalOnPage && totalOnPage > 0;

        sb.Append("""<div class="ll-vt" data-testid="leerling-vakken">""");
        sb.Append("""<div class="ll-vt-scene-bar" data-testid="ll-vt-scene-bar">""");
        sb.Append("<span class=\"ll-vt-scene-bar__tag\">").Append(PupilLeerlingSsrParts.E(SceneTag(culture, theme))).Append("</span>");
        sb.Append("<span class=\"ll-vt-scene-bar__t\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Heading"])).Append("</span>");
        sb.Append("<span class=\"ll-vt-scene-bar__stars\" aria-hidden=\"true\">").Append(PupilLeerlingSsrParts.E(TopStarsLine(culture, page))).Append("</span>");
        sb.Append("</div>");
        sb.Append("""<main class="ll-vt-main">""");
        sb.Append("""<div class="ll-vt-hd">""");
        sb.Append("<div><h1 class=\"ll-vt-title\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Heading"])).Append("</h1>");
        sb.Append("<p class=\"ll-vt-sub\">").Append(PupilLeerlingSsrParts.E(culture[page == 1 ? "Leerling.Vakken.Intro1" : "Leerling.Vakken.Intro2"])).Append("</p></div>");
        sb.Append("""<div class="ll-vt-prog" aria-live="polite" data-ll-vt-prog>""");
        sb.Append("""<div class="ll-vt-prog__labels">""");
        sb.Append("<span data-ll-vt-remaining data-ll-vt-remaining-template=\"");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.PageRemaining"])).Append("\">");
        sb.Append(PupilLeerlingSsrParts.E(string.Format(culture["Leerling.Vakken.PageRemaining"], remainingOnPage))).Append("</span>");
        sb.Append("<span data-ll-vt-count>").Append(PupilLeerlingSsrParts.E(string.Format(culture["Leerling.Vakken.PageProgressCount"], doneOnPage, totalOnPage))).Append("</span>");
        sb.Append("</div>");
        sb.Append("<div class=\"ll-vt-prog__bar\" role=\"progressbar\" aria-valuemin=\"0\" aria-valuemax=\"");
        sb.Append(totalOnPage).Append("\" aria-valuenow=\"").Append(doneOnPage).Append("\">");
        sb.Append("<i style=\"width:").Append(pageProgressPct.ToString("0.##", CultureInfo.InvariantCulture)).Append("%\"></i></div>");
        sb.Append("</div></div>");

        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-vakken-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Page\" value=\"").Append(page).Append("\" />");
        sb.Append("""<div class="ll-vt-grid">""");
        foreach (var key in pageKeys)
        {
            scores.TryGetValue(key, out var p);
            var complete = SubjectComplete(p.Leuk, p.Goed);
            sb.Append("<article class=\"ll-vt-card");
            if (complete)
            {
                sb.Append(" is-done");
            }

            sb.Append("\"><div class=\"ll-vt-card__head\"><span class=\"ll-vt-card__ic\" aria-hidden=\"true\">");
            sb.Append(SubjectEmoji(key));
            sb.Append("</span><h2 class=\"ll-vt-card__name\">");
            sb.Append(PupilLeerlingSsrParts.E(culture[PupilVakSubjectLabels.UiKey(key)]));
            sb.Append("</h2>");
            if (complete)
            {
                sb.Append("""<span class="ll-vt-card__ok" aria-hidden="true">""").Append('✓').Append("</span>");
            }

            sb.Append("</div>");
            sb.Append("<input type=\"hidden\" name=\"Leuk.").Append(key).Append("\" value=\"").Append(p.Leuk).Append("\" />");
            sb.Append("<input type=\"hidden\" name=\"Goed.").Append(key).Append("\" value=\"").Append(p.Goed).Append("\" />");
            AppendGroep78Scale(sb, key, "Leuk", p.Leuk, culture["Leerling.Vakken.Leuk"], culture[PupilVakSubjectLabels.UiKey(key)], culture);
            AppendGroep78Scale(sb, key, "Goed", p.Goed, culture["Leerling.Vakken.Goed"], culture[PupilVakSubjectLabels.UiKey(key)], culture);
            sb.Append("</article>");
        }

        sb.Append("</div>");

        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-vt-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("""<div class="ll-vt-foot">""");
        sb.Append("<a class=\"ll-vt-back\" href=\"");
        sb.Append(page == 2 ? "/leerling/vakken" : "/leerling/reis").Append("\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Back"])).Append("</a>");
        sb.Append("<button type=\"submit\" class=\"ll-vt-next\" data-testid=\"ll-vt-next\"");
        if (!pageComplete)
        {
            sb.Append(" disabled");
        }

        sb.Append('>').Append(PupilLeerlingSsrParts.E(culture[page == 1 ? "Leerling.Vakken.Next" : "Leerling.Vakken.Finish"])).Append("</button>");
        sb.Append("""<div class="ll-vt-lob">""");
        sb.Append("<div class=\"ll-vt-bub\" data-ll-vt-bubble data-ll-vt-bubble-progress=\"");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Bubble.Progress"]));
        sb.Append("\" data-ll-vt-bubble-done=\"");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.Bubble.PageDone"])).Append("\">");
        sb.Append(PupilLeerlingSsrParts.E(BubbleText(culture, remainingOnPage))).Append("</div>");
        sb.Append("""<img src="/leerling/werelden/img/kreeft-portret.png" width="60" height="60" alt="Lobsy de kreeft" />""");
        sb.Append("</div></div></form></main></div>");
    }

    private static void AppendGroep78Scale(
        StringBuilder sb,
        string subject,
        string field,
        int value,
        string label,
        string subjectLabel,
        CultureState culture)
    {
        var aria = $"{subjectLabel} — {label}";
        sb.Append("<div class=\"ll-vt-q\"><div class=\"ll-vt-ql\">").Append(PupilLeerlingSsrParts.E(label)).Append("</div>");
        sb.Append("<div class=\"ll-vt-sc ll-vt-scale-btns\" role=\"radiogroup\" aria-label=\"");
        sb.Append(PupilLeerlingSsrParts.E(aria)).Append("\">");
        for (var v = 1; v <= 5; v++)
        {
            sb.Append("<button type=\"button\" class=\"ll-vt-scale-btn");
            if (value == v)
            {
                sb.Append(" is-on");
            }

            sb.Append("\" role=\"radio\" aria-checked=\"");
            sb.Append(value == v ? "true" : "false");
            sb.Append("\" data-ll-vt-subject=\"").Append(subject).Append("\" data-ll-vt-field=\"").Append(field);
            sb.Append("\" data-ll-vt-scale=\"").Append(v).Append("\">").Append(v).Append("</button>");
        }

        sb.Append("</div><div class=\"ll-vt-ends\"><span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.ScaleLow"]));
        sb.Append("</span><span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Vakken.ScaleHigh"])).Append("</span></div></div>");
    }

    private static void AppendLegacySubject(StringBuilder sb, CultureState culture, string key, int leuk, int goed)
    {
        sb.Append("<article class=\"ll-vakken__subject\"><div class=\"ll-vakken__subject-head\"><span class=\"ll-vakken__emoji\" aria-hidden=\"true\">");
        sb.Append(SubjectEmoji(key));
        sb.Append("</span><h2>").Append(PupilLeerlingSsrParts.E(culture[PupilVakSubjectLabels.UiKey(key)])).Append("</h2></div>");
        sb.Append("<input type=\"hidden\" name=\"Leuk.").Append(key).Append("\" value=\"").Append(leuk).Append("\" />");
        sb.Append("<input type=\"hidden\" name=\"Goed.").Append(key).Append("\" value=\"").Append(goed).Append("\" />");
        AppendLegacyScale(sb, key, "Leuk", leuk, culture["Leerling.Vakken.Leuk"]);
        AppendLegacyScale(sb, key, "Goed", goed, culture["Leerling.Vakken.Goed"]);
        sb.Append("</article>");
    }

    private static void AppendLegacyScale(StringBuilder sb, string subject, string field, int value, string label)
    {
        sb.Append("<div class=\"ll-vakken__scale\"><span class=\"ll-vakken__scale-label\">").Append(PupilLeerlingSsrParts.E(label)).Append("</span>");
        sb.Append("<div class=\"ll-vakken__scale-btns\">");
        for (var v = 1; v <= 5; v++)
        {
            sb.Append("<button type=\"button\" class=\"ll-vakken__btn");
            if (value == v)
            {
                sb.Append(" is-on");
            }

            sb.Append("\" data-ll-vt-subject=\"").Append(subject).Append("\" data-ll-vt-field=\"").Append(field);
            sb.Append("\" data-ll-vt-scale=\"").Append(v).Append("\">").Append(v).Append("</button>");
        }

        sb.Append("</div></div>");
    }

    private static string SceneTag(CultureState culture, string themeKey)
    {
        var themeTitle = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => culture["Leerling.Wereld.Tuin.Title"],
            PupilJourneyTheme.Sterrenkust => culture["Leerling.Wereld.Ster.Title"],
            _ => culture["Leerling.Wereld.Zee.Title"],
        };
        var spot = culture[PupilJourneyTheme.WorldTitleKey(themeKey, "lagune")];
        return $"{themeTitle} · {spot}";
    }

    private static string TopStarsLine(CultureState culture, int page)
    {
        var filled = page == 1 ? 3 : 4;
        var parts = new string[4];
        for (var i = 0; i < 4; i++)
        {
            parts[i] = i < filled ? "★" : "☆";
        }

        return $"{string.Join(" ", parts)} {string.Format(culture["Leerling.Vakken.PageOf"], page, 2)}";
    }

    private static string BubbleText(CultureState culture, int remainingOnPage)
        => remainingOnPage <= 0
            ? culture["Leerling.Vakken.Bubble.PageDone"]
            : string.Format(culture["Leerling.Vakken.Bubble.Progress"], remainingOnPage);

    private static int DoneCountOnPage(IReadOnlyList<string> pageKeys, IReadOnlyDictionary<string, (int Leuk, int Goed)> scores)
    {
        var done = 0;
        foreach (var key in pageKeys)
        {
            if (scores.TryGetValue(key, out var p) && SubjectComplete(p.Leuk, p.Goed))
            {
                done++;
            }
        }

        return done;
    }

    private static bool SubjectComplete(int leuk, int goed) => leuk is >= 1 and <= 5 && goed is >= 1 and <= 5;

    internal static string SubjectEmoji(string key) => key switch
    {
        "rekenen" => "🔢",
        "lezen" => "📖",
        "spelling" => "✏️",
        "taal" => "💬",
        "engels" => "🌍",
        "schrijven" => "📝",
        "aardrijkskunde" => "🗺️",
        "geschiedenis" => "🏛️",
        "natuur-techniek" => "🔬",
        "tekenen" => "🎨",
        "muziek" => "🎵",
        "gym" => "⚽",
        _ => "📚",
    };
}
