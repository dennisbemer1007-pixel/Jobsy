using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

public static class PupilReisSsrHtml
{
    public static string BuildBody(
        PupilProgressStateDto state,
        PupilQuestionSetDef def,
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string antiforgeryHtml,
        string answerSavePath,
        string? error,
        bool groep78,
        string? themeKey)
    {
        var item = state.CurrentItemId is null ? null : def.Bank.GetById(state.CurrentItemId);
        if (item is null)
        {
            return StateCard(culture["Leerling.Reis.ResultPending"], culture);
        }

        var worldKey = item.WorldKey ?? state.CurrentWorldKey ?? "koraalrif";
        var world = PupilWorldCatalog.Find(worldKey);
        var qLabel = culture[item.TextKey];
        var sb = new StringBuilder();
        if (!groep78)
        {
            sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Reis"]));
        }

        if (groep78)
        {
            var worldTitle = culture[PupilJourneyTheme.WorldTitleKey(themeKey, worldKey)];
            var qInWorld = def.IndexInWorld(state.CurrentIndex) + 1;
            var qWorldTotal = def.Worlds.FirstOrDefault(w => string.Equals(w.WorldKey, worldKey, StringComparison.OrdinalIgnoreCase))?.ItemCount ?? 15;
            var partIndex = PupilGroep78SsrParts.WorldPartIndex(def, worldKey);
            var partTotal = def.Worlds.Count;
            var sceneMeta = PupilGroep78SsrParts.SceneJourneyMetaLine(culture, worldTitle, partIndex, partTotal, qInWorld, qWorldTotal);
            var segCount = qWorldTotal;
            var segFilled = Math.Max(0, qInWorld - 1);
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, worldKey, worldKey, sceneMeta, true, "center 30%", segCount, segFilled, sceneMetaIsHtml: true));
        }

        sb.Append(groep78 ? """<div class="ll-reis ll-reis--g78 wrap ll-g78-overlap ll-g78-overlap--below-journey">""" : """<div class="ll-reis">""");
        if (!groep78)
        {
            sb.Append(Rail(def, state, culture, groep78, themeKey, worldKey));
        }

        sb.Append(groep78 ? """<div class="q-layout">""" : "");
        sb.Append(groep78 ? """<section class="card ll-question">""" : """<section class="ll-card ll-question">""");
        if (!groep78)
        {
            var pct = state.TotalItems <= 0 ? 0 : 100.0 * state.AnsweredCount / state.TotalItems;
            sb.Append("<div class=\"ll-progress-bar\" role=\"progressbar\"><span class=\"ll-progress-bar__fill\" style=\"width:");
            sb.Append(pct.ToString("0.#", CultureInfo.InvariantCulture)).Append("%\"></span></div>");
        }

        var worldTitleKey = groep78
            ? PupilJourneyTheme.WorldTitleKey(themeKey, worldKey)
            : world?.TitleKey ?? "Leerling.World.Reef.Title";
        if (groep78)
        {
            var qInWorldMeta = def.IndexInWorld(state.CurrentIndex) + 1;
            var qWorldTotalMeta = def.Worlds.FirstOrDefault(w => string.Equals(w.WorldKey, worldKey, StringComparison.OrdinalIgnoreCase))?.ItemCount ?? 15;
            sb.Append("<div class=\"qmeta\"><span class=\"kicker\">").Append(PupilLeerlingSsrParts.E(culture[worldTitleKey]));
            sb.Append("</span><span aria-hidden=\"true\">·</span><span>");
            sb.Append(PupilLeerlingSsrParts.E(string.Format(culture["Leerling.G78.Journey.QuestionMeta"], qInWorldMeta, qWorldTotalMeta)));
            sb.Append("</span></div>");
        }
        else
        {
            sb.Append("<p class=\"ll-question__world\">").Append(PupilLeerlingSsrParts.E(culture[worldTitleKey])).Append("</p>");
        }

        state.Answers.TryGetValue(item.Id, out var selected);
        sb.Append("<h1 id=\"ll-q-title\" class=\"").Append(groep78 ? "qtitle" : "ll-question__text").Append("\">");
        sb.Append(PupilLeerlingSsrParts.E(qLabel)).Append("</h1>");
        if (groep78)
        {
            sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(qLabel, $"ll-q-{item.Id}", culture, "Leerling.G78.ReadQuestion"));
            sb.Append(PupilGroep78SsrParts.Groep78AnswerScaleForm(def, culture, antiforgeryHtml, answerSavePath, item.Id, selected, error, qLabel));
            sb.Append(PupilGroep78SsrParts.Groep78PrevFoot(culture, state.CurrentIndex > 0));
        }
        else
        {
            sb.Append(PupilLeerlingSsrParts.SpeakButton(qLabel, $"ll-q-{item.Id}", culture));
            sb.Append(PupilLeerlingSsrParts.AnswerScaleForm(def, culture, antiforgeryHtml, answerSavePath, item.Id, selected, error));
            if (state.CurrentIndex > 0)
            {
                sb.Append("""<div class="ll-question__nav qfoot"><a class="btn-secondary" href="/leerling/reis?prev=1">""");
                sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Prev"])).Append("</a></div>");
            }
        }

        sb.Append("</section>");
        if (groep78)
        {
            sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
                culture,
                culture["Leerling.G78.Coach.Question"],
                qLabel,
                $"ll-q-{item.Id}"));
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    public static string WindowClosed(CultureState culture, PupilQuestionSet set)
        => StateCard(culture["Leerling.WindowClosed.Title"], culture, PupilCopy.Key(set, "Leerling.WindowClosed.Body"));

    public static string DoneCard(
        CultureState culture,
        bool groep78 = false,
        string? themeKey = null,
        IPupilQuestionSetRegistry? registry = null)
    {
        if (!groep78 || registry is null)
        {
            var legacy = new StringBuilder("""<section class="ll-card"><h1>""");
            legacy.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Done"])).Append("</h1><p>");
            legacy.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.DoneLead"])).Append("</p>");
            legacy.Append("""<a class="btn-primary" href="/leerling/dit-ben-jij">""");
            legacy.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.ToResult"])).Append("</a></section>");
            return legacy.ToString();
        }

        var snapshot = new PupilGroep78JourneySnapshot(
            "dit-ben-jij",
            culture["Leerling.G78.ReisDone.JourneyMeta"],
            SceneMetaIsHtml: true,
            ShowStrip: true,
            UseResultSceneBand: true);
        var sb = new StringBuilder();
        sb.Append(PupilGroep78ResultSceneSsrHtml.Render(culture, registry, themeKey, snapshot));
        sb.Append("""<div class="wrap ll-g78-overlap ll-g78-overlap--below-journey ll-reis-done--g78" data-testid="leerling-reis-klaar"><div class="q-layout">""");
        sb.Append("""<section class="card ll-reis-done-card">""");
        sb.Append("<div class=\"qmeta\"><span class=\"kicker\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.ReisDone.Kicker"])).Append("</span></div>");
        sb.Append("<h1 class=\"island-lead qtitle\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.ReisDone.Title"])).Append("</h1>");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(
            culture["Leerling.Reis.DoneLead"],
            "ll-reis-done",
            culture,
            "Leerling.G78.ReadAloud"));
        sb.Append("<p class=\"ll-reis-done-lead\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.DoneLead"])).Append("</p>");
        sb.Append("""<div class="qfoot"><a class="btn btn-main" href="/leerling/dit-ben-jij">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.ToResult"]));
        sb.Append(PupilGroep78SsrParts.NextSvg).Append("</a></div></section>");
        sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
            culture,
            culture["Leerling.G78.ReisDone.Coach"],
            culture["Leerling.G78.ReisDone.Coach"],
            "ll-reis-done-coach",
            "kreeft-welkom.png",
            mobileGuideFirst: true));
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string StateCard(string title, CultureState culture, string? bodyKey = null)
    {
        var sb = new StringBuilder("""<section class="ll-card"><h1>""");
        sb.Append(PupilLeerlingSsrParts.E(title)).Append("</h1>");
        if (bodyKey is not null)
        {
            sb.Append("<p>").Append(PupilLeerlingSsrParts.E(culture[bodyKey])).Append("</p>");
        }

        sb.Append("</section>");
        return sb.ToString();
    }

    private static string Rail(
        PupilQuestionSetDef def,
        PupilProgressStateDto state,
        CultureState culture,
        bool groep78,
        string? themeKey,
        string worldKey)
    {
        var sb = new StringBuilder("<aside class=\"ll-rail\" aria-label=\"");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Journey"]));
        sb.Append("\"><p class=\"ll-rail__title\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Journey"])).Append("</p><ol class=\"ll-rail__list\">");
        foreach (var railKey in def.RailWorldKeys())
        {
            var w = PupilWorldCatalog.Find(railKey);
            if (w is null)
            {
                continue;
            }

            var done = IsWorldDone(def, state, w.Key);
            var current = string.Equals(w.Key, worldKey, StringComparison.OrdinalIgnoreCase);
            var railTitleKey = groep78
                ? PupilJourneyTheme.WorldTitleKey(themeKey, w.Key)
                : w.TitleKey;
            sb.Append("<li class=\"").Append(done ? "is-done " : "").Append(current ? "is-now" : "").Append("\"><strong>");
            sb.Append(PupilLeerlingSsrParts.E(culture[railTitleKey])).Append("</strong></li>");
        }

        sb.Append("</ol></aside>");
        return sb.ToString();
    }

    private static bool IsWorldDone(PupilQuestionSetDef def, PupilProgressStateDto state, string worldKey)
    {
        if (string.Equals(worldKey, "pauze-eiland", StringComparison.OrdinalIgnoreCase))
        {
            return state.IslandDone;
        }

        var items = def.Bank.AllItems.Where(i => i.WorldKey == worldKey).ToList();
        return items.Count > 0 && items.All(i => state.Answers.ContainsKey(i.Id));
    }
}
