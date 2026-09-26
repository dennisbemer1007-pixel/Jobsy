using Jobsy.Core.Security;

namespace Jobsy.Web.Security;

/// <summary>Account bucket companion to the web login IP rate limit.</summary>
public sealed class LoginProtectionMiddleware
{
    private readonly RequestDelegate _next;

    public LoginProtectionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, LoginProtectionRateLimiter limiter)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/account/login", StringComparison.OrdinalIgnoreCase))
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var email = form["email"].ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!limiter.TryAcquire("login", ip, email))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync(
                    "Te veel pogingen. Wacht even en probeer opnieuw.",
                    context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }
}

public static class LoginProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseLoginProtection(this IApplicationBuilder app)
        => app.UseMiddleware<LoginProtectionMiddleware>();
}
