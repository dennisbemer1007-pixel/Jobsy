using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET routes for read-only teacher class pages (no Blazor).</summary>
public static class TeacherPortalReadOnlyEndpoints
{
    public static IEndpointRouteBuilder MapTeacherPortalReadOnlyEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" };

        app.MapGet("/leraar/klas/{classId:guid}/groep", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, CancellationToken ct)
            => RenderGroupAsync(http, api, culture, features, classId, ct))
            .RequireAuthorization(auth);

        app.MapGet("/leraar/klas/{classId:guid}/droombanen", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, CancellationToken ct)
            => RenderDreamJobsAsync(http, api, culture, features, classId, ct))
            .RequireAuthorization(auth);

        app.MapGet("/leraar/klas/{classId:guid}/materiaal", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, CancellationToken ct)
            => RenderMaterialsAsync(http, api, culture, features, classId, ct))
            .RequireAuthorization(auth);

        app.MapGet("/leraar/klas/{classId:guid}/mbo-hbo", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, CancellationToken ct)
            => RenderMboHboAsync(http, api, culture, features, classId, ct))
            .RequireAuthorization(auth);

        return app;
    }

    private static async Task<IResult> RenderGroupAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        Guid classId,
        CancellationToken ct)
    {
        var path = $"/leraar/klas/{classId:D}/groep";
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["Leraar.Group.Title"],
            async () =>
            {
                TeacherGroupInsightsDto? group;
                try
                {
                    group = await api.GetTeacherGroupAsync(classId, ct);
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }
                catch
                {
                    return ErrorBody(culture["Leraar.LoadFailed"]);
                }

                if (group is null)
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }

                return TeacherPortalReadOnlySsrHtml.BuildGroupBody(classId, group, culture);
            },
            ct);
    }

    private static async Task<IResult> RenderDreamJobsAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        Guid classId,
        CancellationToken ct)
    {
        var path = $"/leraar/klas/{classId:D}/droombanen";
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["Leraar.DreamJobs.Title"],
            async () =>
            {
                TeacherDreamJobsDto? dto;
                try
                {
                    dto = await api.GetTeacherDreamJobsAsync(classId, ct);
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }
                catch
                {
                    return ErrorBody(culture["Leraar.LoadFailed"]);
                }

                if (dto is null)
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }

                return TeacherPortalReadOnlySsrHtml.BuildDreamJobsBody(classId, dto, culture);
            },
            ct);
    }

    private static async Task<IResult> RenderMaterialsAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        Guid classId,
        CancellationToken ct)
    {
        var path = $"/leraar/klas/{classId:D}/materiaal";
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["Leraar.Material.Title"],
            async () =>
            {
                try
                {
                    var overview = await api.GetTeacherOverviewAsync(classId, ct);
                    if (overview is null)
                    {
                        return ErrorBody(culture["Leraar.Class.NotFound"]);
                    }
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return ErrorBody(culture["Leraar.Class.NotFound"]);
                }
                catch
                {
                    return ErrorBody(culture["Leraar.LoadFailed"]);
                }

                return TeacherPortalReadOnlySsrHtml.BuildMaterialsBody(classId, culture);
            },
            ct);
    }

    private static async Task<IResult> RenderMboHboAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        Guid classId,
        CancellationToken ct)
    {
        var path = $"/leraar/klas/{classId:D}/mbo-hbo";
        TeacherMboHboClassProgressDto? model;
        try
        {
            model = await api.GetTeacherMboHboProgressAsync(classId, ct);
        }
        catch
        {
            model = null;
        }

        var body = model is null
            ? ErrorBody(culture["Leraar.LoadFailed"])
            : TeacherPortalReadOnlySsrHtml.BuildMboHboBody(model, culture);

        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            "MBO/HBO voortgang",
            () => Task.FromResult(body),
            ct);
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

    private static string ErrorBody(string message)
        => $"""<p class="state-message error">{System.Net.WebUtility.HtmlEncode(message)}</p>""";
}
