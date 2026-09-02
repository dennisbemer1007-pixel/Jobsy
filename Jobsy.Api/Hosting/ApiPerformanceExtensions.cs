using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Jobsy.Api.Hosting;

/// <summary>
/// Brotli/Gzip for JSON (and other text) so Render egress stays far under the 5 GB cap.
/// Raster images are already compressed and are not re-wrapped here.
/// </summary>
public static class ApiPerformanceExtensions
{
    public static IServiceCollection AddJobsyApiPerformance(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "application/json",
                "application/problem+json",
                "text/plain"
            ]);
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        return services;
    }
}
