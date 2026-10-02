using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Api.Security;

/// <summary>
/// While maintenance is on (errors 05), non-admin API calls get 503 ProblemDetails plus a
/// <c>Retry-After</c>. Health probes, the public status endpoint and the auth endpoints stay open
/// so Render keeps its green check and admins can still sign in and flip the switch back off.
/// </summary>
public sealed class MaintenanceApiMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceApiMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (IsAlwaysAllowed(path) || IsAdmin(context))
        {
            await _next(context);
            return;
        }

        var state = await ReadStateAsync(context);
        if (state is null || !state.Enabled)
        {
            await _next(context);
            return;
        }

        await WriteProblemAsync(context, state.ExpectedEndUtc);
    }

    /// <summary>
    /// Cached for a few seconds so the common "maintenance is off" path costs no database query.
    /// Writing the switch clears the key, so a flip is visible immediately.
    /// </summary>
    private static async Task<MaintenanceSnapshot?> ReadStateAsync(HttpContext context)
    {
        var cache = context.RequestServices.GetService<IMemoryCache>();
        if (cache is not null
            && cache.TryGetValue(MaintenanceRules.CacheKey, out MaintenanceSnapshot? cached)
            && cached is not null)
        {
            return cached;
        }

        var features = context.RequestServices.GetService<IPlatformFeatureService>();
        if (features is null)
        {
            return null;
        }

        var snap = await features.GetAsync(context.RequestAborted);
        var state = new MaintenanceSnapshot(snap.MaintenanceEnabled, snap.MaintenanceExpectedEndUtc);
        cache?.Set(MaintenanceRules.CacheKey, state, MaintenanceRules.CacheTtl);
        return state;
    }

    private sealed record MaintenanceSnapshot(bool Enabled, DateTime? ExpectedEndUtc);

    /// <summary>Paths that must answer normally even while maintenance is on.</summary>
    public static bool IsAlwaysAllowed(string path)
        => Matches(path, "/health")
           || Matches(path, "/api/site/status")
           || Matches(path, "/api/auth");

    private static bool IsAdmin(HttpContext context)
        => context.User.Identity?.IsAuthenticated == true
           && context.User.IsInRole(JobsyRoles.Admin);

    private static bool Matches(string path, string prefix)
        => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
           && (path.Length == prefix.Length || path[prefix.Length] is '/');

    private static async Task WriteProblemAsync(HttpContext context, DateTime? expectedEndUtc)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var retryAfter = MaintenanceRules.RetryAfterSeconds(expectedEndUtc, DateTime.UtcNow);

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers.CacheControl = "no-store";

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Service unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = "Lobsy is even in onderhoud. Probeer het zo opnieuw."
        };
        problem.Extensions["code"] = MaintenanceRules.Code;
        problem.Extensions["retryAfterSeconds"] = retryAfter;
        problem.Extensions["expectedEndUtc"] = expectedEndUtc;
        problem.Extensions["supportCode"] = SupportCodeGenerator.Create();

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, ProblemJson),
            context.RequestAborted);
    }

    private static readonly JsonSerializerOptions ProblemJson = new(JsonSerializerDefaults.Web);
}
