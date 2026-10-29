using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.UwvMockups;

internal static class OndernemersCheckSsrShells
{
    internal static string BuildBody(CultureState culture, OndernemersCheckSsrData data)
    {
        if (!data.UwvEnabled)
        {
            return $"<div class=\"test-page journey-page\"><p class=\"state-message\">{WebUtility.HtmlEncode(culture["Common.Error"])}</p></div>";
        }

        return data.Phase switch
        {
            "question" => BuildQuestion(culture),
            "result" => BuildResult(culture, data.OutcomeKey ?? "explore"),
            _ => BuildIntro(culture),
        };
    }

    private static string BuildIntro(CultureState culture)
    {
        var lang = culture.Language;
        var sb = new StringBuilder(4096);
        sb.Append("<div class=\"journey-page panel-page--cand-ill uwv-ondernemer-intro-page\">");
        sb.Append("<div class=\"opl-wrap uwv-ondernemer-intro-layout\"><header class=\"uwv-ondernemer-intro-layout__head\">");
        sb.Append("<p class=\"uwv-ondernemer-intro-layout__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("TestFlow.Eyebrow.Intro", UwvMockupCopy.Ondernemer.TestName(lang))));
        sb.Append("</p><h1 class=\"uwv-ondernemer-intro-layout__title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Ondernemer.TestName(lang)));
        sb.Append("</h1><p class=\"uwv-ondernemer-intro-layout__lead\">");
        sb.Append(WebUtility.HtmlEncode($"{UwvMockupCopy.Ondernemer.IntroBenefit(lang)} · {UwvMockupCopy.Ondernemer.IntroDuration(lang)}"));
        sb.Append("</p></header><section class=\"detail-card career-card uwv-ondernemer-intro-card__inner uwv-ondernemer-intro-layout__card\">");
        sb.Append("<p>").Append(WebUtility.HtmlEncode(UwvMockupCopy.Ondernemer.IntroLead(lang))).Append("</p>");
        sb.Append("<h2 class=\"uwv-ondernemer-intro-card__sub\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Ondernemer.IntroDiscoverTitle(lang)));
        sb.Append("</h2><ul class=\"uwv-ondernemer-intro-card__list\">");
        foreach (var item in UwvMockupCopy.Ondernemer.IntroDiscoverItems(lang))
        {
            sb.Append("<li>").Append(WebUtility.HtmlEncode(item)).Append("</li>");
        }

        sb.Append("</ul><footer class=\"test-shell__footer\"><a class=\"btn btn-primary\" href=\"/candidate/ondernemerscheck?stap=question\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "Start" : "Begin"));
        sb.Append("</a></footer></section></div></div>");
        return sb.ToString();
    }

    private static string BuildQuestion(CultureState culture)
    {
        var lang = culture.Language;
        var questions = UwvMockupCopy.Ondernemer.Questions(lang).ToArray();
        var labels = UwvMockupCopy.IsEnglish(lang)
            ? new[] { "Strongly disagree", "Disagree", "Neutral", "Agree", "Strongly agree" }
            : new[] { "Helemaal niet", "Niet", "Neutraal", "Wel", "Helemaal wel" };
        var boot = JsonSerializer.Serialize(new
        {
            questions,
            scaleLabels = labels,
            testName = UwvMockupCopy.Ondernemer.TestName(lang),
            levelName = UwvMockupCopy.IsEnglish(lang) ? "Short" : "Kort",
            bubbleText = UwvMockupCopy.IsEnglish(lang) ? "Take your time" : "Neem je tijd",
            questionOfFormat = culture["TestFlow.QuestionOf"],
            backLabel = UwvMockupCopy.IsEnglish(lang) ? "Back" : "Vorige",
            introBackHref = "/candidate/ondernemerscheck?stap=intro",
            introBackLabel = UwvMockupCopy.IsEnglish(lang) ? "Back" : "Terug",
            saveUrl = "/api/uwv-client/ondernemer",
            resultHref = "/candidate/ondernemerscheck?stap=result",
            errorMessage = culture["Common.Error"],
        });

        var sb = new StringBuilder(2048);
        sb.Append("<div class=\"journey-page uwv-ondernemer-test uwv-ondernemer-test--question\" data-uwv-ondernemer-question=\"1\">");
        JsonScriptEmbed.Append(sb, "uwv-ondernemer-boot", boot);
        sb.Append("<div class=\"uwv-ondernemer-test__shell\" data-uwv-ondernemer-mount></div></div>");
        return sb.ToString();
    }

    private static string BuildResult(CultureState culture, string outcomeKey)
    {
        var lang = culture.Language;
        var (resultTitle, resultLead) = UwvMockupCopy.Ondernemer.ResultCopy(outcomeKey, lang);
        var sb = new StringBuilder(4096);
        sb.Append("<div class=\"journey-page uwv-ondernemer-test uwv-ondernemer-test--result\"><div class=\"uwv-ondernemer-test__shell\">");
        sb.Append("<article class=\"detail-card career-card uwv-ondernemer-result__panel\"><div class=\"uwv-ondernemer-result__main\">");
        sb.Append("<p class=\"uwv-ondernemer-result__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("TestDone.Eyebrow", UwvMockupCopy.Ondernemer.TestName(lang), UwvMockupCopy.IsEnglish(lang) ? "Short" : "Kort")));
        sb.Append("</p><h1 class=\"uwv-ondernemer-result__title\" tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(resultTitle));
        sb.Append("</h1><p class=\"uwv-ondernemer-result__lead\">");
        sb.Append(WebUtility.HtmlEncode(resultLead));
        sb.Append("</p><h2 class=\"uwv-ondernemer-result__steps-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Ondernemer.ResultNextStepsTitle(lang)));
        sb.Append("</h2><ol class=\"uwv-ondernemer-result__steps\">");
        foreach (var step in UwvMockupCopy.Ondernemer.ResultNextSteps(lang))
        {
            sb.Append("<li>");
            if (step.External)
            {
                sb.Append("<a class=\"uwv-ondernemer-result__step-link uwv-ondernemer-result__step-link--external\" href=\"");
                sb.Append(WebUtility.HtmlEncode(step.Href));
                sb.Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");
                sb.Append(WebUtility.HtmlEncode(step.Label));
                sb.Append("<span class=\"uwv-ondernemer-result__step-icon\" aria-hidden=\"true\">&nbsp;↗</span></a>");
            }
            else
            {
                sb.Append("<a class=\"uwv-ondernemer-result__step-link\" href=\"");
                sb.Append(WebUtility.HtmlEncode(step.Href));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(step.Label));
                sb.Append("<span class=\"uwv-ondernemer-result__step-icon\" aria-hidden=\"true\">&nbsp;›</span></a>");
            }

            sb.Append("</li>");
        }

        sb.Append("</ol><footer class=\"test-shell__footer uwv-ondernemer-result__footer\">");
        sb.Append("<a class=\"btn btn-ghost\" href=\"/candidate/ontdekkingsreis\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "Back to discovery" : "Terug naar ontdekken"));
        sb.Append("</a><a class=\"btn btn-primary\" href=\"/candidate/adviseur-werk\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "Take to your work adviser" : "Neem mee naar je adviseur"));
        sb.Append("</a></footer><p class=\"uwv-ondernemer-result__done muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Ondernemer.ResultDoneLine(lang)));
        sb.Append("</p></div><figure class=\"uwv-ondernemer-result__scene\" aria-hidden=\"true\"><img src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.OndernemerResultScene));
        sb.Append("\" alt=\"\" width=\"960\" height=\"540\" decoding=\"async\" loading=\"lazy\" /></figure></article></div></div>");
        return sb.ToString();
    }
}
