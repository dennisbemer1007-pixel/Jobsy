using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET routes for teacher class overview and code detail (no Blazor).</summary>
public static class TeacherPortalOverviewEndpoints
{
    private const int PendingPreviewMax = 8;

    public static IEndpointRouteBuilder MapTeacherPortalOverviewEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" };

        app.MapGet("/leraar/klas/{classId:guid}", (Guid classId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, CancellationToken ct)
            => RenderOverviewAsync(http, api, culture, features, classId, ct))
            .RequireAuthorization(auth);

        app.MapGet("/leraar/klas/{classId:guid}/code/{codeId:guid}", (Guid classId, Guid codeId, HttpContext http, JobsyApiClient api, CultureState culture, IFeatureFlags features, CancellationToken ct)
            => RenderCodeDetailAsync(http, api, culture, features, classId, codeId, ct))
            .RequireAuthorization(auth);

        return app;
    }

    private static async Task<IResult> RenderOverviewAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        Guid classId,
        CancellationToken ct)
    {
        var path = $"/leraar/klas/{classId:D}";
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["Leraar.Overview.Title"],
            async () =>
            {
                TeacherClassOverviewDto? overview;
                try
                {
                    overview = await api.GetTeacherOverviewAsync(classId, ct);
                    if (overview is null)
                    {
                        await Task.Delay(1500, ct);
                        overview = await api.GetTeacherOverviewAsync(classId, ct);
                    }
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return (ErrorBody(culture["Leraar.Class.NotFound"]), culture["Leraar.Class.NotFound"]);
                }
                catch
                {
                    return (ErrorBody(culture["Leraar.LoadFailed"]), culture["Leraar.Overview.Title"]);
                }

                if (overview is null)
                {
                    return (ErrorBody(culture["Leraar.Class.NotFound"]), culture["Leraar.Class.NotFound"]);
                }

                var pendingTotal = 0;
                IReadOnlyList<TeacherCodeRowDto> pendingPreview = [];
                TeacherMboHboGroupChartsDto? mboCharts = null;
                try
                {
                    var codes = await api.GetTeacherCodesAsync(classId, ct);
                    var pending = codes
                        .Where(c => c.Status != PupilCodeStatus.Completed)
                        .OrderBy(c => c.Number)
                        .ToList();
                    pendingTotal = pending.Count;
                    pendingPreview = pending.Take(PendingPreviewMax).ToList();
                    if (overview.QuestionSet == PupilQuestionSet.MboHbo)
                    {
                        mboCharts = (await api.GetTeacherMboHboProgressAsync(classId, ct))?.Charts;
                    }
                }
                catch
                {
                    // Overview still useful without pending / MBO chart extras.
                }

                var pageTitle = $"Klas {overview.ClassName}";
                var body = TeacherKlasOverviewSsrHtml.BuildBody(
                    classId,
                    overview,
                    pendingTotal,
                    pendingPreview,
                    mboCharts,
                    culture);
                return (body, pageTitle);
            },
            ct);
    }

    private static async Task<IResult> RenderCodeDetailAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        Guid classId,
        Guid codeId,
        CancellationToken ct)
    {
        var path = $"/leraar/klas/{classId:D}/code/{codeId:D}";
        return await RenderAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["Leraar.Detail.Title"],
            async () =>
            {
                var flags = await features.GetAsync(ct);
                TeacherCodeDetailDto? detail;
                var missingCode = false;
                try
                {
                    detail = await api.GetTeacherCodeDetailAsync(classId, codeId, ct);
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    detail = null;
                    try
                    {
                        var overview = await api.GetTeacherOverviewAsync(classId, ct);
                        missingCode = overview is not null;
                    }
                    catch (Exception overviewError) when (PortalLoad.IsNotFound(overviewError))
                    {
                        missingCode = false;
                    }
                    catch
                    {
                        return (ErrorBody(culture["Leraar.LoadFailed"]), culture["Leraar.Detail.Title"]);
                    }
                }
                catch
                {
                    return (ErrorBody(culture["Leraar.LoadFailed"]), culture["Leraar.Detail.Title"]);
                }

                if (detail is null)
                {
                    var msg = missingCode ? culture["Leraar.Code.NotFound"] : culture["Leraar.Class.NotFound"];
                    return (ErrorBody(msg), culture["Leraar.Detail.Title"]);
                }

                PupilG78PassportWebDto? g78 = null;
                if (flags.PupilPassportV2Enabled
                    && detail.Status == PupilCodeStatus.Completed
                    && detail.QuestionSet == PupilQuestionSet.Groep78)
                {
                    try
                    {
                        g78 = await api.GetTeacherG78PassportAsync(classId, codeId, ct);
                    }
                    catch
                    {
                        g78 = null;
                    }
                }

                var passportV2 = flags.PupilPassportV2Enabled;
                var pageTitle = detail.DisplayCode;
                var body = TeacherCodeDetailSsrHtml.BuildBody(classId, codeId, detail, g78, passportV2, culture);
                return (body, pageTitle);
            },
            ct);
    }

    private static async Task<IResult> RenderAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        string path,
        string defaultTitle,
        Func<Task<(string Body, string PageTitle)>> buildAsync,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);

        if (!await SchoolsFeatureGate.IsEnabledAsync(features, ct))
        {
            var shellOff = await ScholenPortalShell.BuildAsync(http, culture, api, features, path);
            return Results.Content(
                ScholenPortalSsrFrame.RenderDocument(defaultTitle, "", shellOff, culture, path),
                "text/html; charset=utf-8");
        }

        var (body, pageTitle) = await buildAsync();
        var shell = await ScholenPortalShell.BuildAsync(http, culture, api, features, path);
        return Results.Content(
            ScholenPortalSsrFrame.RenderDocument(pageTitle, body, shell, culture, path),
            "text/html; charset=utf-8");
    }

    private static string ErrorBody(string message)
        => $"""<p class="state-message error">{System.Net.WebUtility.HtmlEncode(message)}</p>""";
}
