using System.Threading.RateLimiting;
using Jobsy.Core.Sales;
using Jobsy.Web.Sales;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Hosting;

public static class SalesReferralEndpoints
{
    public static IServiceCollection AddSalesReferralRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Merged with existing policies via Configure; caller also registers in Program.
        });
        return services;
    }

    public static void MapSalesReferralEndpoints(this WebApplication app)
    {
        app.MapGet("/p/{code}", HandleShortLinkAsync)
            .AllowAnonymous()
            .RequireRateLimiting("public-redirect");
    }

    private static async Task<IResult> HandleShortLinkAsync(
        string code,
        HttpContext http,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var normalized = SalesTrackingCodes.Normalize(code);
        var channelQuery = http.Request.Query["b"].ToString();
        var channel = SalesTrackingCodes.ChannelFromQuery(channelQuery);

        if (normalized is null || SalesTrackingCodes.IsAmbassadeur(normalized))
        {
            // Unknown or parked AM- → 404, no cookie, no click.
            return Results.NotFound();
        }

        var isBot = BotUserAgents.IsBot(http.Request.Headers.UserAgent.ToString());
        var isHead = HttpMethods.IsHead(http.Request.Method);
        var sameDayRepeat = SalesReferralCookie.AlreadyHoldsCodeToday(http, normalized);
        var isPreview = http.Request.Query.TryGetValue("preview", out var previewVal)
                        && (previewVal == "1"
                            || string.Equals(previewVal.ToString(), "true", StringComparison.OrdinalIgnoreCase));

        SalesReferralVisitResult? visit = null;
        try
        {
            visit = await api.RecordSalesReferralVisitAsync(
                normalized,
                channel.ToString(),
                countClick: !isPreview && !isBot && !isHead && !sameDayRepeat,
                cancellationToken);
        }
        catch
        {
            // Soft-fail visit recording — still redirect when the code looks well-formed.
        }

        if (visit is { Active: true })
        {
            if (!isPreview)
            {
                SalesReferralCookie.TrySetFirstClick(http, normalized, visit.CookieDays);
            }

            var target = $"/partner/{Uri.EscapeDataString(normalized)}";
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(channelQuery))
            {
                qs.Add($"b={Uri.EscapeDataString(channelQuery)}");
            }

            if (isPreview)
            {
                qs.Add("preview=1");
            }

            if (qs.Count > 0)
            {
                target += "?" + string.Join('&', qs);
            }

            return Results.Redirect(target, permanent: false);
        }

        // Inactive / unknown after API check → 404
        if (visit is { Active: false })
        {
            return Results.NotFound();
        }

        // API unreachable: still redirect well-formed SM/BM/IM so printed links work.
        var fallback = $"/partner/{Uri.EscapeDataString(normalized)}";
        if (!string.IsNullOrWhiteSpace(channelQuery))
        {
            fallback += $"?b={Uri.EscapeDataString(channelQuery)}";
        }

        return Results.Redirect(fallback, permanent: false);
    }
}

public sealed class SalesReferralVisitResult
{
    public bool Active { get; set; }
    public int CookieDays { get; set; } = 30;
    public string? Code { get; set; }
    public string? Kind { get; set; }
}
