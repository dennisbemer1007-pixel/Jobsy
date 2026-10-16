namespace Jobsy.Web.Auth;

/// <summary>
/// Logged-out GET/HEAD to candidate-only entry URLs go straight to login (one hop, no Blazor round trip).
/// </summary>
public sealed class CandidateGuestRedirectMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> GuestOnlyPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/candidate/start",
        "/candidate/paspoort",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsRedirectCandidate(context)
            && context.User?.Identity?.IsAuthenticated != true)
        {
            var path = context.Request.Path.Value ?? "/";
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
            var returnUrl = Uri.EscapeDataString(path + query);
            context.Response.Redirect($"/login?returnUrl={returnUrl}");
            return;
        }

        await next(context);
    }

    private static bool IsRedirectCandidate(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;
        return path is not null && GuestOnlyPaths.Contains(path);
    }
}

public static class CandidateGuestRedirectExtensions
{
    public static IApplicationBuilder UseCandidateGuestRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<CandidateGuestRedirectMiddleware>();
}
