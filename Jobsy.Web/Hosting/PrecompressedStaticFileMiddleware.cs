using Microsoft.AspNetCore.StaticFiles;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Serves precompressed <c>.br</c> / <c>.gz</c> sidecars from wwwroot when the client accepts them.
/// Sidecars must be smaller than on-the-fly gzip (quality 11 brotli at build time).
/// </summary>
public sealed class PrecompressedStaticFileMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path)
            || path.Contains("..", StringComparison.Ordinal)
            || !IsPrecompressible(path))
        {
            await next(context);
            return;
        }

        var accept = context.Request.Headers.AcceptEncoding.ToString();
        var encoding = PickEncoding(accept);
        if (encoding is null)
        {
            await next(context);
            return;
        }

        var sidecarPath = path + encoding.Value.Extension;
        var physical = ResolveSidecarPath(env.WebRootPath, sidecarPath);
        if (physical is null || !File.Exists(physical))
        {
            await next(context);
            return;
        }

        if (!ContentTypes.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.Headers.ContentEncoding = encoding.Value.Header;
        context.Response.Headers.Vary = "Accept-Encoding";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        var versioned = context.Request.Query.ContainsKey("v");
        context.Response.Headers.CacheControl =
            WebPerformanceExtensions.StaticAssetCacheControl(versioned);

        var length = new FileInfo(physical).Length;
        if (HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.ContentLength = length;
            return;
        }

        await context.Response.SendFileAsync(physical);
    }

    private static string? ResolveSidecarPath(string webRoot, string requestPath)
    {
        var relative = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(webRoot, relative));
        var rootFull = Path.GetFullPath(webRoot);
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return combined;
    }

    private static (string Extension, string Header)? PickEncoding(string acceptEncoding)
    {
        if (string.IsNullOrEmpty(acceptEncoding))
        {
            return null;
        }

        if (acceptEncoding.Contains("br", StringComparison.OrdinalIgnoreCase))
        {
            return (".br", "br");
        }

        if (acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
        {
            return (".gz", "gzip");
        }

        return null;
    }

    private static bool IsPrecompressible(string path)
    {
        var ext = Path.GetExtension(path);
        return ext is ".js" or ".css" or ".map" or ".json" or ".svg";
    }
}
