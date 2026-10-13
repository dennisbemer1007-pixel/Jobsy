using Jobsy.Core.Authorization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Same-origin frames for admin HTML/PDF that must not inherit the page CSP.
/// Email previews carry their own inline styles; partner PDFs are rendered by the API.
/// </summary>
public static class AdminFrameEndpoints
{
    public static IEndpointRouteBuilder MapAdminFrameEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/content/emails/preview-frame", async (
            HttpContext http,
            JobsyApiClient api,
            string key,
            string? lang,
            string? theme) =>
        {
            EmailTemplatePreviewItem? preview;
            try
            {
                preview = await api.GetEmailTemplatePreviewAsync(
                    key,
                    string.IsNullOrWhiteSpace(lang) ? "nl" : lang,
                    string.IsNullOrWhiteSpace(theme) ? "light" : theme,
                    http.RequestAborted);
            }
            catch (Exception)
            {
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            if (preview is null || string.IsNullOrEmpty(preview.Html))
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-store";
            http.Response.Headers.ContentSecurityPolicy =
                "default-src 'none'; style-src 'unsafe-inline'; img-src https: data:; font-src data:; base-uri 'none'; form-action 'none'; frame-ancestors 'self';";
            return Results.Content(preview.Html, "text/html; charset=utf-8");
        }).RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Admin });

        app.MapGet("/admin/paspoortpartners/{id:guid}/codes.pdf", async (
            HttpContext http,
            JobsyApiClient api,
            Guid id) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            var (bytes, status) = await api.GetPassportPartnerCodesPdfAsync(id, http.RequestAborted);
            if (bytes is null || bytes.Length == 0)
            {
                return Results.StatusCode(status is >= 400 and < 600 ? status : StatusCodes.Status502BadGateway);
            }

            return Results.File(bytes, "application/pdf", "partnercodes.pdf");
        }).RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Admin });

        return app;
    }
}
