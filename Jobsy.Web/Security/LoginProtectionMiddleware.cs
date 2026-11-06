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
            && (context.Request.Path.Equals("/account/login", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.Equals("/account/mfa/verify", StringComparison.OrdinalIgnoreCase)))
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var email = form["email"].ToString();
            var ip = TrustedClientIp.Resolve(context) ?? "unknown";
            var operation = context.Request.Path.Equals("/account/mfa/verify", StringComparison.OrdinalIgnoreCase)
                ? "login"
                : "login";
            if (!limiter.TryAcquire(operation, ip, string.IsNullOrWhiteSpace(email) ? null : email))
            {
                var until = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
                var target = context.Request.Path.Equals("/account/mfa/verify", StringComparison.OrdinalIgnoreCase)
                    ? $"/account/mfa?error=too-many&until={until}"
                    : $"/login?error=too-many&until={until}";
                context.Response.StatusCode = StatusCodes.Status303SeeOther;
                context.Response.Headers.Location = target;
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
