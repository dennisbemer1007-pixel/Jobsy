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
    public void Pagehide_shim_is_idempotent_and_keeps_native_add()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("window.__jobsyPagehideShim", app, StringComparison.Ordinal);
        Assert.Contains("if (!window.__jobsyPagehideShim)", app, StringComparison.Ordinal);
        Assert.Contains("var add = EventTarget.prototype.addEventListener;", app, StringComparison.Ordinal);
        Assert.Contains("var remove = EventTarget.prototype.removeEventListener;", app, StringComparison.Ordinal);
        // Patched add/remove call the closed-over natives, not a live re-read of the prototype.
        Assert.Contains("return add.call(this, type, listener, options);", app, StringComparison.Ordinal);
        Assert.Contains("return remove.call(this, type, listener, options);", app, StringComparison.Ordinal);
        Assert.DoesNotContain("EventTarget.prototype.addEventListener.call(this", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_enables_websockets_for_the_blazor_circuit()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("UseWebSockets", program);
        Assert.Contains("KeepAliveInterval", program);
        Assert.Contains("ClientTimeoutInterval", program);
        Assert.Contains("DisconnectedCircuitRetentionPeriod", program);
        Assert.Contains("TimeSpan.FromMinutes(15)", program);
    }

    [Fact]
    public void App_shell_uses_a_subtle_reconnect_toast_instead_of_a_blocking_modal()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("id=\"components-reconnect-modal\"", app);
        Assert.Contains("reconnect-toast", app);
        Assert.Contains("data-reconnect-reload", app);
        // errors 04 §04.3: the toast reads the catalog, so assert the key, not a Dutch literal.
        Assert.Contains("Status.Reconnect.Trying", app, StringComparison.Ordinal);
        Assert.Contains("Status.Reconnect.Failed", app, StringComparison.Ordinal);
        Assert.Contains("Status.Reconnect.Rejected", app, StringComparison.Ordinal);
        Assert.Contains("Status.Reconnect.Reload", app, StringComparison.Ordinal);
        Assert.DoesNotContain(">Verbinding herstellen", app, StringComparison.Ordinal);
        Assert.Equal(
            "Verbinding herstellen…",
            Jobsy.Web.Localization.UiStrings.Get("Status.Reconnect.Trying", "nl"));
        Assert.NotEqual(
            Jobsy.Web.Localization.UiStrings.Get("Status.Reconnect.Trying", "nl"),
            Jobsy.Web.Localization.UiStrings.Get("Status.Reconnect.Trying", "ar"));
        Assert.DoesNotContain("onclick=\"location.reload()\"", app);
        Assert.Contains("closest(\"[data-reconnect-reload]\")", app);

        var appHead = app[..app.IndexOf("@if (LoadBlazorRuntime)", StringComparison.Ordinal)];
        Assert.Contains("css/features/reconnect.css?v=", appHead, StringComparison.Ordinal);
        Assert.Contains("data-reconnect-css", appHead, StringComparison.Ordinal);
        var critical = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "critical.css"));
        Assert.Contains(
            "#components-reconnect-modal:not(.components-reconnect-show)",
            critical,
            StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "reconnect.css"));
        Assert.Contains(".reconnect-toast {\n    display: none;\n    position: fixed;", css);
        Assert.Contains("bottom: calc(5.5rem + env(safe-area-inset-bottom, 0px));", css);
        Assert.Contains(".reconnect-toast.components-reconnect-retrying", css, StringComparison.Ordinal);
        Assert.Contains(".reconnect-toast.components-reconnect-paused", css, StringComparison.Ordinal);
        Assert.Contains(".reconnect-toast__msg {\n    display: none;", css, StringComparison.Ordinal);
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


    [Fact]
    public void Main_layout_wraps_body_in_ErrorBoundary_and_registers_circuit_logger()
    {
        var root = FindRepoRoot();
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("CircuitErrorBoundary", layout);
        var boundary = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "CircuitErrorBoundary.razor"));
        Assert.Contains("ErrorBoundary", boundary);
        Assert.Contains("Circuit.ErrorRetry", boundary);
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Program.cs"));
        Assert.Contains("CircuitExceptionLogger", program);
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web", "Hosting", "CircuitExceptionLogger.cs")));
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
