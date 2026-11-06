using System.IO.Compression;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public sealed class PrecompressedStaticAssetTests
{
    /// <summary>Keep in sync with <c>tools/scripts/precompress-wwwroot-assets.py</c> DEFAULTS.</summary>
    public static TheoryData<string> PrecompressAssetPaths { get; } = new()
    {
        "lib/maplibre/maplibre-gl-csp.js",
        "lib/maplibre/maplibre-gl-csp-worker.js",
        "js/jobMap.min.js",
        "css/features/sollicitaties-page.css",
        "css/features/applications.css",
        "css/features/na-de-match.css",
        "js/ndm-confirmed.js",
        "js/ndm-reschedule.js",
        "js/ndm-employer.js",
        "js/ndm-employer-outcome.js",
        "js/ndm-mail-chrome.js",
        "js/ndm-herinneringen.js",
        "js/ndm-employer-reschedule.js",
        "js/ndm-employer-cancel.js",
        "js/ndm-employer-messages.js",
        "js/ndm-pick-slot.js",
        "js/ndm-outcome.js",
        "js/ndm-messages.js",
        "js/passport-availability-matrix.js",
    };

    [Theory]
    [MemberData(nameof(PrecompressAssetPaths))]
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

    [Theory]
    [MemberData(nameof(PrecompressAssetPaths))]
    public void Precompressed_sidecars_match_source_bytes(string relativePath)
    {
        var root = FindRepoRoot();
        var basePath = Path.Combine(root, "Jobsy.Web", "wwwroot", relativePath);
        var raw = File.ReadAllBytes(basePath);
        var gzBytes = DecompressGzip(basePath + ".gz");
        var brBytes = DecompressBrotli(basePath + ".br");
        Assert.Equal(raw, gzBytes);
        Assert.Equal(raw, brBytes);
        if (relativePath == "js/jobMap.min.js")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains(".fb.euro .tip", text, StringComparison.Ordinal);
            Assert.DoesNotContain("tip--sheet-incard", text, StringComparison.Ordinal);
        }

        if (relativePath == "css/features/sollicitaties-page.css")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains(
                ".sollicitaties-page-wrap .sollicitaties-hero-strip",
                text,
                StringComparison.Ordinal);
        }

        if (relativePath == "css/features/na-de-match.css")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains(
                ".ndm-confirmed-page .row.wrapflex.ndm-confirmed-actions{flex-direction:row!important",
                text,
                StringComparison.Ordinal);
            Assert.Contains(".ndm.ndm-reschedule-page", text, StringComparison.Ordinal);
            Assert.Contains(".ndm-reschedule-page button.chip.on{border-color:var(--brand);background:#eef2f7}", text, StringComparison.Ordinal);
            Assert.Contains(".ndm-reschedule-page [hidden]", text, StringComparison.Ordinal);
            Assert.Contains("display:none!important", text, StringComparison.Ordinal);
        }

        if (relativePath == "js/ndm-employer.js")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains("function setPanel", text, StringComparison.Ordinal);
            Assert.Contains("data-ndm-open-panel", text, StringComparison.Ordinal);
        }

        if (relativePath == "js/ndm-reschedule.js")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains("alternativeSlotId", text, StringComparison.Ordinal);
        }

        if (relativePath == "js/ndm-employer-outcome.js")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains("data-ndm-outcome-submitting", text, StringComparison.Ordinal);
        }

        if (relativePath == "js/ndm-employer-messages.js")
        {
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains("data-ndm-sensitive-warning", text, StringComparison.Ordinal);
        }
    }

    private static byte[] DecompressGzip(string path)
    {
        using var input = File.OpenRead(path);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DecompressBrotli(string path)
    {
        using var input = File.OpenRead(path);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
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
