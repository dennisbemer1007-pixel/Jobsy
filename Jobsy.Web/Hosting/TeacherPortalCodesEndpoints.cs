using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET/POST for teacher class codes (no Blazor).</summary>
public static class TeacherPortalCodesEndpoints
{
    public static IEndpointRouteBuilder MapTeacherPortalCodesEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" };

        app.MapGet("/leraar/klas/{classId:guid}/codes", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, IAntiforgery antiforgery, CancellationToken ct)
            => RenderGetAsync(http, api, culture, features, antiforgery, classId, ct))
            .RequireAuthorization(auth);

        app.MapPost("/leraar/klas/{classId:guid}/codes/add", (Guid classId, HttpContext http, JobsyApiClient api, IAntiforgery antiforgery, CancellationToken ct)
            => PostAddAsync(http, api, antiforgery, classId, ct))
            .RequireAuthorization(auth);

        app.MapPost("/leraar/klas/{classId:guid}/codes/{codeId:guid}/replace", (Guid classId, Guid codeId, HttpContext http, JobsyApiClient api, IAntiforgery antiforgery, CancellationToken ct)
            => PostReplaceAsync(http, api, antiforgery, classId, codeId, ct))
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
        var (query, status, sort, sortDesc) = ParseListQuery(http);
        var path = TeacherCodesSsrHtml.ListPath(classId, query, status, sort, sortDesc);
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["Leraar.Codes.Title"],
            async () =>
            {
                IReadOnlyList<TeacherCodeRowDto> rows;
                try
                {
                    rows = await api.GetTeacherCodesAsync(classId, ct);
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }
                catch
                {
                    return ErrorBody(culture["Leraar.LoadFailed"]);
                }

                var success = ResolveSuccessMessage(http);
                var error = ResolveErrorMessage(http, culture);
                var tokens = antiforgery.GetAndStoreTokens(http);
                var tokenHtml = AntiforgeryField(tokens);
                var body = TeacherCodesSsrHtml.BuildBody(
                    classId,
                    rows,
                    query,
                    status,
                    sort,
                    sortDesc,
                    success,
                    error,
                    tokenHtml,
                    tokenHtml,
                    culture);
                return body;
            },
            ct);
    }

    private static async Task<IResult> PostAddAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        var (query, status, sort, sortDesc) = ParseListQuery(http);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return SeeOther(TeacherCodesSsrHtml.ListPath(classId, query, status, sort, sortDesc, error: "retry"));
        }

        try
        {
            var before = (await api.GetTeacherCodesAsync(classId, ct)).Select(c => c.Id).ToHashSet();
            var detail = await api.AddTeacherCodesAsync(classId, 1, ct);
            string? addedCode = null;
            if (detail?.Codes is { Count: > 0 } codes)
            {
                var added = codes.FirstOrDefault(c => !before.Contains(c.Id))
                            ?? codes.OrderByDescending(c => c.Number).First();
                addedCode = added.DisplayCode;
            }

            return SeeOther(TeacherCodesSsrHtml.ListPath(
                classId,
                query,
                status,
                sort,
                sortDesc,
                flash: "added",
                flashCode: addedCode));
        }
        catch (Exception ex)
        {
            return SeeOther(TeacherCodesSsrHtml.ListPath(
                classId,
                query,
                status,
                sort,
                sortDesc,
                error: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> PostReplaceAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        Guid codeId,
        CancellationToken ct)
    {
        var (query, status, sort, sortDesc) = ParseListQuery(http);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return SeeOther(TeacherCodesSsrHtml.ListPath(classId, query, status, sort, sortDesc, error: "retry"));
        }

        try
        {
            _ = await api.ReplaceTeacherCodeAsync(classId, codeId, ct);
            return SeeOther(TeacherCodesSsrHtml.ListPath(classId, query, status, sort, sortDesc));
        }
        catch (Exception ex)
        {
            return SeeOther(TeacherCodesSsrHtml.ListPath(
                classId,
                query,
                status,
                sort,
                sortDesc,
                error: PortalActionError.FromException(ex)));
        }
    }

    private static (string Query, PupilCodeStatus? Status, string Sort, bool SortDesc) ParseListQuery(HttpContext http)
    {
        var query = http.Request.Query["q"].ToString();
        PupilCodeStatus? status = null;
        var statusRaw = http.Request.Query["status"].ToString();
        if (!string.IsNullOrWhiteSpace(statusRaw)
            && Enum.TryParse<PupilCodeStatus>(statusRaw, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var sort = http.Request.Query["sort"].ToString();
        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "num";
        }

        var descRaw = http.Request.Query["desc"].ToString();
        var sortDesc = descRaw is "1" or "true" or "yes";
        return (query, status, sort, sortDesc);
    }

    private static string? ResolveSuccessMessage(HttpContext http)
    {
        return http.Request.Query["flash"].ToString() switch
        {
            "added" when http.Request.Query["code"].ToString() is { Length: > 0 } code
                => $"Code toegevoegd: {code}",
            "added" => "Code toegevoegd.",
            _ => null
        };
    }

    private static string? ResolveErrorMessage(HttpContext http, CultureState cultureState)
    {
        var error = http.Request.Query["error"].ToString();
        if (string.IsNullOrEmpty(error))
        {
            return null;
        }

        return error switch
        {
            "retry" => cultureState["Common.Error.TryAgain"],
            _ => PortalActionError.From(error)
        };
    }

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
