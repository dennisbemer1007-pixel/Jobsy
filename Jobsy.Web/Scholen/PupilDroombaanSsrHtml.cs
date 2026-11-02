using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

public sealed record PupilDroombaanPageModel(
    PupilResultPageDto Result,
    DreamJobRouteStubDto? Dream,
    string DreamJobFieldValue,
    string? FormError);

public static class PupilDroombaanSsrHtml
{
    public static string BuildBody(
        PupilDroombaanPageModel model,
        CultureState culture,
        string antiforgeryFieldHtml,
        bool useGroep78Shell = false,
        string? journeyThemeKey = null,
        IPupilQuestionSetRegistry? registry = null)
    {
        if (useGroep78Shell && registry is not null && ShouldShowJobPick(model))
        {
            return BuildGroep78JobPick(model, culture, antiforgeryFieldHtml, journeyThemeKey, registry);
        }

        var sb = new StringBuilder();
        if (useGroep78Shell)
        {
            var theme = PupilJourneyTheme.NormalizeOrDefault(journeyThemeKey);
            var band = PupilJourneyTheme.WorldBandImagePath(theme, "koraalrif");
            sb.Append("""<section class="ll-g78-scene" data-testid="leerling-g78-scene">""");
            sb.Append("""<section class="band ll-g78-world-band" style="background-image:url('""");
            sb.Append(band);
            sb.Append("""');background-position:center 30%;" aria-hidden="true"></section>""");
            sb.Append("</section>");
            sb.Append("""<div class="wrap ll-g78-overlap">""");
        }
        else
        {
            sb.Append("""<div class="ll-reis ll-dream" data-testid="leerling-droombaan">""");
        }

        sb.Append("""<section class="card ll-dream-ssr">""");
        sb.Append("<p class=\"ll-question__world\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.Title"]));
        sb.Append("</p><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.Lead"]));
        sb.Append("</h1>");

        if (!string.IsNullOrEmpty(model.FormError))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(model.FormError));
            sb.Append("</p>");
        }

        sb.Append($"""<form id="leerling-droombaan-form" method="post" action="{PupilDroombaanFormEndpoints.SavePath}" class="ll-dream-ssr__form" autocomplete="off">""");
        sb.Append(antiforgeryFieldHtml);
        sb.Append("<label class=\"ll-field\"><span class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.Search"]));
        sb.Append("</span><input type=\"text\" name=\"DreamJob\" value=\"");
        sb.Append(WebUtility.HtmlEncode(model.DreamJobFieldValue));
        sb.Append("\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.Search"]));
        sb.Append("\" autocomplete=\"off\" data-testid=\"leerling-droombaan-search\" /></label>");
        sb.Append("<button type=\"submit\" class=\"btn-primary ll-dream-ssr__save\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.Save"]));
        sb.Append("</button></form>");

        sb.Append("<p class=\"ll-dream-ssr__pick-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.PickFromList"]));
        sb.Append("</p><ul class=\"ll-dream-ssr__suggestions\">");

        foreach (var job in DreamJobCatalog.All.Take(12))
        {
            sb.Append("<li><form method=\"post\" action=\"");
            sb.Append(PupilDroombaanFormEndpoints.SavePath);
            sb.Append("\" class=\"ll-dream-ssr__pick-form\">");
            sb.Append(antiforgeryFieldHtml);
            sb.Append("<input type=\"hidden\" name=\"DreamJob\" value=\"");
            sb.Append(WebUtility.HtmlEncode(job.TitleNl));
            sb.Append("\" /><button type=\"submit\" class=\"ll-dream-ssr__pick\">");
            sb.Append(WebUtility.HtmlEncode(job.TitleNl));
            sb.Append("</button></form></li>");
        }

        sb.Append("</ul>");

        AppendDreamResult(sb, model, culture);

        sb.Append("</section>");
        var pdfHref = PupilStoryPdfLink.Href(
            null,
            PupilStoryPdfLink.NormalizeLanguageCode(culture.Language) ?? culture.Language);
        sb.Append("<nav class=\"ll-dream-ssr__nav ll-result__actions\" aria-label=\"Verder\">");
        sb.Append("<a class=\"btn-secondary\" href=\"/leerling/dit-ben-jij\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.OtherJob"]));
        sb.Append("</a>");
        if (model.Result.QuestionSet == PupilQuestionSet.Vo)
        {
            sb.Append("<a class=\"btn-secondary\" href=\"/leerling/vo-paspoort\">");
            sb.Append(WebUtility.HtmlEncode(culture["LeerlingPassport.Vo.Form.Title"]));
            sb.Append("</a>");
        }

        sb.Append("<a class=\"btn-secondary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(pdfHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.SavePdf"]));
        sb.Append("</a></nav>");
        if (useGroep78Shell)
        {
            sb.Append("</div>");
        }
        else
        {
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    private static bool ShouldShowJobPick(PupilDroombaanPageModel model)
        => string.IsNullOrWhiteSpace(model.DreamJobFieldValue)
           && (string.IsNullOrWhiteSpace(model.Result.DreamJobKey)
               || string.Equals(model.Result.DreamJobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase));

    private static string BuildGroep78JobPick(
        PupilDroombaanPageModel model,
        CultureState culture,
        string antiforgeryFieldHtml,
        string? journeyThemeKey,
        IPupilQuestionSetRegistry registry)
    {
        var (bandStem, bandPos) = PupilJourneyTheme.ResultSceneBand(journeyThemeKey);
        var sb = new StringBuilder();
        sb.Append("<section class=\"band jb ll-g78-dream-band\" style=\"background-image:url('/leerling/werelden/img/");
        sb.Append(bandStem).Append(".jpg');background-position:").Append(bandPos).Append(";min-height:360px\">");
        sb.Append("""<div class="wrap ll-g78-dream-band__journey">""");
        sb.Append(PupilLeerlingSsrParts.Groep78JourneyStrip(
            culture, registry, journeyThemeKey, "dit-ben-jij", culture["Leerling.G78.Dream.JourneyMeta"], 0, 0, sceneMetaIsHtml: true));
        sb.Append("</div></section>");
        sb.Append("""<div class="wrap ll-g78-overlap ll-g78-overlap--below-journey" data-testid="leerling-droombaan"><div class="q-layout">""");
        sb.Append("""<section class="card ll-dream-ssr ll-dream-pick">""");
        if (!string.IsNullOrEmpty(model.FormError))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(WebUtility.HtmlEncode(model.FormError)).Append("</p>");
        }

        sb.Append("<div class=\"qmeta\"><span class=\"kicker\">").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.Kicker"]));
        sb.Append("</span><span aria-hidden=\"true\">·</span><span>").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.PickOne"])).Append("</span></div>");
        sb.Append("<p class=\"job-intro-b1\">").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.Intro"])).Append("</p>");
        sb.Append("<h1 class=\"island-lead\">").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.Title"])).Append("</h1>");
        sb.Append("<p class=\"jpick-hint\">").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.Hint"])).Append("</p>");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(culture["Leerling.G78.Dream.Coach"], "ll-dream-intro", culture, "Leerling.G78.Dream.ReadIntro"));
        sb.Append($"""<form method="post" action="{PupilDroombaanFormEndpoints.SavePath}" class="ll-dream-ssr__form" data-ll-job-pick-form="1" autocomplete="off">""");
        sb.Append(antiforgeryFieldHtml);
        sb.Append("""<input type="hidden" name="DreamJob" value="" data-ll-job-pick-value="1" />""");
        sb.Append("""<div class="job-picks" role="radiogroup" data-testid="ll-g78-job-picks" aria-label="Kies een beroep">""");
        var suggestions = model.Result.JobSuggestions ?? [];
        var pickIndex = 0;
        var rendered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in suggestions)
        {
            var key = PupilGroep78MockHtml.ResolveJobKey(job.Title);
            if (!rendered.Add(key))
            {
                continue;
            }

            var selected = pickIndex == 0;
            sb.Append(PupilGroep78MockHtml.JobPickCard(culture, key, job.Title, job.DomainLabel, job.Blurb, selected));
            pickIndex++;
        }

        foreach (var jobTitle in model.Result.Story.JobIdeas)
        {
            if (pickIndex >= 5)
            {
                break;
            }

            var key = PupilGroep78MockHtml.ResolveJobKey(jobTitle);
            if (!rendered.Add(key))
            {
                continue;
            }

            sb.Append(PupilGroep78MockHtml.JobPickCard(culture, key, jobTitle, "", "", pickIndex == 0));
            pickIndex++;
        }

        sb.Append(PupilGroep78MockHtml.JobPickNone(culture, selected: false));
        sb.Append("</div><div class=\"qfoot\"><a class=\"btn btn-ghost\" href=\"/leerling/dit-ben-jij\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.OtherJob"])).Append("</a>");
        sb.Append("""<button type="submit" class="btn btn-main">""").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.Confirm"]));
        sb.Append(PupilGroep78SsrParts.NextSvg).Append("</button></div></form>");
        sb.Append("<p class=\"jpick-hint\">").Append(WebUtility.HtmlEncode(culture["Leerling.G78.Dream.ConfirmHint"])).Append("</p></section>");
        sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
            culture,
            culture["Leerling.G78.Dream.Coach"],
            culture["Leerling.G78.Dream.Coach"],
            "ll-dream-coach",
            "kreeft-kijker.png"));
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static void AppendDreamResult(StringBuilder sb, PupilDroombaanPageModel model, CultureState culture)
    {
        var dream = model.Dream;
        if (dream is null || string.IsNullOrWhiteSpace(dream.JobKey)
            || string.Equals(dream.JobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(dream?.JobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<h2 class=\"ll-result__sub\">");
                sb.Append(WebUtility.HtmlEncode(culture["LeerlingStory.JobsTitle"]));
                sb.Append("</h2><div class=\"ll-chips__row\">");
                foreach (var job in model.Result.Story.JobIdeas)
                {
                    sb.Append("<span class=\"ll-job-idea\">");
                    sb.Append(WebUtility.HtmlEncode(job));
                    sb.Append("</span>");
                }

                sb.Append("</div><p>");
                sb.Append(WebUtility.HtmlEncode(culture[PupilCopy.Key(model.Result.QuestionSet, "LeerlingDroom.UndecidedHint")]));
                sb.Append("</p>");
            }

            return;
        }

        sb.Append("<h2 class=\"ll-result__sub\">");
        sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.HaveTitle"]));
        sb.Append("</h2><ul class=\"ll-dream__list ll-dream__list--have\">");
        foreach (var item in dream.HaveItems)
        {
            sb.Append("<li>");
            sb.Append(WebUtility.HtmlEncode(item));
            sb.Append("</li>");
        }

        sb.Append("</ul>");

        if (dream.LearnItems.Count > 0)
        {
            sb.Append("<h2 class=\"ll-result__sub\">");
            sb.Append(WebUtility.HtmlEncode(culture["LeerlingDroom.NeedTitle"]));
            sb.Append("</h2><ul class=\"ll-dream__list ll-dream__list--need\">");
            foreach (var item in dream.LearnItems)
            {
                sb.Append("<li>");
                sb.Append(WebUtility.HtmlEncode(item));
                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        if (dream.RouteSteps.Count > 0)
        {
            sb.Append("<ul class=\"ll-dream__route\">");
            foreach (var step in dream.RouteSteps)
            {
                sb.Append("<li>");
                sb.Append(WebUtility.HtmlEncode(step));
                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        if (!string.IsNullOrWhiteSpace(dream.Encouragement))
        {
            sb.Append("<p class=\"ll-dream__cheer\">");
            sb.Append(WebUtility.HtmlEncode(dream.Encouragement));
            sb.Append("</p>");
        }
    }

    public static string ResolveFieldValue(PupilDroombaanPageModel? fromApi, string? queryDream)
    {
        if (!string.IsNullOrWhiteSpace(queryDream))
        {
            return queryDream.Trim();
        }

        var dream = fromApi?.Dream;
        if (!string.IsNullOrWhiteSpace(dream?.JobTitle))
        {
            return dream.JobTitle.Trim();
        }

        return "";
    }
}
