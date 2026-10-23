using System.IO.Compression;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public sealed class PrecompressedStaticAssetTests
{
    [Theory]
    [InlineData("lib/maplibre/maplibre-gl-csp.js")]
    [InlineData("lib/maplibre/maplibre-gl-csp-worker.js")]
    [InlineData("js/jobMap.min.js")]
    public void Precompressed_brotli_is_smaller_than_gzip(string relativePath)
    {
        var root = FindRepoRoot();
        var basePath = Path.Combine(root, "Jobsy.Web", "wwwroot", relativePath);
        var raw = new FileInfo(basePath);
        var br = new FileInfo(basePath + ".br");
        var gz = new FileInfo(basePath + ".gz");
        Assert.True(raw.Exists, "Missing source " + relativePath);
        Assert.True(br.Exists, "Missing sidecar " + relativePath + ".br (run tools/scripts/precompress-wwwroot-assets.py)");
        Assert.True(gz.Exists, "Missing sidecar " + relativePath + ".gz");
        Assert.True(br.Length < gz.Length,
            $"{relativePath}: .br ({br.Length}) must be smaller than .gz ({gz.Length})");
        Assert.True(br.Length < raw.Length,
            $"{relativePath}: .br ({br.Length}) must be smaller than raw ({raw.Length})");
    }

    [Fact]
    public void Brotli_response_compression_uses_optimal_level()
    {
        var services = new ServiceCollection();
        services.AddJobsyWebPerformance();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrotliCompressionProviderOptions>>().Value;
        Assert.Equal(CompressionLevel.Optimal, options.Level);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
