using Jobsy.Core.Authorization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>
/// <c>GET /leraar/klas/{classId}/code/{codeId}/report.pdf</c> on the web host — forwards to the API
/// with the staff session (same authenticated <see cref="JobsyApiClient"/> as Blazor).
/// </summary>
public static class TeacherPupilReportPdfEndpoints
{
    public static IEndpointRouteBuilder MapTeacherPupilReportPdfEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" };

        app.MapGet("/leraar/klas/{classId:guid}/code/{codeId:guid}/report.pdf", (Guid classId, Guid codeId, HttpContext http, JobsyApiClient api, CancellationToken ct)
            => ProxyAsync(http, api, classId, codeId, ct))
            .RequireAuthorization(auth);

        return app;
    }

    private static async Task<IResult> ProxyAsync(
        HttpContext http,
        JobsyApiClient api,
        Guid classId,
        Guid codeId,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        using var response = await api.GetTeacherPupilReportFileAsync(classId, codeId, ct);
        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "lobsy-ontdekkingsreis.pdf";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        return Results.File(bytes, contentType, fileName);
    }
}
