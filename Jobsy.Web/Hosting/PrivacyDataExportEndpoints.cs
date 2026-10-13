using System.Net.Http.Headers;
using System.Text;
using Jobsy.Core.Time;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Hosting;

/// <summary>
/// <c>GET /privacy/data/export</c> (public-pages 07): the signed-in visitor's AVG export,
/// always as a downloaded file (<c>Content-Disposition: attachment</c>). The JSON body is
/// forwarded from the existing API export and never rendered on a page or kept in a Blazor
/// circuit (D12).
/// </summary>
public static class PrivacyDataExportEndpoints
{
    public const string HttpClientName = "JobsyPrivacyExport";
    private const string ExportFailedRedirect = "/privacy/data?export=failed";

    public static IEndpointRouteBuilder MapPrivacyDataExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/privacy/data/export", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens,
            ILoggerFactory loggerFactory) =>
        {
            var clientIp = TrustedClientIp.Resolve(http);
            var jwt = accessTokens.TryCreate(http.User, clientIp);
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Redirect(ExportFailedRedirect);
            }

            try
            {
                var client = httpClientFactory.CreateClient(HttpClientName);
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/privacy/export");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                using var response = await client.SendAsync(request, http.RequestAborted);
                if (!response.IsSuccessStatusCode)
                {
                    return Results.Redirect(ExportFailedRedirect);
                }

                var json = await response.Content.ReadAsStringAsync(http.RequestAborted);
                var bytes = Encoding.UTF8.GetBytes(json);
                var filename = $"lobsy-mijn-gegevens-{AmsterdamTime.ToLocal(DateTime.UtcNow):yyyy-MM-dd}.json";
                http.Response.Headers.CacheControl = "no-store";
                return Results.File(bytes, "application/json; charset=utf-8", filename);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                loggerFactory
                    .CreateLogger("Jobsy.Web.PrivacyDataExport")
                    .LogWarning(ex, "Could not fetch the privacy export from the API.");
                return Results.Redirect(ExportFailedRedirect);
            }
        })
        .RequireAuthorization()
        .RequireRateLimiting("export");

        return app;
    }
}
