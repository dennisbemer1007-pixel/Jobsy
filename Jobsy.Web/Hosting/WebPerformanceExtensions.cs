using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;

namespace Jobsy.Web.Hosting;

public static class WebPerformanceExtensions
{
    public static IServiceCollection AddJobsyWebPerformance(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "image/svg+xml",
                "application/javascript",
                "text/javascript",
                "text/css",
                "application/json"
            ]);
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        return services;
    }

    /// <summary>
    /// Lighthouse <c>uses-long-cache-ttl</c> treats &lt; 30 days as inefficient
    /// and 1 year as efficient. Versioned URLs (<c>?v=</c>) are immutable.
    /// </summary>
    public const int VersionedMaxAgeSeconds = 31_536_000; // 365 days
    public const int UnversionedMaxAgeSeconds = 2_592_000; // 30 days

    public static string StaticAssetCacheControl(bool versioned)
        => versioned
            ? $"public,max-age={VersionedMaxAgeSeconds},immutable"
            : $"public,max-age={UnversionedMaxAgeSeconds},stale-while-revalidate=86400";

    public static StaticFileOptions JobsyStaticFiles()
    {
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".map"] = "application/json";
        return new StaticFileOptions
        {
            ContentTypeProvider = contentTypes,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                var ext = Path.GetExtension(ctx.File.Name);
                if (ext is ".js" or ".css" or ".map" or ".webp" or ".png" or ".jpg" or ".jpeg" or ".svg"
                    or ".woff2" or ".woff" or ".ico")
                {
                    var versioned = ctx.Context.Request.Query.ContainsKey("v");
                    ctx.Context.Response.Headers.CacheControl = StaticAssetCacheControl(versioned);
                }
            }
        };
    }
}
