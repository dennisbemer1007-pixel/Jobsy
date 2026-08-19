using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

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

    public static StaticFileOptions JobsyStaticFiles()
        => new()
        {
            OnPrepareResponse = ctx =>
            {
                var ext = Path.GetExtension(ctx.File.Name);
                if (ext is ".js" or ".css" or ".webp" or ".png" or ".jpg" or ".jpeg" or ".svg"
                    or ".woff2" or ".woff" or ".ico")
                {
                    // Query-string cache busting is already used on CSS/JS/brand marks.
                    ctx.Context.Response.Headers.CacheControl =
                        "public,max-age=31536000,immutable,stale-while-revalidate=86400";
                }
            }
        };
}
