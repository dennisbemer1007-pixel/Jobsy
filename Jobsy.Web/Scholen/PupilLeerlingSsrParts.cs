using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Scholen;

public static class PupilLeerlingSsrParts
{
    public static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    public static string AntiforgeryField(IAntiforgery antiforgery, HttpContext http)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        return tokens.RequestToken is { } token
            ? $"""<input type="hidden" name="{tokens.FormFieldName}" value="{E(token)}" />"""
            : "";
    }

    public static string SpeakButton(string text, string speechKey, CultureState culture)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        return $"""
            <button type="button" class="read-aloud read-aloud--icon" data-read-aloud-text="{E(text)}" data-speech-key="{E(speechKey)}" aria-label="{E(culture["ReadAloud.Play"])}" title="{E(culture["ReadAloud.Play"])}"><span class="read-aloud__icon" aria-hidden="true">🔊</span></button>
            """;
    }

    public static string CoachCue(string text)
        => $"""<div class="ll-coach-cue" data-testid="ll-coach-cue"><p>{E(text)}</p></div>""";

    public static string Groep78WorldBand(string? themeKey, string worldKey, string backgroundPosition = "center 30%")
    {
        var band = PupilJourneyTheme.WorldBandImagePath(themeKey, worldKey);
        return $"""
            <section class="band ll-g78-world-band" style="background-image:url('{E(band)}');background-position:{E(backgroundPosition)};" aria-hidden="true"></section>
            """;
    }

    public static string Groep78SceneHeader(
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey,
        string worldKey,
        string currentNodeKey,
        string? metaLine = null,
        bool showJourney = true,
        string backgroundPosition = "center 30%",
        int segmentCount = 0,
        int segmentsFilled = 0,
        bool sceneMetaIsHtml = false)
    {
        var sb = new StringBuilder();
        sb.Append("""<section class="ll-g78-scene" data-testid="leerling-g78-scene">""");
        sb.Append(Groep78WorldBand(themeKey, worldKey, backgroundPosition));
        if (showJourney)
        {
            sb.Append("""<div class="ll-g78-scene__journey">""");
            sb.Append(Groep78JourneyStrip(culture, registry, themeKey, currentNodeKey, metaLine, segmentCount, segmentsFilled, sceneMetaIsHtml));
            sb.Append("</div>");
        }

        sb.Append("</section>");
        return sb.ToString();
    }

    public static string Groep78JourneyStrip(
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey,
        string currentNodeKey,
        string? metaLine,
        int segmentCount,
        int segmentsFilled,
        bool sceneMetaIsHtml = false)
    {
        var def = registry.Get(PupilQuestionSet.Groep78);
        var nodes = PupilGroep78Journey.BuildNodes(def);
        var currentIndex = PupilGroep78Journey.IndexOfCurrent(nodes, currentNodeKey);
        var progressWidth = nodes.Count <= 1 ? 0 : currentIndex / (double)(nodes.Count - 1) * 100;

        var sb = new StringBuilder();
        sb.Append("""<div class="g78-journey" data-testid="leerling-g78-journey"><div class="g78-jrow">""");
        if (progressWidth > 0)
        {
            sb.Append("<span class=\"g78-jdone\" style=\"width:");
            sb.Append(progressWidth.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append("%\"></span>");
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var cur = i == currentIndex;
            var done = i < currentIndex;
            var puzzle = node.Kind == PupilGroep78JourneyNodeKind.Puzzle;
            var title = culture[PupilJourneyTheme.StripTitleKey(themeKey, node)];
            var dotStyle = puzzle
                ? ""
                : $"background-image:url('/leerling/werelden/img/{PupilGroep78Journey.DotImageStem(themeKey, node)}.jpg')";
            sb.Append("<div class=\"g78-jn");
            if (done)
            {
                sb.Append(" is-done");
            }

            if (cur)
            {
                sb.Append(" is-cur");
            }

            if (puzzle)
            {
                sb.Append(" is-puzzle");
            }

            sb.Append("\">");
            if (cur)
            {
                sb.Append("<span class=\"g78-here\">");
                sb.Append(E(culture["Leerling.G78.Journey.Here"]));
                sb.Append("</span>");
            }

            sb.Append("<span class=\"g78-dot\" style=\"");
            sb.Append(E(dotStyle));
            sb.Append("\">");
            if (puzzle)
            {
                sb.Append("""<svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor" aria-hidden="true" style="width:20px;height:20px;max-width:20px;max-height:20px;display:block"><path d="M10 3a2 2 0 014 0v2h4a1 1 0 011 1v4h-2a2 2 0 000 4h2v4a1 1 0 01-1 1h-4v-2a2 2 0 00-4 0v2H6a1 1 0 01-1-1v-4h2a2 2 0 000-4H5V6a1 1 0 011-1h4V3z"/></svg>""");
            }

            sb.Append("</span><b>");
            sb.Append(E(title));
            sb.Append("</b></div>");
        }

        sb.Append("</div>");
        if (!string.IsNullOrEmpty(metaLine))
        {
            sb.Append("<div class=\"g78-jmeta\"><span>");
            if (sceneMetaIsHtml)
            {
                sb.Append(metaLine);
            }
            else
            {
                sb.Append(E(metaLine));
            }

            sb.Append("</span>");
            if (segmentCount > 0)
            {
                sb.Append("<div class=\"g78-seg\" aria-hidden=\"true\">");
                for (var s = 0; s < segmentCount; s++)
                {
                    var cls = s < segmentsFilled ? "is-filled" : s == segmentsFilled ? "is-cur" : "";
                    sb.Append("<i class=\"");
                    sb.Append(cls);
                    sb.Append("\"></i>");
                }

                sb.Append("</div>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    public static string AnswerScaleForm(
        PupilQuestionSetDef def,
        CultureState culture,
        string antiforgeryHtml,
        string saveAction,
        string? itemId,
        int? selected,
        string? error)
    {
        var sb = new StringBuilder();
        sb.Append($"""<form method="post" action="{E(saveAction)}" class="ll-answers-form" data-ll-answer-scale="1">""");
        sb.Append(antiforgeryHtml);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"");
        sb.Append(E(itemId));
        sb.Append("\" />");
        sb.Append("<div class=\"ll-answers");
        if (def.Set == PupilQuestionSet.Vo)
        {
            sb.Append(" ll-answers--vo");
        }

        sb.Append("\" role=\"radiogroup\" aria-label=\"");
        sb.Append(E(culture["Leerling.Reis.Answers"]));
        sb.Append("\">");
        foreach (var (value, key) in def.AnswerLabels)
        {
            var label = culture[key];
            var isSel = selected == value;
            sb.Append("<button type=\"submit\" name=\"Value\" value=\"");
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" class=\"ll-answer");
            if (def.Set == PupilQuestionSet.Vo)
            {
                sb.Append(" ll-answer--vo");
            }

            if (isSel)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" role=\"radio\" aria-checked=\"");
            sb.Append(isSel ? "true" : "false");
            sb.Append("\">");
            if (def.Set == PupilQuestionSet.Vo)
            {
                sb.Append("<span class=\"ll-answer__box\" aria-hidden=\"true\">");
                if (isSel)
                {
                    sb.Append("""<svg viewBox="0 0 16 16" focusable="false"><path fill="currentColor" d="M6.2 11.4 2.8 8l1.1-1.1 2.3 2.3 5.9-5.9L13.2 4.4z" /></svg>""");
                }

                sb.Append("</span>");
            }
            else
            {
                sb.Append("<span class=\"ll-answer__dot\" aria-hidden=\"true\"></span>");
            }

            sb.Append("<span class=\"ll-answer__label\">");
            sb.Append(E(label));
            sb.Append("</span></button>");
        }

        sb.Append("</div></form>");
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">");
            sb.Append(E(error));
            sb.Append("</p>");
        }

        return sb.ToString();
    }
}
