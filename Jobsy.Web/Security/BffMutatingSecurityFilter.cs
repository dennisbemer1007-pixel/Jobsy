using Microsoft.AspNetCore.Antiforgery;
namespace Jobsy.Web.Security;

/// <summary>
/// Same-origin and antiforgery gate for cookie-authenticated BFF POST/PUT/PATCH/DELETE endpoints.
/// </summary>
public sealed class BffMutatingSecurityFilter : IEndpointFilter
{
    public static readonly BffMutatingSecurityFilter Instance = new();

    private BffMutatingSecurityFilter()
    {
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var method = http.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return await next(context);
        }

        if (!IsSameOrigin(http, http.RequestServices.GetRequiredService<IConfiguration>()))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Results.StatusCode(StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }

    internal static bool IsSameOrigin(HttpContext http, IConfiguration configuration)
    {
        var fetchSite = http.Request.Headers["Sec-Fetch-Site"].ToString();
        if (!string.IsNullOrEmpty(fetchSite)
            && !string.Equals(fetchSite, "same-origin", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fetchSite, "none", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var origin = http.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            return true;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        var expected = ResolveExpectedOrigin(http, configuration);
        if (expected is null)
        {
            return string.Equals(originUri.Host, http.Request.Host.Host, StringComparison.OrdinalIgnoreCase)
                && originUri.Scheme.Equals(http.Request.Scheme, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            Jobsy.Core.JobsyPublicUrl.NormalizeOrigin(origin),
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string? ResolveExpectedOrigin(HttpContext http, IConfiguration configuration)
    {
        var configured = configuration["PublicWebBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Jobsy.Core.JobsyPublicUrl.NormalizeOrigin(configured).TrimEnd('/');
        }

        return $"{http.Request.Scheme}://{http.Request.Host.Value}".TrimEnd('/');
    }
}

public static class BffMutatingEndpointExtensions
{
    public static RouteHandlerBuilder RequireBffMutatingSecurity(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter(BffMutatingSecurityFilter.Instance);
}
