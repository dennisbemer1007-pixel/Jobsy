using Jobsy.Core.Sales;
using Jobsy.Web.Features;

namespace Jobsy.Web.Hosting;

/// <summary>
/// <c>GET /partner/flyer.pdf</c> (public-pages 09): the partner flyer as a plain download link so
/// the share row on the static <c>/partner</c> page works without JavaScript. The PDF itself is
/// rendered by the API; this endpoint only forwards it, and it follows the same werkgevers-actief
/// gate as the page.
/// </summary>
public static class PartnerFlyerEndpoints
{
    public const string Path = "/partner/flyer.pdf";
    public const string HttpClientName = "JobsyPartnerFlyer";
    private const string FileName = "lobsy-partner-flyer.pdf";

    public static IEndpointRouteBuilder MapPartnerFlyerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            IEmployersSwitch employers,
            ILoggerFactory loggerFactory) =>
        {
            if (!await employers.IsEnabledAsync(http.RequestAborted))
            {
                return Results.Redirect("/");
            }

            var code = SalesTrackingCodes.Normalize(http.Request.Query["code"].ToString());
            var url = code is null || SalesTrackingCodes.IsAmbassadeur(code)
                ? "api/sales-commercial/flyer.pdf"
                : $"api/sales-commercial/flyer.pdf?trackingCode={Uri.EscapeDataString(code)}";

            try
            {
                var client = httpClientFactory.CreateClient(HttpClientName);
                using var response = await client.GetAsync(url, http.RequestAborted);
                if (!response.IsSuccessStatusCode)
                {
                    return Results.Redirect("/partner");
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(http.RequestAborted);
                return Results.File(bytes, "application/pdf", FileName);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                loggerFactory
                    .CreateLogger("Jobsy.Web.PartnerFlyer")
                    .LogWarning(ex, "Could not fetch the partner flyer from the API.");
                return Results.Redirect("/partner");
            }
        })
        .AllowAnonymous()
        .RequireRateLimiting("partner-flyer");

        return app;
    }
}
