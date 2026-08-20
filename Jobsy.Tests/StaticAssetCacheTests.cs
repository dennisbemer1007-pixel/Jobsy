using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Jobsy.Tests;

/// <summary>
/// PageSpeed/Lighthouse <c>uses-long-cache-ttl</c> ("Efficiënte levensduur voor het cachegeheugen")
/// fails when static assets are cached for less than 30 days. Versioned URLs may use 1 year + immutable.
/// </summary>
public class StaticAssetCacheTests
{
    [Fact]
    public void Versioned_assets_use_one_year_immutable_cache()
    {
        Assert.Equal(31_536_000, WebPerformanceExtensions.VersionedMaxAgeSeconds);
        Assert.Equal(
            "public,max-age=31536000,immutable",
            WebPerformanceExtensions.StaticAssetCacheControl(versioned: true));

        var response = PrepareResponse("app.css", "?v=20260820-r163");
        Assert.Equal("public,max-age=31536000,immutable", response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void Unversioned_assets_use_at_least_thirty_day_cache()
    {
        Assert.Equal(2_592_000, WebPerformanceExtensions.UnversionedMaxAgeSeconds);
        Assert.Equal(
            "public,max-age=2592000,stale-while-revalidate=86400",
            WebPerformanceExtensions.StaticAssetCacheControl(versioned: false));

        var response = PrepareResponse("maplibre-gl.js", query: null);
        var header = response.Headers.CacheControl.ToString();
        Assert.DoesNotContain("604800", header);
        Assert.Contains("max-age=2592000", header);
    }

    [Fact]
    public void Html_does_not_get_long_lived_static_cache()
    {
        var response = PrepareResponse("index.html", "?v=1");
        Assert.True(string.IsNullOrEmpty(response.Headers.CacheControl.ToString()));
    }

    [Fact]
    public void Source_does_not_use_seven_day_static_cache()
    {
        var hosting = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "WebPerformanceExtensions.cs"));
        Assert.DoesNotContain("max-age=604800", hosting);
        Assert.Contains("max-age={VersionedMaxAgeSeconds},immutable", hosting);
        Assert.Contains("max-age={UnversionedMaxAgeSeconds}", hosting);
    }

    [Fact]
    public void Maplibre_and_blazor_urls_are_cache_busted()
    {
        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Home.razor"));
        Assert.Contains("lib/maplibre/maplibre-gl.css?v=", home);
        Assert.Contains("lib/maplibre/maplibre-gl.js?v=", home);

        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("_framework/blazor.web.js?v=", app);
        Assert.Contains("js/app-core.js?v=", app);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("/lib/maplibre/maplibre-gl.css?v=", maps);
        Assert.Contains("/lib/maplibre/maplibre-gl.js?v=", maps);
        Assert.Contains("function pathOnly(url)", maps);

        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("/lib/maplibre/maplibre-gl.css?v=", bundle);
        Assert.Contains("function pathOnly(url)", bundle);
    }

    private static HttpResponse PrepareResponse(string fileName, string? query)
    {
        var http = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(query))
        {
            http.Request.QueryString = new QueryString(query.StartsWith('?') ? query : "?" + query);
        }

        var ctx = new StaticFileResponseContext(http, new NamedFile(fileName));
        WebPerformanceExtensions.JobsyStaticFiles().OnPrepareResponse(ctx);
        return http.Response;
    }

    private sealed class NamedFile : IFileInfo
    {
        public NamedFile(string name) => Name = name;

        public bool Exists => true;
        public long Length => 1;
        public string? PhysicalPath => Name;
        public string Name { get; }
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => Stream.Null;
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
