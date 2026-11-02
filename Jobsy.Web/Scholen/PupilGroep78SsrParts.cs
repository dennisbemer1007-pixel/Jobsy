using System.Globalization;
using System.Text;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>Mockup-aligned HTML fragments for groep 7/8 SSR pages.</summary>
public static class PupilGroep78SsrParts
{
    internal const string SpeakSvg =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11 5L6 9H2v6h4l5 4V5z"/><path d="M15.5 8.5a5 5 0 010 7"/><path d="M19 5a10 10 0 010 14"/></svg>""";

    internal const string BackSvg =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M15 18l-6-6 6-6"/></svg>""";

    internal const string NextSvg =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14M13 6l6 6-6 6"/></svg>""";

    internal const string CheckSvg =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="10"/><path d="M7 12.5l3.5 3.5L17 9"/></svg>""";

    public static string ThemeHeaderSmall(CultureState culture, string? themeKey)
    {
        themeKey = PupilJourneyTheme.NormalizeOrDefault(themeKey);
        var key = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => "Leerling.G78.ThemeShort.Tuin",
            PupilJourneyTheme.Sterrenkust => "Leerling.G78.ThemeShort.Ster",
            _ => "Leerling.G78.ThemeShort.Zee",
        };
        return culture[key];
    }

    public static string Groep78SpeakButton(string text, string speechKey, CultureState culture, bool active = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var cls = active ? "read-aloud speak on" : "read-aloud speak";
        return $"""
            <button type="button" class="{cls}" data-read-aloud-text="{PupilLeerlingSsrParts.E(text)}" data-speech-key="{PupilLeerlingSsrParts.E(speechKey)}" aria-label="{PupilLeerlingSsrParts.E(culture["ReadAloud.Play"])}">{SpeakSvg}</button>
            """;
    }

    public static string Groep78SpeakRow(string text, string speechKey, CultureState culture, string labelKey)
        => $"""
            <div class="qrow" data-testid="ll-g78-speak-row">{Groep78SpeakButton(text, speechKey, culture, active: true)}<span class="speak-label">{PupilLeerlingSsrParts.E(culture[labelKey])}</span></div>
            """;

    public static string Groep78CoachAside(
        CultureState culture,
        string bubbleText,
        string speakText,
        string speechKey,
        string lobsterImage = "kreeft-welkom.png",
        bool mobileGuideFirst = false)
    {
        var sb = new StringBuilder();
        if (mobileGuideFirst)
        {
            sb.Append(MobileCoachGuide(culture, bubbleText, speakText, speechKey));
        }

        sb.Append("""<aside class="ll-g78-coach d-only" aria-label="Lobsy" data-testid="ll-g78-coach">""");
        sb.Append("""<div class="bubble tail-b"><span class="who">Lobsy</span>""");
        sb.Append(PupilLeerlingSsrParts.E(bubbleText));
        sb.Append("""<div class="bt">""").Append(Groep78SpeakButton(speakText, speechKey, culture));
        sb.Append("<span class=\"speak-label\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.ReadAloud"])).Append("</span></div></div>");
        sb.Append("<img class=\"lob\" src=\"/leerling/werelden/img/").Append(PupilLeerlingSsrParts.E(lobsterImage));
        sb.Append("\" width=\"250\" height=\"250\" alt=\"\" /></aside>");
        if (!mobileGuideFirst)
        {
            sb.Append(MobileCoachGuide(culture, bubbleText, speakText, speechKey));
        }

        return sb.ToString();
    }

    private static string MobileCoachGuide(CultureState culture, string bubbleText, string speakText, string speechKey)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="m-only ll-g78-coach-mobile" data-testid="ll-g78-coach-mobile"><div class="guide"><div class="ring" aria-hidden="true"></div><div class="bubble tail-l"><span class="who">Lobsy</span>""");
        sb.Append(PupilLeerlingSsrParts.E(bubbleText));
        sb.Append("""<div class="bt">""").Append(Groep78SpeakButton(speakText, speechKey, culture));
        sb.Append("<span class=\"speak-label\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.ReadAloud"])).Append("</span></div></div></div></div>");
        return sb.ToString();
    }

    public static string Groep78AnswerScaleForm(
        PupilQuestionSetDef def,
        CultureState culture,
        string antiforgeryHtml,
        string saveAction,
        string? itemId,
        int? selected,
        string? error)
    {
        var sb = new StringBuilder();
        sb.Append($"""<form method="post" action="{PupilLeerlingSsrParts.E(saveAction)}" class="ll-answers-form" data-ll-answer-scale="1">""");
        sb.Append(antiforgeryHtml);
        sb.Append("<input type=\"hidden\" name=\"ItemId\" value=\"");
        sb.Append(PupilLeerlingSsrParts.E(itemId));
        sb.Append("\" /><input type=\"hidden\" name=\"Value\" value=\"");
        sb.Append(selected?.ToString(CultureInfo.InvariantCulture) ?? "");
        sb.Append("\" />");
        sb.Append("<div class=\"answers\" role=\"radiogroup\" aria-label=\"");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Answers"]));
        sb.Append("\" data-testid=\"ll-g78-answers\">");
        foreach (var (value, key) in def.AnswerLabels)
        {
            var label = culture[key];
            var isSel = selected == value;
            sb.Append("<button type=\"button\" data-ll-answer-value=\"");
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" class=\"ans");
            if (isSel)
            {
                sb.Append(" sel");
            }

            sb.Append("\" role=\"radio\" aria-checked=\"");
            sb.Append(isSel ? "true" : "false");
            sb.Append("\"><span class=\"lvl\"><i style=\"height:");
            sb.Append(AnswerFillPercent(value).ToString(CultureInfo.InvariantCulture));
            sb.Append("%\"></i></span>");
            sb.Append(PupilLeerlingSsrParts.E(label));
            sb.Append("</button>");
        }

        sb.Append("</div></form>");
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">");
            sb.Append(PupilLeerlingSsrParts.E(error));
            sb.Append("</p>");
        }

        return sb.ToString();
    }

    public static string Groep78PrevFoot(CultureState culture, bool canGoBack)
    {
        var label = PupilLeerlingSsrParts.E(culture["Leerling.Reis.Prev"]);
        if (canGoBack)
        {
            return $"""<div class="qfoot ll-question__nav" data-testid="ll-reis-prev"><a class="btn btn-ghost" href="/leerling/reis?prev=1">{BackSvg}{label}</a></div>""";
        }

        return $"""<div class="qfoot ll-question__nav" data-testid="ll-reis-prev"><button type="button" class="btn btn-ghost" disabled>{BackSvg}{label}</button></div>""";
    }

    /// <summary>Scene band meta matching mockup: bold world · deel · vraag.</summary>
    public static string SceneJourneyMetaLine(
        CultureState culture,
        string worldTitle,
        int partIndex,
        int partTotal,
        int qIndex,
        int qTotal)
        => $"<b>{PupilLeerlingSsrParts.E(worldTitle)}</b> · {PupilLeerlingSsrParts.E(string.Format(culture["Leerling.G78.Journey.PartMeta"], partIndex, partTotal))} · {PupilLeerlingSsrParts.E(string.Format(culture["Leerling.G78.Journey.SceneQuestionMeta"], qIndex, qTotal))}";

    public static int AnswerFillPercent(int answerValue) => answerValue switch
    {
        1 => 0,
        2 => 25,
        3 => 50,
        4 => 75,
        5 => 100,
        _ => 0,
    };

    public static int WorldPartIndex(PupilQuestionSetDef def, string worldKey)
    {
        for (var i = 0; i < def.Worlds.Count; i++)
        {
            if (string.Equals(def.Worlds[i].WorldKey, worldKey, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;
            }
        }

        return 1;
    }
}
