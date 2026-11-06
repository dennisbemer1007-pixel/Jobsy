using System.Globalization;
using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET/POST for teacher test window (no Blazor).</summary>
public static class TeacherPortalTestWindowEndpoints
{
    public static IEndpointRouteBuilder MapTeacherPortalTestWindowEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" };

        app.MapGet("/leraar/klas/{classId:guid}/testvenster", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, IAntiforgery antiforgery, CancellationToken ct)
            => RenderGetAsync(http, api, culture, features, antiforgery, classId, ct))
            .RequireAuthorization(auth);

        app.MapPost("/leraar/klas/{classId:guid}/testvenster", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IAntiforgery antiforgery, CancellationToken ct)
            => PostWindowAsync(http, api, culture, antiforgery, classId, ct))
            .RequireAuthorization(auth);

        app.MapPost("/leraar/klas/{classId:guid}/testvenster/clear-pause", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IAntiforgery antiforgery, CancellationToken ct)
            => PostClearPauseAsync(http, api, culture, antiforgery, classId, ct))
            .RequireAuthorization(auth);

        return app;
    }

    private static async Task<IResult> RenderGetAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        var path = TeacherTestWindowSsrHtml.PagePath(classId);
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["School.TestWindow.Title"],
            async () =>
            {
                TeacherClassOverviewDto? overview;
                try
                {
                    overview = await api.GetTeacherOverviewAsync(classId, ct);
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }
                catch
                {
                    return ErrorBody(culture["Leraar.LoadFailed"]);
                }

                if (overview is null)
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }

                var windowError = ResolveQueryError(http.Request.Query["error"].ToString());
                var pauseError = ResolveQueryError(http.Request.Query["pauseError"].ToString());
                var tokens = antiforgery.GetAndStoreTokens(http);
                var tokenHtml = AntiforgeryField(tokens);
                return TeacherTestWindowSsrHtml.BuildBody(
                    classId,
                    overview,
                    windowError,
                    pauseError,
                    tokenHtml,
                    tokenHtml,
                    culture);
            },
            ct);
    }

    private static async Task<IResult> PostWindowAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId, error: culture["Common.Error.TryAgain"]));
        }

        if (!http.Request.HasFormContentType)
        {
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var action = form["action"].ToString().Trim();
        DateOnly? closesOn = null;
        var closesRaw = form["closesOn"].ToString();
        if (!string.IsNullOrWhiteSpace(closesRaw)
            && DateOnly.TryParse(closesRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            closesOn = parsed;
        }

        try
        {
            _ = await api.SetTeacherTestWindowAsync(classId, action, closesOn, ct);
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId));
        }
        catch (Exception ex)
        {
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId, error: PortalActionError.From(ex.Message)));
        }
    }

    private static async Task<IResult> PostClearPauseAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId, pauseError: culture["Common.Error.TryAgain"]));
        }

        try
        {
            await api.ClearTeacherLoginPauseAsync(classId, ct);
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId));
        }
        catch (Exception ex)
        {
            return SeeOther(TeacherTestWindowSsrHtml.PagePath(classId, pauseError: PortalActionError.From(ex.Message)));
        }
    }

    private static string? ResolveQueryError(string raw)
        => string.IsNullOrWhiteSpace(raw) ? null : PortalActionError.From(raw);

    private static async Task<IResult> RenderAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        string path,
        string pageTitle,
        Func<Task<string>> buildBodyAsync,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);

        if (!await SchoolsFeatureGate.IsEnabledAsync(features, ct))
        {
            var shellOff = await ScholenPortalShell.BuildAsync(http, culture, api, features, path);
            return Results.Content(
                ScholenPortalSsrFrame.RenderDocument(pageTitle, "", shellOff, culture, path),
                "text/html; charset=utf-8");
        }

        var body = await buildBodyAsync();
        var shell = await ScholenPortalShell.BuildAsync(http, culture, api, features, path);
        return Results.Content(
            ScholenPortalSsrFrame.RenderDocument(pageTitle, body, shell, culture, path),
            "text/html; charset=utf-8");
    }

    private static string AntiforgeryField(AntiforgeryTokenSet tokens)
        => tokens.RequestToken is { } token
            ? $"""<input type="hidden" name="{WebUtility.HtmlEncode(tokens.FormFieldName)}" value="{WebUtility.HtmlEncode(token)}" />"""
            : "";

    private static IResult SeeOther(string url) => Results.Redirect(url, permanent: false, preserveMethod: false);

    private static string ErrorBody(string message)
        => $"""<p class="state-message error">{WebUtility.HtmlEncode(message)}</p>""";
}
