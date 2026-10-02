using Microsoft.AspNetCore.Diagnostics;

namespace Jobsy.Web.Hosting;

/// <summary>Wiring for the status / error pages (errors stack 01).</summary>
public static class ErrorPagesExtensions
{
    public const string ErrorPath = "/Error";
    public const string StatusPathTemplate = "/status/{0}";

    /// <summary>Set to true in tests to exercise the production handlers inside Development.</summary>
    public const string ForceHandlerConfigKey = "Errors:ForceHandler";

    /// <summary>
    /// Opt-in switch for <see cref="TestThrowPath"/>. Off by default, only honoured in
    /// Development, and never set on Render — it exists so the CI browser suite can look at the
    /// real 500 page instead of a stubbed one.
    /// </summary>
    public const string EnableTestThrowConfigKey = "Errors:EnableTestThrow";

    public const string TestThrowPath = "/__test/throw";

    /// <summary>Request method before the re-execute was forced to GET.</summary>
    public const string OriginalMethodItemsKey = "Jobsy.Error.OriginalMethod";

    /// <summary>Status the visitor should see, remembered before any re-execute can fail.</summary>
    public const string OriginalStatusItemsKey = "Jobsy.Error.OriginalStatus";

    private static readonly string[] NonHtmlPrefixes =
    [
        "/api",
        "/_blazor",
        "/_framework",
        "/_content",
        "/healthz"
    ];

    /// <summary>
    /// Outermost safety net: when even <c>ErrorLayout</c> cannot render, write minimal
    /// hard-coded HTML instead of an empty body, keeping the status the visitor should see.
    /// </summary>
    public static IApplicationBuilder UseErrorPageFallback(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception) when (!context.Response.HasStarted)
            {
                var status = context.Items.TryGetValue(OriginalStatusItemsKey, out var remembered)
                             && remembered is int code
                    ? code
                    : StatusCodes.Status500InternalServerError;
                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "text/html; charset=utf-8";
                ErrorResponse.ApplyHeaders(context);
                await context.Response.WriteAsync(ErrorResponse.MinimalHtml(status));
            }
        });

    /// <summary>
    /// Adds <see cref="TestThrowPath"/> when <see cref="EnableTestThrowConfigKey"/> is on in a
    /// Development host. Anywhere else this is a no-op, so the path 404s like any unknown page.
    /// </summary>
    public static IApplicationBuilder UseTestThrowPath(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()
            || !app.Configuration.GetValue<bool>(EnableTestThrowConfigKey))
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals(TestThrowPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Deliberate error from " + TestThrowPath + ".");
            }

            await next(context);
        });
    }

    /// <summary>
    /// The exception handler re-executes with the original method; <c>/Error</c> is a GET-only
    /// Razor endpoint, so a POST that throws would 405 and never reach the page.
    /// </summary>
    public static IApplicationBuilder UseErrorPageMethodReset(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (!HttpMethods.IsGet(context.Request.Method)
                && context.Features.Get<IExceptionHandlerPathFeature>() is not null
                && context.Request.Path.StartsWithSegments(ErrorPath, StringComparison.OrdinalIgnoreCase))
            {
                context.Items[OriginalMethodItemsKey] = context.Request.Method;
                context.Request.Method = HttpMethods.Get;
                context.Request.ContentType = null;
                context.Request.ContentLength = null;
                context.Request.Body = Stream.Null;
            }

            await next(context);
        });

    /// <summary>
    /// Re-executes HTML page requests that ended in a bare status code through
    /// <c>/status/{code}</c>. API, Blazor, framework and static-file responses are untouched.
    /// </summary>
    public static IApplicationBuilder UseHtmlStatusCodePages(this IApplicationBuilder app)
        => app.UseWhen(
            WantsHtmlStatusPage,
            branch => branch
                .UseStatusCodePagesWithReExecute(StatusPathTemplate)
                .Use(async (context, next) =>
                {
                    await next(context);
                    // Runs before the re-execute above, so the fallback knows which code to keep
                    // even when rendering /status/{code} fails.
                    if (!context.Items.ContainsKey(OriginalStatusItemsKey))
                    {
                        context.Items[OriginalStatusItemsKey] = context.Response.StatusCode;
                    }
                }));

    public static bool WantsHtmlStatusPage(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? "/";
        foreach (var prefix in NonHtmlPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (path.Length == prefix.Length || path[prefix.Length] == '/'))
            {
                return false;
            }
        }

        if (HasFileExtension(path))
        {
            return false;
        }

        return AcceptsHtml(request.Headers.Accept.ToString());
    }

    private static bool HasFileExtension(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        var segment = lastSlash < 0 ? path : path[(lastSlash + 1)..];
        var dot = segment.LastIndexOf('.');
        return dot > 0 && dot < segment.Length - 1;
    }

    // No Accept header or */* (curl, simple crawlers) also gets the HTML page, as the public-pages
    // hotfix already did on acceptatie; API/static/Blazor paths are excluded above.
    private static bool AcceptsHtml(string? accept)
        => string.IsNullOrEmpty(accept)
           || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
           || accept.Contains("*/*", StringComparison.Ordinal);
}
