using System.Net;
using System.Text.RegularExpressions;
using Jobsy.Tests.Errors;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// The reconnect markup lives in the static shell. On <c>[NoBlazorRuntime]</c> pages
/// <c>app.min.css</c> is not linked, so the toast CSS must load on its own and hide
/// every state until Blazor sets exactly one. Also asserts the document CSP does not
/// report <c>style-src-elem</c> (or any other) violations while that CSS applies.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ReconnectOverlayPlaywrightTests
{
    private const string Origin = "https://lobsy.reconnect.test";

    [Fact]
    public async Task Public_shell_hides_reconnect_ui_until_one_state_and_reports_no_csp_violations()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();
        using var response = await client.GetAsync("/status/404");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.Headers.TryGetValues("Content-Security-Policy", out var cspValues),
            "Expected a Content-Security-Policy header.");
        var csp = string.Join("; ", cspValues!);
        // Playwright reads computed style via eval. Production CSP omits 'unsafe-eval';
        // style-src-elem stays exactly as the server sent it.
        csp = Regex.Replace(
            csp,
            @"script-src\s+'self'\s+'nonce-[^']+'",
            m => m.Value + " 'unsafe-eval'");

        var root = FindRepoRoot();
        var wwwroot = Path.Combine(root, "Jobsy.Web", "wwwroot");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        var violations = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error"
                && msg.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(msg.Text);
            }
        };

        await page.RouteAsync("**/*", async route =>
        {
            var request = route.Request;
            var path = new Uri(request.Url).AbsolutePath;
            if (path is "/" or "/status/404")
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    ContentType = "text/html; charset=utf-8",
                    Body = html,
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Security-Policy"] = csp
                    }
                });
                return;
            }

            var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var file = Path.GetFullPath(Path.Combine(wwwroot, relative));
            if (!file.StartsWith(wwwroot, StringComparison.Ordinal) || !File.Exists(file))
            {
                await route.FulfillAsync(new() { Status = 404, Body = "" });
                return;
            }

            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = ContentTypeFor(file),
                BodyBytes = await File.ReadAllBytesAsync(file)
            });
        });

        await page.GotoAsync(Origin + "/status/404", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        var modal = page.Locator("#components-reconnect-modal");
        await Assertions.Expect(modal).ToBeHiddenAsync();

        var hidden = await SnapshotAsync(page);
        Assert.Equal("none", hidden.Display);
        Assert.Equal("none", hidden.Show);
        Assert.Equal("none", hidden.Failed);
        Assert.Equal("none", hidden.Rejected);

        await page.EvaluateAsync("""
            () => {
              const el = document.getElementById('components-reconnect-modal');
              el.className = 'reconnect-toast components-reconnect-show';
            }
            """);
        var showing = await SnapshotAsync(page);
        Assert.Equal("block", showing.Display);
        Assert.Equal("fixed", showing.Position);
        Assert.Equal("block", showing.Show);
        Assert.Equal("none", showing.Failed);
        Assert.Equal("none", showing.Rejected);
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--failed")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--rejected")).ToBeHiddenAsync();

        await page.EvaluateAsync("""
            () => {
              const el = document.getElementById('components-reconnect-modal');
              el.className = 'reconnect-toast components-reconnect-failed';
            }
            """);
        var failed = await SnapshotAsync(page);
        Assert.Equal("block", failed.Display);
        Assert.Equal("fixed", failed.Position);
        Assert.Equal("none", failed.Show);
        Assert.Equal("block", failed.Failed);
        Assert.Equal("none", failed.Rejected);
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--failed")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--show")).ToBeHiddenAsync();

        await page.EvaluateAsync("""
            () => {
              const el = document.getElementById('components-reconnect-modal');
              el.className = 'reconnect-toast components-reconnect-rejected';
            }
            """);
        var rejected = await SnapshotAsync(page);
        Assert.Equal("block", rejected.Display);
        Assert.Equal("fixed", rejected.Position);
        Assert.Equal("none", rejected.Show);
        Assert.Equal("none", rejected.Failed);
        Assert.Equal("block", rejected.Rejected);
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--rejected")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--failed")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".reconnect-toast__msg--rejected .reconnect-toast__action")).ToBeVisibleAsync();

        Assert.True(
            violations.Count == 0,
            "CSP violations: " + string.Join(" | ", violations.Take(8)));
    }

    private static async Task<ToastSnapshot> SnapshotAsync(IPage page)
    {
        return await page.EvaluateAsync<ToastSnapshot>(
            """
            () => {
              const el = document.getElementById('components-reconnect-modal');
              const cs = getComputedStyle(el);
              const msg = (name) => getComputedStyle(el.querySelector(name)).display;
              return {
                display: cs.display,
                position: cs.position,
                show: msg('.reconnect-toast__msg--show'),
                failed: msg('.reconnect-toast__msg--failed'),
                rejected: msg('.reconnect-toast__msg--rejected')
              };
            }
            """);
    }

    private static string ContentTypeFor(string file)
    {
        var ext = Path.GetExtension(file);
        return ext switch
        {
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".json" => "application/json",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream"
        };
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

    private sealed class ToastSnapshot
    {
        public string Display { get; set; } = "";
        public string Position { get; set; } = "";
        public string Show { get; set; } = "";
        public string Failed { get; set; } = "";
        public string Rejected { get; set; } = "";
    }
}
