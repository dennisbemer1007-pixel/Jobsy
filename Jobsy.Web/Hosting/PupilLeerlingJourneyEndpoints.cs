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

        app.MapGet("/leerling/verder", GetVerderAsync).RequireAuthorization(pupil);
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

    private static async Task<IResult> GetVerderAsync(HttpContext http, JobsyApiClient api, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var progress = await LoadProgressAsync(api, ct);
        if (progress is null)
        {
            return Results.Redirect("/leerling?error=expired");
        }

        return Results.Redirect(PupilGroep78EntryPaths.AfterLogin(progress, progress.Completed));
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

        if (PupilGroep78Navigation.ThemeRedirectPath(progress) is { } themeRedir)
        {
            return Results.Redirect(themeRedir);
        }

        if (PupilLeerlingProgressRouting.RedirectForStart(progress) is { } redir)
        {
            return Results.Redirect(redir);
        }

        var def = registry.Get(progress.QuestionSet);
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress);
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilStartSsrHtml.BuildBody(def, culture, groep78, progress.JourneyThemeKey, registry);
        var title = groep78 ? culture["Leerling.G78.Route.Title"] : culture["Leerling.Start.Title"];
        return Html(http, title, body, "/leerling/start", groep78, progress.JourneyThemeKey, afHtml, progress: progress);
    }

    private static async Task<IResult> GetStopAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        IAntiforgery af,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var done = http.Request.Query["done"].ToString();
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var progress = await LoadProgressAsync(api, ct);
        var themeFromQuery = http.Request.Query["theme"].ToString();
        var g78FromQuery = string.Equals(http.Request.Query["g78"], "1", StringComparison.Ordinal);
        var useG78 = g78FromQuery
                     || (progress?.QuestionSet == PupilQuestionSet.Groep78
                         && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress));
        var themeKey = PupilJourneyTheme.IsValid(progress?.JourneyThemeKey)
            ? progress!.JourneyThemeKey
            : PupilJourneyTheme.IsValid(themeFromQuery) ? themeFromQuery : null;
        var journey = progress is not null
            ? PupilGroep78JourneySnapshot.FromProgress(progress, culture, registry, themeKey)
            : PupilGroep78JourneySnapshot.FromPauseQuery(http.Request.Query, culture, registry, themeKey);
        var body = PupilStopSsrHtml.BuildBody(culture, done, afHtml, useG78, themeKey, registry, journey);
        var doc = PupilLeerlingSsrFrame.RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = string.IsNullOrEmpty(done) ? culture["Leerling.Stop.AskTitle"] : culture["Leerling.Stop.Title"],
            BodyHtml = body,
            Pupil = http.User,
            RoutePath = "/leerling/stop",
            UseGroep78Shell = useG78,
            JourneyThemeKey = themeKey,
            ShowPause = false,
        });
        return Results.Content(doc, "text/html; charset=utf-8");
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
        var savedTheme = PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress)
            ? progress.JourneyThemeKey
            : null;
        var body = PupilWereldSsrHtml.BuildBody(culture, afHtml, savedTheme, WereldSavePath);
        return Html(http, culture["Leerling.Wereld.Title"], body, "/leerling/wereld", useG78: true, savedTheme, afHtml, showPause: false);
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
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress);
        if (!progress.WindowOpen && !progress.Completed)
        {
            var closedAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            var closed = PupilReisSsrHtml.WindowClosed(culture, progress.QuestionSet);
            return Html(http, culture["Leerling.Reis.Title"], closed, "/leerling/reis", groep78, progress.JourneyThemeKey, closedAf, progress: progress);
        }

        if (progress.Completed)
        {
            var doneAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            string doneBody;
            try
            {
                doneBody = PupilReisSsrHtml.DoneCard(culture, groep78, progress.JourneyThemeKey, registry);
            }
            catch
            {
                doneBody = PupilReisSsrHtml.DoneCard(culture, groep78: false);
            }

            return Html(
                http,
                culture["Leerling.Reis.Title"],
                doneBody,
                "/leerling/reis",
                groep78,
                progress.JourneyThemeKey,
                doneAf,
                progress: progress);
        }

        var error = http.Request.Query["error"].ToString() switch
        {
            "save" => culture["Leerling.Reis.SaveFailed"],
            _ => null,
        };
        var display = progress;
        if (string.Equals(http.Request.Query["prev"], "1", StringComparison.Ordinal))
        {
            display = PupilReisReview.WithPreviousQuestion(progress, def) ?? progress;
        }

        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        string body;
        try
        {
            body = PupilReisSsrHtml.BuildBody(display, def, culture, registry, afHtml, ReisAnswerPath, error, groep78, progress.JourneyThemeKey);
        }
        catch
        {
            body = groep78
                ? $"""<section class="ll-card"><p class="ll-error" role="alert">{PupilLeerlingSsrParts.E(culture["Leerling.Reis.SaveFailed"])}</p><a class="btn-primary" href="/leerling/reis">Opnieuw</a></section>"""
                : PupilReisSsrHtml.WindowClosed(culture, progress.QuestionSet);
        }

        return Html(http, culture["Leerling.Reis.Title"], body, "/leerling/reis", groep78, progress.JourneyThemeKey, afHtml, progress: display);
    }

    private static async Task<IResult> PostReisAnswerAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery af,
        CancellationToken ct)
    {
        var wantsJson = PupilLeerlingClientJson.WantsJson(http);
        if (!http.Request.HasFormContentType || !await af.IsRequestValidAsync(http))
        {
            return ReisAnswerSaveFailed(culture, wantsJson, StatusCodes.Status400BadRequest);
        }

        var form = await http.Request.ReadFormAsync(ct);
        var itemId = form["ItemId"].ToString();
        if (!int.TryParse(form["Value"].ToString(), out var value))
        {
            return ReisAnswerSaveFailed(culture, wantsJson, StatusCodes.Status400BadRequest);
        }

        try
        {
            var (result, apiStatus, errorBody) = await api.SavePupilAnswerAsync(itemId, value, ct);
            if (result is null)
            {
                var status = MapReisAnswerApiFailureStatus(apiStatus);
                var recoveryRedirect = ReisAnswerRecoveryRedirect(errorBody);
                return ReisAnswerSaveFailed(culture, wantsJson, status, recoveryRedirect);
            }

            var redirect = PupilLeerlingProgressRouting.RedirectAfterAnswer(result) ?? "/leerling/reis";
            if (wantsJson)
            {
                return ReisAnswerSaveJsonOk(http, af, redirect);
            }

            return Results.Redirect(redirect);
        }
        catch
        {
            return ReisAnswerSaveFailed(culture, wantsJson, StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Never surface HTTP 500 on answer save — map API/server faults to 502 or the API's 4xx.</summary>
    private static int MapReisAnswerApiFailureStatus(int apiStatus)
    {
        if (apiStatus is >= 400 and < 500)
        {
            return apiStatus;
        }

        return StatusCodes.Status502BadGateway;
    }

    private static IResult ReisAnswerSaveJsonOk(HttpContext http, IAntiforgery af, string redirectPath)
    {
        var tokens = af.GetAndStoreTokens(http);
        return Results.Json(new PupilReisAnswerSaveJson(
            Ok: true,
            RedirectPath: redirectPath,
            Error: null,
            Status: StatusCodes.Status200OK,
            AntiforgeryField: tokens.FormFieldName,
            AntiforgeryToken: tokens.RequestToken));
    }

    private static IResult ReisAnswerSaveFailed(
        CultureState culture,
        bool wantsJson,
        int status,
        string? recoveryRedirect = null)
    {
        if (wantsJson)
        {
            return Results.Json(
                new PupilReisAnswerSaveJson(
                    Ok: false,
                    RedirectPath: recoveryRedirect,
                    Error: culture["Leerling.Reis.SaveFailed"],
                    Status: status),
                statusCode: status);
        }

        return Results.Redirect(string.IsNullOrEmpty(recoveryRedirect)
            ? "/leerling/reis?error=save"
            : recoveryRedirect);
    }

    private static string? ReisAnswerRecoveryRedirect(string? errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody))
        {
            return null;
        }

        try
        {
            var err = System.Text.Json.JsonSerializer.Deserialize<PupilErrorDto>(errorBody, JobsyApiClient.ApiJson);
            if (err is null)
            {
                return null;
            }

            return err.NextStep switch
            {
                "puzzle" when PupilPuzzleRoutes.IsBuilt(err.NextPuzzleKey)
                    => $"/leerling/puzzel/{Uri.EscapeDataString(err.NextPuzzleKey!)}",
                "island" => "/leerling/eiland",
                "vakken" => "/leerling/vakken",
                _ => "/leerling/reis",
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<IResult> GetEilandAsync(HttpContext http, JobsyApiClient api, CultureState culture, IPupilQuestionSetRegistry registry, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        if (string.Equals(http.Request.Query["part"], "1", StringComparison.Ordinal))
        {
            var partAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            return Html(http, culture["Leerling.Vo.PartBreak.DocumentTitle"], PupilEilandSsrHtml.PartBreak(culture, partAf), "/leerling/eiland", false, null, partAf);
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
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress);
        var error = http.Request.Query["error"].ToString() == "save" ? culture["Leerling.Reis.SaveFailed"] : null;
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilEilandSsrHtml.BuildBody(progress, def, culture, registry, afHtml, EilandSavePath, error, groep78, progress.JourneyThemeKey);
        return Html(http, culture["Leerling.Island.Title"], body, "/leerling/eiland", groep78, progress.JourneyThemeKey, afHtml, progress: progress);
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
        foreach (var key in likes)
        {
            dislikes.RemoveAll(d => string.Equals(d, key, StringComparison.OrdinalIgnoreCase));
        }
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

    private static async Task<IResult> GetVakkenAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery af,
        IPupilQuestionSetRegistry registry,
        CancellationToken ct)
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
        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress);
        var themeKey = groep78 ? PupilJourneyTheme.NormalizeOrDefault(progress.JourneyThemeKey) : null;
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilVakkenSsrHtml.BuildBody(progress, culture, page, scores, afHtml, VakkenSavePath, null, groep78, themeKey, registry);
        return Html(http, culture["Leerling.Vakken.Title"], body, "/leerling/vakken", groep78, themeKey, afHtml, progress: progress);
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
            var missingAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            return Html(http, culture["Leerling.Puzzle.Title"], """<section class="ll-card"><a class="btn-primary" href="/leerling/reis">Terug</a></section>""", $"/leerling/puzzel/{key}", false, null, missingAf);
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

        var groep78 = progress.QuestionSet == PupilQuestionSet.Groep78 && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress);
        var savePath = $"/leerling/puzzel/{Uri.EscapeDataString(key)}/save";
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilPuzzelSsrHtml.BuildBody(key, culture, afHtml, savePath, groep78, progress.JourneyThemeKey, registry, null);
        return Html(http, culture["Leerling.Puzzle.Title"], body, $"/leerling/puzzel/{key}", groep78, progress.JourneyThemeKey, afHtml, progress: progress);
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
            var pendingAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            return Html(http, culture["LeerlingStory.Title"], pending, "/leerling/dit-ben-jij", false, null, pendingAf);
        }

        var result = await response.Content.ReadFromJsonAsync<PupilResultPageDto>(JobsyApiClient.ApiJson, ct);
        if (result is null)
        {
            return PlainError(culture["Leerling.Reis.ResultPending"]);
        }

        var progress = await LoadProgressAsync(api, ct);
        var groep78 = progress?.QuestionSet == PupilQuestionSet.Groep78 && progress is not null && PupilGroep78EntryPaths.HasExplicitJourneyThemeChoice(progress);
        var flag = await features.GetAsync(ct);
        var passportV2 = flag.PupilPassportV2Enabled && result.QuestionSet == PupilQuestionSet.Groep78;
        var pdfHref = PupilStoryPdfLink.Href(null, PupilStoryPdfLink.NormalizeLanguageCode(culture.Language) ?? culture.Language);
        var body = PupilDitBenJijSsrHtml.BuildBody(result, culture, groep78, progress?.JourneyThemeKey, registry, passportV2, pdfHref);
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        return Html(http, culture["LeerlingStory.Title"], body, "/leerling/dit-ben-jij", groep78, progress?.JourneyThemeKey, afHtml, progress: progress);
    }

    private static async Task<IResult> GetPaspoortAsync(HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, IAntiforgery af, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var progress = await LoadProgressAsync(api, ct);
        var flag = await features.GetAsync(ct);
        if (!flag.PupilPassportV2Enabled)
        {
            var (disabledShell, disabledTheme) = ResolvePaspoortG78Shell(progress, g78PassportContent: false);
            var disabled = """<div class="ll-reis" data-testid="leerling-paspoort-disabled"><section class="ll-card"><a class="btn-primary" href="/leerling/dit-ben-jij">Terug</a></section></div>""";
            var disabledAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            return Html(http, culture["LeerlingPassport.Page.Title"], disabled, "/leerling/paspoort", disabledShell, disabledTheme, disabledAf);
        }

        var lang = PupilStoryPdfLink.NormalizeLanguageCode(culture.Language) ?? culture.Language;
        var naam = http.Request.Query["naam"].ToString();
        var model = await api.GetPupilG78PassportAsync(string.IsNullOrWhiteSpace(naam) ? null : naam, lang, ct);
        if (model is null || !model.Enabled)
        {
            var (emptyShell, emptyTheme) = ResolvePaspoortG78Shell(progress, g78PassportContent: false);
            var emptyAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
            return Html(http, culture["LeerlingPassport.Page.Title"], """<section class="ll-card"><a href="/leerling/dit-ben-jij">Terug</a></section>""", "/leerling/paspoort", emptyShell, emptyTheme, emptyAf);
        }

        var (useG78Shell, themeKey) = ResolvePaspoortG78Shell(progress, g78PassportContent: true);
        var pdfHref = PupilStoryPdfLink.Href(naam, lang);
        var body = PupilPaspoortSsrHtml.BuildPage(culture, model, naam, pdfHref);
        var pasAf = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        return Html(http, culture["LeerlingPassport.Page.Title"], body, "/leerling/paspoort", useG78Shell, themeKey, pasAf);
    }

    /// <summary>G78 passport SSR always uses the journey shell; theme defaults when unset (e.g. legacy progress rows).</summary>
    private static (bool UseGroep78Shell, string? JourneyThemeKey) ResolvePaspoortG78Shell(
        PupilProgressStateDto? progress,
        bool g78PassportContent)
    {
        var groep78Pupil = progress?.QuestionSet == PupilQuestionSet.Groep78;
        if (!groep78Pupil && !g78PassportContent)
        {
            return (false, null);
        }

        return (true, PupilJourneyTheme.NormalizeOrDefault(progress?.JourneyThemeKey));
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
        var afHtml = PupilLeerlingSsrParts.AntiforgeryField(af, http);
        var body = PupilVoPaspoortSsrHtml.BuildBody(culture, afHtml, VoPaspoortSavePath, model, null);
        return Html(http, culture["LeerlingPassport.Vo.Form.Title"], body, "/leerling/vo-paspoort", false, null, afHtml);
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

    private static IResult Html(
        HttpContext http,
        string title,
        string body,
        string route,
        bool useG78,
        string? themeKey,
        string pauseAntiforgeryHtml,
        bool showPause = true,
        PupilProgressStateDto? progress = null)
    {
        string pauseHtml = "";
        if (showPause)
        {
            var pauseFields = PupilGroep78JourneySnapshot.PauseFormFields(progress);
            pauseHtml = PupilLeerlingSsrFrame.PauseForm(
                pauseAntiforgeryHtml,
                "Pauze",
                groep78: useG78,
                journeyThemeKey: themeKey,
                journeyNodeKey: pauseFields?.NodeKey,
                answeredCount: pauseFields?.Answered,
                totalItems: pauseFields?.Total,
                journeyCompleted: pauseFields?.Completed ?? false);
        }

        var doc = PupilLeerlingSsrFrame.RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = title,
            BodyHtml = body,
            Pupil = http.User,
            RoutePath = route,
            UseGroep78Shell = useG78,
            JourneyThemeKey = themeKey,
            ShowPause = showPause,
            PauseFormHtml = pauseHtml,
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
