using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Auth;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Plain POST target of the static <c>/melden</c> form (public-pages 06). It works without JS:
/// antiforgery is checked here, the API applies the <c>report</c> rate limit, and the visitor is
/// redirected back to the form with a result flag.
/// </summary>
public static class ContentReportEndpoints
{
    public const string HttpClientName = "JobsyContentReports";

    public static IEndpointRouteBuilder MapContentReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/melden", async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory) =>
        {
            var form = await http.Request.ReadFormAsync();
            var type = NormalizeType(form["type"].ToString());
            var id = form["id"].ToString().Trim();
            var backUrl = BuildFormUrl(type, id);

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect(backUrl + "&fout=opnieuw");
            }

            if (!Enum.TryParse<ContentReportReason>(form["reason"].ToString(), ignoreCase: true, out var reason)
                || !Enum.IsDefined(reason))
            {
                return Results.Redirect(backUrl + "&fout=reden");
            }

            var email = ContentReportRules.NormalizeEmail(form["email"].ToString());
            var payload = new
            {
                type,
                id,
                reason,
                details = ContentReportRules.NormalizeDetails(form["details"].ToString()),
                email,
                language = http.Request.Cookies.TryGetValue(CultureState.CookieName, out var lang)
                           && !string.IsNullOrWhiteSpace(lang)
                    ? lang
                    : null
            };

            try
            {
                var client = httpClientFactory.CreateClient(HttpClientName);
                var response = await client.PostAsJsonAsync("api/reports", payload, http.RequestAborted);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return Results.Redirect(backUrl + "&fout=teveel");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return Results.Redirect(backUrl + "&fout=opnieuw");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                loggerFactory
                    .CreateLogger("Jobsy.Web.ContentReports")
                    .LogWarning(ex, "Could not forward a content report to the API.");
                return Results.Redirect(backUrl + "&fout=opnieuw");
            }

            return Results.Redirect(
                backUrl + (email is null ? "&verzonden=1" : "&verzonden=mail"));
        })
        .AllowAnonymous()
        .RequireRateLimiting("report-form");

        return app;
    }

    internal static string NormalizeType(string? value)
        => string.Equals(value?.Trim(), "company", StringComparison.OrdinalIgnoreCase)
            ? "company"
            : "vacancy";

    internal static string BuildFormUrl(string type, string? id)
        => $"/melden?type={type}&id={Uri.EscapeDataString(id ?? string.Empty)}";
}
