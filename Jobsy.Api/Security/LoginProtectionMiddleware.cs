using System.Text.Json;
using Jobsy.Core.Security;

namespace Jobsy.Api.Security;

/// <summary>Applies independent IP and account buckets to anonymous login-code flows.</summary>
public sealed class LoginProtectionMiddleware
{
    private readonly RequestDelegate _next;

    public LoginProtectionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, LoginProtectionRateLimiter limiter)
    {
        var operation = GetOperation(context.Request);
        if (operation is not null)
        {
            var account = await ReadAccountAsync(context);
            var ip = context.User.FindFirst(JobsyAccessToken.ClientIpClaim)?.Value
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
            if (!limiter.TryAcquire(operation, ip, account))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(
                    new { message = "Te veel pogingen. Wacht even en probeer opnieuw." },
                    context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }

    private static string? GetOperation(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return null;
        }

        var path = request.Path.Value ?? string.Empty;
        if (path.Equals("/api/auth/local-login", StringComparison.OrdinalIgnoreCase))
        {
            return "login";
        }

        if (path.Equals("/api/registration", StringComparison.OrdinalIgnoreCase))
        {
            return "register";
        }

        if (path.StartsWith("/api/registration/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/applications", StringComparison.OrdinalIgnoreCase))
        {
            return "verification-code";
        }

        return null;
    }

    private static async Task<string?> ReadAccountAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                   ?? context.User.FindFirst("sub")?.Value;
        }

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            return form["email"].FirstOrDefault() ?? form["contactEmail"].FirstOrDefault();
        }

        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return context.Request.Path.Value;
        }

        context.Request.EnableBuffering();
        try
        {
            using var document = await JsonDocument.ParseAsync(
                context.Request.Body,
                cancellationToken: context.RequestAborted);
            var root = document.RootElement;
            return ReadProperty(root, "email")
                   ?? ReadProperty(root, "contactEmail")
                   ?? context.Request.Path.Value;
        }
        catch (JsonException)
        {
            return context.Request.Path.Value;
        }
        finally
        {
            context.Request.Body.Position = 0;
        }
    }

    private static string? ReadProperty(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public static class LoginProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseLoginProtection(this IApplicationBuilder app)
        => app.UseMiddleware<LoginProtectionMiddleware>();
}
