namespace Jobsy.Web.Security;

/// <summary>Returns 404 outside Development for dev/test-only routes (<c>/dev/</c>, <c>/__test/</c>).</summary>
public sealed class DevOnlyRouteMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public DevOnlyRouteMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (!_environment.IsDevelopment())
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/dev/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/__test/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }
        }

        return _next(context);
    }
}

public static class DevOnlyRouteMiddlewareExtensions
{
    public static IApplicationBuilder UseDevOnlyRouteGate(this IApplicationBuilder app)
        => app.UseMiddleware<DevOnlyRouteMiddleware>();
}
