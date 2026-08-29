using System.Text.Json;
using Jobsy.Web.Hosting;
using Jobsy.Web.Seo;

namespace Jobsy.Tests;

public class BlazorCircuitGuardTests
{
    public static TheoryData<string?, bool> UserAgents() => new()
    {
        { null, false },
        { "", false },
        { "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36", false },
        { "Mozilla/5.0 (Linux; Android 10; CUBOT X20) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.210 Mobile Safari/537.36", false },
        { "Mozilla/5.0 (Linux; Android 11; moto g power (2022)) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36 Chrome-Lighthouse", true },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 PTST/2312", true },
        { "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", true },
        { "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm) Chrome/116.0.1938.76 Safari/537.36", true },
        { "Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 WhatsApp/2.24.20.76", false }
    };

    [Theory]
    [MemberData(nameof(UserAgents))]
    public void Interactive_runtime_is_skipped_only_for_auditors_and_crawlers(string? userAgent, bool skip)
        => Assert.Equal(skip, CrawlerUserAgent.ShouldSkipInteractiveRuntime(userAgent));

    [Fact]
    public void App_shell_skips_blazor_for_auditors_and_remaps_unload()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("CrawlerUserAgent.ShouldSkipInteractiveRuntime", app);
        Assert.Contains("_framework/blazor.web.js?v=", app);
        Assert.Contains("LoadBlazorRuntime", app);
        Assert.Contains("type === \"unload\"", app);
        Assert.Contains("pagehide", app);
        Assert.Contains("js/app-core.js?v=", app);
        Assert.Contains("nonce=\"@Nonce\"", app);
        Assert.Contains("defer", app);
    }

    [Fact]
    public void Host_enables_websockets_for_the_blazor_circuit()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("UseWebSockets", program);
        Assert.Contains("KeepAliveInterval", program);
        Assert.Contains("ClientTimeoutInterval", program);
        Assert.Contains("DisconnectedCircuitRetentionPeriod", program);
        Assert.Contains("TimeSpan.FromMinutes(5)", program);
    }

    [Fact]
    public void App_shell_uses_a_subtle_reconnect_toast_instead_of_a_blocking_modal()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("id=\"components-reconnect-modal\"", app);
        Assert.Contains("reconnect-toast", app);
        Assert.Contains("Verbinding herstellen", app);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(".reconnect-toast {\n    display: none;\n    position: fixed;", css);
        Assert.Contains("bottom: calc(5.5rem + env(safe-area-inset-bottom, 0px));", css);
    }

    [Fact]
    public void Maplibre_ships_a_source_map_for_the_large_first_party_bundle()
    {
        var root = FindRepoRoot();
        var js = Path.Combine(root, "Jobsy.Web", "wwwroot", "lib", "maplibre", "maplibre-gl-csp.js");
        var map = Path.Combine(root, "Jobsy.Web", "wwwroot", "lib", "maplibre", "maplibre-gl-csp.js.map");
        Assert.True(File.Exists(map));
        Assert.Contains("//# sourceMappingURL=maplibre-gl-csp.js.map", File.ReadAllText(js));
        Assert.True(new FileInfo(js).Length < 1_000_000);

        using var doc = JsonDocument.Parse(File.ReadAllText(map));
        Assert.Equal(3, doc.RootElement.GetProperty("version").GetInt32());
        Assert.Equal("maplibre-gl-csp.js", doc.RootElement.GetProperty("file").GetString());
        Assert.False(doc.RootElement.TryGetProperty("sourcesContent", out _));
        Assert.True(doc.RootElement.GetProperty("sources").GetArrayLength() > 0);
        Assert.True(new FileInfo(map).Length < 2_000_000);
    }

    [Fact]
    public void Source_map_files_use_the_same_cache_policy_as_javascript()
    {
        var hosting = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "WebPerformanceExtensions.cs"));
        Assert.Contains("Mappings[\".map\"]", hosting);
        Assert.Contains("or \".map\"", hosting);

        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?v=20260820-r180");
        var ctx = new Microsoft.AspNetCore.StaticFiles.StaticFileResponseContext(
            http,
            new NamedFile("maplibre-gl-csp.js.map"));
        WebPerformanceExtensions.JobsyStaticFiles().OnPrepareResponse(ctx);
        Assert.Equal("public,max-age=31536000,immutable", http.Response.Headers.CacheControl.ToString());
    }

    private sealed class NamedFile : Microsoft.Extensions.FileProviders.IFileInfo
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
