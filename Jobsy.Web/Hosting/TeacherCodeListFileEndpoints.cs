using Jobsy.Core.Authorization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>
/// <c>GET /leraar/klas/{classId}/codelist.pdf|csv</c> on the web host — forwards to the API
/// with the staff session (same authenticated <see cref="JobsyApiClient"/> as Blazor).
/// </summary>
public static class TeacherCodeListFileEndpoints
{
    public static IEndpointRouteBuilder MapTeacherCodeListFileEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" };

        app.MapGet("/leraar/klas/{classId:guid}/codelist.pdf", (Guid classId, HttpContext http, JobsyApiClient api, CancellationToken ct)
            => ProxyAsync(http, api, classId, "pdf", "codelijst.pdf", "application/pdf", ct))
            .RequireAuthorization(auth);

        app.MapGet("/leraar/klas/{classId:guid}/codelist.csv", (Guid classId, HttpContext http, JobsyApiClient api, CancellationToken ct)
            => ProxyAsync(http, api, classId, "csv", "codelijst.csv", "text/csv", ct))
            .RequireAuthorization(auth);

        return app;
    }

    private static async Task<IResult> ProxyAsync(
        HttpContext http,
        JobsyApiClient api,
        Guid classId,
        string extension,
        string fallbackName,
        string fallbackContentType,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        using var response = await api.GetTeacherCodeListFileAsync(classId, extension, ct);
        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? fallbackName;
        var contentType = response.Content.Headers.ContentType?.MediaType ?? fallbackContentType;
        return Results.File(bytes, contentType, fileName);
    }
}
