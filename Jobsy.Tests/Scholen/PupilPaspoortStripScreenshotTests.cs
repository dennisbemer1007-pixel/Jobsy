using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>Generates before/after paspoort header evidence when JOBSY_PASPOORT_STRIP_SHOT=1.</summary>
public sealed class PupilPaspoortStripScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/paspoort-strip-remove";
    private static readonly string Wwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web", "wwwroot"));
    private StaticFileServer? _server;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_PASPOORT_STRIP_SHOT"), "1", StringComparison.Ordinal))
        {
            return;
        }

        Directory.CreateDirectory(ArtifactsDir);
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _server = new StaticFileServer(Wwwroot);
    }

    public async ValueTask DisposeAsync()
    {
        _server?.Dispose();
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Theory]
    [InlineData(1440, 900, "after-1440.png")]
    [InlineData(390, 844, "after-390.png")]
    public async Task Capture_paspoort_header_without_strip(int width, int height, string fileName)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_PASPOORT_STRIP_SHOT"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "paspoort-g78-after-fixture.html"));
        Assert.True(File.Exists(fixture), $"Missing fixture {fixture}");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 600,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_server!.BaseUrl}/fixture/paspoort-g78-after-fixture.html", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("[data-testid=leerling-paspoort-g78]").ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, fileName),
        });
    }

    private sealed class StaticFileServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly string _root;
        private readonly Task _loop;

        public StaticFileServer(string wwwrootRoot)
        {
            _root = wwwrootRoot;
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            BaseUrl = $"http://127.0.0.1:{port}";
            _loop = Task.Run(ServeLoop);
        }

        public string BaseUrl { get; }

        private async Task ServeLoop()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    var rel = ctx.Request.Url?.AbsolutePath.TrimStart('/') ?? "";
                    if (rel.StartsWith("fixture/", StringComparison.OrdinalIgnoreCase))
                    {
                        var fixtureRoot = Path.GetFullPath(Path.Combine(_root, "..", "..", "Jobsy.Tests", "Assets"));
                        var fixturePath = Path.Combine(fixtureRoot, rel["fixture/".Length..].Replace('/', Path.DirectorySeparatorChar));
                        if (fixturePath.StartsWith(fixtureRoot, StringComparison.Ordinal) && File.Exists(fixturePath))
                        {
                            await WriteFileResponse(ctx, fixturePath);
                            continue;
                        }

                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        continue;
                    }

                    var path = Path.Combine(_root, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!path.StartsWith(_root, StringComparison.Ordinal) || !File.Exists(path))
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        continue;
                    }

                    await WriteFileResponse(ctx, path);
                }
                catch
                {
                    // Test server shuts down.
                }
            }
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            try
            {
                _loop.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignore
            }
        }

        private static async Task WriteFileResponse(HttpListenerContext ctx, string path)
        {
            var bytes = await File.ReadAllBytesAsync(path);
            ctx.Response.ContentType = path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                ? "text/css"
                : path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    ? "image/png"
                    : "text/html";
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();
        }
    }
}
