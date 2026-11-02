using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET/POST for pupil journey pages (no Blazor under /leerling/* except login).</summary>
public static class PupilLeerlingJourneyEndpoints
{
    public const string WereldSavePath = "/leerling/wereld/save";
    public const string ReisAnswerPath = "/leerling/reis/answer";
    public const string EilandSavePath = "/leerling/eiland/save";
    public const string VakkenSavePath = "/leerling/vakken/save";
    public const string VoPaspoortSavePath = "/leerling/vo-paspoort/save";
    public const string MboIntroPath = "/leerling/mbo-hbo/uitleg/start";

    public static IEndpointRouteBuilder MapPupilLeerlingJourneyEndpoints(this IEndpointRouteBuilder app)
    {
        var pupil = new AuthorizeAttribute { Policy = JobsyPolicies.PupilSession };

        app.MapGet("/leerling/start", GetStartAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/stop", GetStopAsync).AllowAnonymous();
        app.MapGet("/leerling/wereld", GetWereldAsync).RequireAuthorization(pupil);
        app.MapPost(WereldSavePath, PostWereldAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/reis", GetReisAsync).RequireAuthorization(pupil);
        app.MapPost(ReisAnswerPath, PostReisAnswerAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/eiland", GetEilandAsync).RequireAuthorization(pupil);
        app.MapPost(EilandSavePath, PostEilandAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/vakken", GetVakkenAsync).RequireAuthorization(pupil);
        app.MapPost(VakkenSavePath, PostVakkenAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/puzzel/{key}", GetPuzzelAsync).RequireAuthorization(pupil);
        app.MapPost("/leerling/puzzel/{key}/save", PostPuzzelAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/dit-ben-jij", GetDitBenJijAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/paspoort", GetPaspoortAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/vo-paspoort", GetVoPaspoortAsync).RequireAuthorization(pupil);
        app.MapPost(VoPaspoortSavePath, PostVoPaspoortAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/mbo-hbo/uitleg", GetMboUitlegAsync).RequireAuthorization(pupil);
        app.MapPost(MboIntroPath, PostMboIntroAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/mbo-hbo/overzicht", GetMboOverzichtAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/mbo-hbo/test/{blockKey}", GetMboTestAsync).RequireAuthorization(pupil);
        app.MapPost("/leerling/mbo-hbo/test/{blockKey}/answer", PostMboTestAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/mbo-hbo/uitkomst/{blockKey}", GetMboUitkomstAsync).RequireAuthorization(pupil);
        app.MapGet("/leerling/mbo-hbo/paspoort", GetMboPaspoortAsync).RequireAuthorization(pupil);
        return app;
    }

    private static async Task<IResult> GetStartAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return PlainError("Kon start niet laden.");
        }

        if (PupilLeerlingProgressRouting.RedirectForStart(progress) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var def = registry.Get(progress.QuestionSet);
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);
        var body = PupilStartSsrHtml.BuildBody(def, culture, groep78, progress.JourneyThemeKey, registry);
        return Html(http, culture["Leerling.Start.Title"], body, "/leerling/start", groep78, progress.JourneyThemeKey, af);
    }

    private static Task<IResult> GetStopAsync(HttpContext http, CultureState culture, IAntiforgery af)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var done = http.Request.Query["done"].ToString();
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilStopSsrHtml.BuildBody(culture, done, afHtml);
        var doc = PupilLeerlingSsrFrame.RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = string.IsNullOrEmpty(done) ? culture["Leerling.Stop.AskTitle"] : culture["Leerling.Stop.Title"],
            BodyHtml = body,
            Pupil = http.User,
            RoutePath = "/leerling/stop",
            ShowPause = false,
        });
        return Task.FromResult(Results.Content(doc, "text/html; charset=utf-8"));
    }

    private static async Task<IResult> GetWereldAsync(HttpContext http, JobsyApiClient api, CultureState culture, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return Results.Redirect("/leerling?error=expired");
        }

        if (PupilLeerlingProgressRouting.RedirectForWereld(progress) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilWereldSsrHtml.BuildBody(culture, afHtml, progress.JourneyThemeKey, WereldSavePath);
        return Html(http, culture["Leerling.Wereld.Title"], body, "/leerling/wereld", useG78: true, progress.JourneyThemeKey, af, showPause: false);
    }

    private static async Task<IResult> PostWereldAsync(HttpContext http, JobsyApiClient api, IAntiforgery af, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType || !await af.IsRequestValidAsync(http))
        {
            return Results.Redirect("/leerling/wereld?error=retry");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var key = form["ThemeKey"].ToString();
        var saved = await api.SavePupilJourneyThemeAsync(key, ct);
        return Results.Redirect(saved?.RedirectPath ?? "/leerling/start");
    }

    private static async Task<IResult> GetReisAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return PlainError(culture["Leerling.Session.Lost"]);
        }

        if (PupilGroep78Navigation.ThemeRedirectPath(progress) is { } themeRedir)
        {
            return Results.Redirect(themeRedir);
        }

        if (PupilLeerlingProgressRouting.RedirectForReis(progress) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var def = registry.Get(progress.QuestionSet);
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);
        if (!progress.WindowOpen && !progress.Completed)
        {
            var closed = PupilReisSsrHtml.WindowClosed(culture, progress.QuestionSet);
            return Html(http, culture["Leerling.Reis.Title"], closed, "/leerling/reis", groep78, progress.JourneyThemeKey, af);
        }

        if (progress.Completed)
        {
            return Html(http, culture["Leerling.Reis.Title"], PupilReisSsrHtml.DoneCard(culture), "/leerling/reis", groep78, progress.JourneyThemeKey, af);
        }

        var error = http.Request.Query["error"].ToString() switch
        {
            "save" => culture["Leerling.Reis.SaveFailed"],
            _ => null,
        };
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilReisSsrHtml.BuildBody(progress, def, culture, registry, afHtml, ReisAnswerPath, error, groep78, progress.JourneyThemeKey);
        return Html(http, culture["Leerling.Reis.Title"], body, "/leerling/reis", groep78, progress.JourneyThemeKey, af);
    }

    private static async Task<IResult> PostReisAnswerAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery af,
        CancellationToken ct)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect("/leerling/reis");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var itemId = form["ItemId"].ToString();
        if (!int.TryParse(form["Value"].ToString(), out var value))
        {
            return Results.Redirect("/leerling/reis?error=save");
        }

        try
        {
            var result = await api.SavePupilAnswerAsync(itemId, value, ct);
            if (result is null)
            {
                return Results.Redirect("/leerling/reis?error=save");
            }

            if (PupilLeerlingProgressRouting.RedirectAfterAnswer(result) is { } redir)
            {
                return Results.Redirect(redir);
            }
        }
        catch
        {
            return Results.Redirect("/leerling/reis?error=save");
        }

        return Results.Redirect("/leerling/reis");
    }

    private static async Task<IResult> GetEilandAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        if (string.Equals(http.Request.Query["part"], "1", StringComparison.Ordinal))
        {
            var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            return Html(http, culture["Leerling.Vo.PartBreak.DocumentTitle"], PupilEilandSsrHtml.PartBreak(culture, afHtml), "/leerling/eiland", false, null, af);
        }

        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return PlainError(culture["Leerling.Session.Lost"]);
        }

        if (PupilGroep78Navigation.ThemeRedirectPath(progress) is { } themeRedir)
        {
            return Results.Redirect(themeRedir);
        }

        if (PupilLeerlingProgressRouting.RedirectForEiland(progress) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var def = registry.Get(progress.QuestionSet);
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);
        var error = http.Request.Query["error"].ToString() == "save" ? culture["Leerling.Reis.SaveFailed"] : null;
        var body = PupilEilandSsrHtml.BuildBody(progress, def, culture, registry, PupilLeerlingSsrParts.AntiforgeryField(af, http), EilandSavePath, error, groep78, progress.JourneyThemeKey);
        return Html(http, culture["Leerling.Island.Title"], body, "/leerling/eiland", groep78, progress.JourneyThemeKey, af);
    }

    private static async Task<IResult> PostEilandAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IAntiforgery af, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType || !await af.IsRequestValidAsync(http))
        {
            return Results.Redirect("/leerling/eiland?error=save");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var likes = form["Likes"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var dislikes = form["Dislikes"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var likeOther = form["LikeOther"].ToString();
        var dislikeOther = form["DislikeOther"].ToString();
        try
        {
            var result = await api.SavePupilChipsAsync(new PupilChipsRequest(
                likes, dislikes,
                string.IsNullOrWhiteSpace(likeOther) ? null : likeOther.Trim(),
                string.IsNullOrWhiteSpace(dislikeOther) ? null : dislikeOther.Trim()), ct);
            if (result is null)
            {
                return Results.Redirect("/leerling/eiland?error=save");
            }

            var progress = await LoadProgressAsync(api, ct);
            var def = progress is null ? null : registry.Get(progress.QuestionSet);
            if (def?.PartBreakAfterIsland == true)
            {
                return Results.Redirect("/leerling/eiland?part=1");
            }

            return Results.Redirect("/leerling/reis");
        }
        catch
        {
            return Results.Redirect("/leerling/eiland?error=save");
        }
    }

    private static async Task<IResult> GetVakkenAsync(HttpContext http, JobsyApiClient api, CultureState culture, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return PlainError(culture["Leerling.Session.Lost"]);
        }

        if (PupilGroep78Navigation.ThemeRedirectPath(progress) is { } themeRedir)
        {
            return Results.Redirect(themeRedir);
        }

        if (PupilLeerlingProgressRouting.RedirectForVakken(progress) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var page = http.Request.Query["page"].ToString() == "2" ? 2 : 1;
        var scores = BuildVakScores(progress, page);
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);
        var body = PupilVakkenSsrHtml.BuildBody(progress, culture, page, scores, PupilLeerlingSsrParts.AntiforgeryField(af, http), VakkenSavePath, null, groep78);
        return Html(http, culture["Leerling.Vakken.Title"], body, "/leerling/vakken", groep78, progress.JourneyThemeKey, af);
    }

    private static async Task<IResult> PostVakkenAsync(HttpContext http, JobsyApiClient api, CultureState culture, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect("/leerling/vakken");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var page = form["Page"].ToString() == "2" ? 2 : 1;
        var keys = PupilVakScores.KeysForPage(page);
        var inputs = new List<PupilVakScoreInput>();
        foreach (var key in keys)
        {
            if (!int.TryParse(form[$"Leuk.{key}"].ToString(), out var leuk) || !int.TryParse(form[$"Goed.{key}"].ToString(), out var goed))
            {
                return Results.Redirect($"/leerling/vakken?page={page}&error=save");
            }

            inputs.Add(new PupilVakScoreInput(key, leuk, goed));
        }

        var result = await api.PutPupilVakScoresAsync(inputs, ct);
        if (result is null)
        {
            return Results.Redirect($"/leerling/vakken?page={page}&error=save");
        }

        if (page == 1)
        {
            return Results.Redirect("/leerling/vakken?page=2");
        }

        if (result.JourneyCompleted || string.Equals(result.NextStep, "done", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Redirect("/leerling/dit-ben-jij");
        }

        return Results.Redirect("/leerling/reis");
    }

    private static async Task<IResult> GetPuzzelAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IAntiforgery af, string key, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        if (!PupilPuzzleRoutes.IsBuilt(key))
        {
            return Html(http, culture["Leerling.Puzzle.Title"], """<section class="ll-card"><a class="btn-primary" href="/leerling/reis">Terug</a></section>""", $"/leerling/puzzel/{key}", false, null, af);
        }

        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return PlainError(culture["Leerling.Session.Lost"]);
        }

        if (PupilGroep78Navigation.ThemeRedirectPath(progress) is { } themeRedir)
        {
            return Results.Redirect(themeRedir);
        }

        if (PupilLeerlingProgressRouting.RedirectForPuzzle(progress, key) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);
        var savePath = $"/leerling/puzzel/{Uri.EscapeDataString(key)}/save";
        var body = PupilPuzzelSsrHtml.BuildBody(key, culture, PupilLeerlingSsrParts.AntiforgeryField(af, http), savePath, groep78, progress.JourneyThemeKey, registry, null);
        return Html(http, culture["Leerling.Puzzle.Title"], body, $"/leerling/puzzel/{key}", groep78, progress.JourneyThemeKey, af);
    }

    private static async Task<IResult> PostPuzzelAsync(HttpContext http, JobsyApiClient api, string key, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect($"/leerling/puzzel/{Uri.EscapeDataString(key)}");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var outcome = form["Outcome"].ToString();
        if (string.IsNullOrWhiteSpace(outcome))
        {
            outcome = "skipped";
        }

        var result = await api.SavePupilPuzzleAsync(key, new PupilPuzzleRequest(outcome), ct);
        if (result is null)
        {
            return Results.Redirect($"/leerling/puzzel/{Uri.EscapeDataString(key)}?error=save");
        }

        return Results.Redirect(PupilLeerlingProgressRouting.RedirectAfterPuzzle(result)!);
    }

    private static async Task<IResult> GetDitBenJijAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IFeatureFlags features, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        using var response = await api.GetPupilResultResponseAsync(ct);
        if (PupilLoadRetry.IsSessionLost(response.StatusCode))
        {
            return Results.Redirect("/leerling?error=expired");
        }

        if (!response.IsSuccessStatusCode)
        {
            var pending = """<section class="ll-card"><p class="ll-error" role="alert">"""+culture["Leerling.Reis.ResultPending"]+"""</p><a class="btn-primary" href="/leerling/dit-ben-jij">Opnieuw</a></section>""";
            return Html(http, culture["LeerlingStory.Title"], pending, "/leerling/dit-ben-jij", false, null, af);
        }

        var result = await response.Content.ReadFromJsonAsync<PupilResultPageDto>(JobsyApiClient.ApiJson, ct);
        if (result is null)
        {
            return PlainError(culture["Leerling.Reis.ResultPending"]);
        }

        var progress = await LoadProgressAsync(api, ct);
        var groep78 = progress?.QuestionSet == PupilQuestionSet.Groep78 && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);
        var flag = await features.GetAsync(ct);
        var passportV2 = flag.PupilPassportV2Enabled && result.QuestionSet == PupilQuestionSet.Groep78;
        var pdfHref = PupilStoryPdfLink.Href(null, PupilStoryPdfLink.NormalizeLanguageCode(culture.Language) ?? culture.Language);
        var body = PupilDitBenJijSsrHtml.BuildBody(result, culture, groep78, progress?.JourneyThemeKey, registry, passportV2, pdfHref);
        return Html(http, culture["LeerlingStory.Title"], body, "/leerling/dit-ben-jij", groep78, progress?.JourneyThemeKey, af);
    }

    private static async Task<IResult> GetPaspoortAsync(HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        await culture.InitializeAsync();
        var flag = await features.GetAsync(ct);
        if (!flag.PupilPassportV2Enabled)
        {
            var disabled = """<div class="ll-reis" data-testid="leerling-paspoort-disabled"><section class="ll-card"><a class="btn-primary" href="/leerling/dit-ben-jij">Terug</a></section></div>""";
            return Html(http, culture["LeerlingPassport.Page.Title"], disabled, "/leerling/paspoort", false, null, af);
        }

        var lang = PupilStoryPdfLink.NormalizeLanguageCode(culture.Language) ?? culture.Language;
        var naam = http.Request.Query["naam"].ToString();
        var model = await api.GetPupilG78PassportAsync(string.IsNullOrWhiteSpace(naam) ? null : naam, lang, ct);
        if (model is null || !model.Enabled)
        {
            return Html(http, culture["LeerlingPassport.Page.Title"], """<section class="ll-card"><a href="/leerling/dit-ben-jij">Terug</a></section>""", "/leerling/paspoort", false, null, af);
        }

        var pdfHref = PupilStoryPdfLink.Href(naam, lang);
        var body = PupilPaspoortSsrHtml.BuildPage(culture, model, naam, pdfHref);
        return Html(http, culture["LeerlingPassport.Page.Title"], body, "/leerling/paspoort", false, null, af);
    }

    private static async Task<IResult> GetVoPaspoortAsync(HttpContext http, JobsyApiClient api, CultureState culture, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var dto = await api.GetPupilVoPassportAsync(ct);
        if (dto?.Enabled != true)
        {
            return Results.Redirect("/leerling/droombaan");
        }

        var model = new PupilVoPaspoortFormModel(
            string.Join(", ", dto.FavoriteSubjects),
            dto.ProfileKey ?? "",
            string.Join(", ", dto.SideJobAvailability),
            dto.TravelModeKey ?? "",
            string.Join("\n", dto.LearningGoals),
            dto.OwnWords ?? "");
        var body = PupilVoPaspoortSsrHtml.BuildBody(culture, PupilLeerlingSsrParts.AntiforgeryField(af, http), VoPaspoortSavePath, model, null);
        return Html(http, culture["LeerlingPassport.Vo.Form.Title"], body, "/leerling/vo-paspoort", false, null, af);
    }

    private static async Task<IResult> PostVoPaspoortAsync(HttpContext http, JobsyApiClient api, CultureState culture, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect("/leerling/vo-paspoort");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var req = new PupilVoPassportSaveRequest(
            SplitCsv(form["Subjects"].ToString()),
            string.IsNullOrWhiteSpace(form["ProfileKey"].ToString()) ? null : form["ProfileKey"].ToString(),
            SplitCsv(form["SideJob"].ToString()),
            string.IsNullOrWhiteSpace(form["TravelKey"].ToString()) ? null : form["TravelKey"].ToString(),
            string.IsNullOrWhiteSpace(form["OwnWords"].ToString()) ? null : form["OwnWords"].ToString(),
            SplitLines(form["LearningGoals"].ToString()));
        var ok = await api.SavePupilVoPassportAsync(req, ct);
        return Results.Redirect(ok is null ? "/leerling/vo-paspoort?error=save" : "/leerling/droombaan");
    }

    private static async Task<IResult> GetMboUitlegAsync(HttpContext http, CultureState culture, IAntiforgery af)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var body = PupilMboHboSsrHtml.Uitleg(PupilLeerlingSsrParts.AntiforgeryField(af, http), MboIntroPath);
        return MboHtml(http, "Ontdek wat bij je past", body, "/leerling/mbo-hbo/uitleg");
    }

    private static async Task<IResult> PostMboIntroAsync(HttpContext http, JobsyApiClient api, IAntiforgery af, CancellationToken ct)
    {
        if (!await af.IsRequestValidAsync(http))
        {
            return Results.Redirect("/leerling/mbo-hbo/uitleg");
        }

        var res = await api.MarkMboHboIntroSeenAsync(ct);
        return Results.Redirect(res?.RedirectPath ?? PupilMboHboEntryPaths.Overzicht);
    }

    private static async Task<IResult> GetMboOverzichtAsync(HttpContext http, JobsyApiClient api, CultureState culture, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var model = await api.GetMboHboOverviewAsync(ct);
        var body = model is null ? "<div class=\"mhbo-wrap\"><p>Kon niet laden.</p></div>" : PupilMboHboSsrHtml.Overzicht(model);
        return MboHtml(http, "Jouw testen", body, "/leerling/mbo-hbo/overzicht");
    }

    private static async Task<IResult> GetMboTestAsync(HttpContext http, JobsyApiClient api, string blockKey, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var q = await api.GetMboHboQuestionAsync(blockKey, itemId: null, ct);
        var body = q is null ? "<div class=\"mhbo-wrap\"><p>Vraag niet gevonden.</p></div>" : PupilMboHboSsrHtml.TestQuestion(q, blockKey, PupilLeerlingSsrParts.AntiforgeryField(af, http), $"/leerling/mbo-hbo/test/{Uri.EscapeDataString(blockKey)}/answer");
        return MboHtml(http, "Test", body, $"/leerling/mbo-hbo/test/{blockKey}");
    }

    private static async Task<IResult> PostMboTestAsync(HttpContext http, JobsyApiClient api, string blockKey, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect($"/leerling/mbo-hbo/test/{Uri.EscapeDataString(blockKey)}");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var itemId = form["ItemId"].ToString();
        PupilMboHboAnswerResponse? res;
        if (form.ContainsKey("Leuk") && form.ContainsKey("Goed"))
        {
            _ = int.TryParse(form["Leuk"].ToString(), out var leuk);
            _ = int.TryParse(form["Goed"].ToString(), out var goed);
            res = await api.SaveMboHboVakAnswerAsync(itemId, leuk, goed, ct);
        }
        else
        {
            _ = int.TryParse(form["Value"].ToString(), out var val);
            res = await api.SaveMboHboAnswerAsync(blockKey, itemId, val, ct);
        }

        if (res is null)
        {
            return Results.Redirect($"/leerling/mbo-hbo/test/{Uri.EscapeDataString(blockKey)}");
        }

        if (res.BlockComplete)
        {
            return Results.Redirect(PupilMboHboEntryPaths.Uitkomst(PupilMboHboEntryPaths.TryParseBlockRoute(blockKey, out var b) ? b : PupilMboHboBlock.Interesse));
        }

        return Results.Redirect(string.IsNullOrEmpty(res.NextItemId)
            ? PupilMboHboEntryPaths.Overzicht
            : $"/leerling/mbo-hbo/test/{Uri.EscapeDataString(blockKey)}?itemId={Uri.EscapeDataString(res.NextItemId)}");
    }

    private static async Task<IResult> GetMboUitkomstAsync(HttpContext http, JobsyApiClient api, string blockKey, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var model = await api.GetMboHboOutcomeAsync(blockKey, ct);
        var body = model is null ? "<div class=\"mhbo-wrap\"><p>Laden…</p></div>" : PupilMboHboSsrHtml.Uitkomst(model);
        return MboHtml(http, "Uitkomst", body, $"/leerling/mbo-hbo/uitkomst/{blockKey}");
    }

    private static async Task<IResult> GetMboPaspoortAsync(HttpContext http, JobsyApiClient api, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var model = await api.GetMboHboPassportAsync(ct);
        var body = model is null ? "<div class=\"mhbo-wrap\"><p>Laden…</p></div>" : PupilMboHboSsrHtml.Paspoort(model);
        return MboHtml(http, "Jouw Lobsy-paspoort", body, "/leerling/mbo-hbo/paspoort");
    }

    private static async Task<PupilProgressStateDto?> LoadProgressAsync(JobsyApiClient api, CancellationToken ct)
    {
        try
        {
            return await api.GetPupilProgressAsync(ct);
        }
        catch (HttpRequestException ex) when (PupilLoadRetry.IsSessionLost(ex.StatusCode))
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, (int Leuk, int Goed)> BuildVakScores(PupilProgressStateDto _, int page)
    {
        var dict = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in PupilVakScores.KeysForPage(page))
        {
            dict[key] = (0, 0);
        }

        return dict;
    }

    private static IResult Html(HttpContext http, string title, string body, string route, bool useG78, string? themeKey, IAntiforgery af, bool showPause = true)
    {
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var doc = PupilLeerlingSsrFrame.RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = title,
            BodyHtml = body,
            Pupil = http.User,
            RoutePath = route,
            UseGroep78Shell = useG78,
            JourneyThemeKey = themeKey,
            ShowPause = showPause,
            PauseFormHtml = PupilLeerlingSsrFrame.PauseForm(afHtml, "Pauze"),
        });
        return Results.Content(doc, "text/html; charset=utf-8");
    }

    private static IResult MboHtml(HttpContext http, string title, string body, string route)
    {
        var doc = PupilLeerlingSsrFrame.RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = title,
            BodyHtml = body,
            Pupil = http.User,
            RoutePath = route,
            UseMboHboShell = true,
            ShowPause = false,
        });
        return Results.Content(doc, "text/html; charset=utf-8");
    }

    private static IResult PlainError(string message)
        => Results.Content(message, "text/plain; charset=utf-8", statusCode: 503);

    private static IReadOnlyList<string> SplitCsv(string raw)
        => raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static IReadOnlyList<string> SplitLines(string raw)
        => raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
