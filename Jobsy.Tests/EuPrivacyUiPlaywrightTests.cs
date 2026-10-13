using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Browser checks for local read-aloud voices, the insights map style, and the video placeholder.
/// These pages are built in Chromium and do not need a running site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EuPrivacyUiPlaywrightTests
{
    [Fact]
    public async Task Read_aloud_picks_only_a_local_voice_for_the_language()
    {
        await using var session = await BrowserAsync();
        var page = session.Page;
        await page.SetContentAsync("<!DOCTYPE html><html lang=\"nl\"><body></body></html>");
        await page.AddScriptTagAsync(new()
        {
            Path = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "js", "read-aloud.js")
        });

        var picked = await page.EvaluateAsync<string?>("""
            () => {
              const voices = [
                { lang: "nl-NL", localService: false, name: "Online NL" },
                { lang: "en-US", localService: true, name: "Local EN" },
                { lang: "nl-NL", localService: true, name: "Local NL" }
              ];
              const voice = window.lobsyReadAloud.selectLocalVoice(voices, "nl");
              const onlineOnly = window.lobsyReadAloud.selectLocalVoice(
                [{ lang: "nl-NL", localService: false, name: "Online NL" }],
                "nl");
              window.__lobsyReadAloudSynth = {
                getVoices: () => [{ lang: "nl-NL", localService: false, name: "Online NL" }],
                cancel() {},
                speak() {}
              };
              const probe = window.lobsyReadAloud.probe("nl");
              return JSON.stringify({
                name: voice && voice.name,
                online: onlineOnly,
                supported: probe.supported
              });
            }
            """);

        Assert.Contains("\"name\":\"Local NL\"", picked, StringComparison.Ordinal);
        Assert.Contains("\"online\":null", picked, StringComparison.Ordinal);
        Assert.Contains("\"supported\":false", picked, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Insights_map_uses_openfreemap_and_paints_a_canvas()
    {
        await using var session = await BrowserAsync();
        var page = session.Page;
        var blocked = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("demotiles", StringComparison.OrdinalIgnoreCase)
                || request.Url.Contains("i.ytimg.com", StringComparison.OrdinalIgnoreCase))
            {
                blocked.Add(request.Url);
            }
        };
        page.Console += (_, message) =>
        {
            if (message.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase)
                || message.Text.Contains("demotiles", StringComparison.OrdinalIgnoreCase))
            {
                blocked.Add(message.Text);
            }
        };

        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta http-equiv="Content-Security-Policy"
                    content="img-src 'self' data: https://tiles.openfreemap.org; connect-src 'self' https://tiles.openfreemap.org; default-src 'self' 'unsafe-inline'">
            </head>
            <body>
              <div id="insights-map" style="width:320px;height:240px"></div>
            </body>
            </html>
            """);
        await page.AddScriptTagAsync(new()
        {
            Content = """
                window.__insightsStyle = null;
                window.__csp = [];
                document.addEventListener("securitypolicyviolation", (event) => {
                  window.__csp.push(event.blockedURI || event.violatedDirective || "csp");
                });
                function noop() {}
                function InsightsMap(options) {
                  window.__insightsStyle = options.style;
                  const canvas = document.createElement("canvas");
                  canvas.className = "maplibregl-canvas";
                  canvas.width = 320;
                  canvas.height = 240;
                  options.container.appendChild(canvas);
                  this.scrollZoom = { disable: noop };
                  this.dragPan = { disable: noop };
                  this.boxZoom = { disable: noop };
                  this.dragRotate = { disable: noop };
                  this.touchZoomRotate = { disableRotation: noop };
                }
                InsightsMap.prototype.on = function (name, fn) { if (name === "load") fn(); return this; };
                InsightsMap.prototype.addControl = function () { return this; };
                InsightsMap.prototype.addSource = function () { return this; };
                InsightsMap.prototype.addLayer = function () { return this; };
                window.maplibregl = { Map: InsightsMap, NavigationControl: function () {} };
                """
        });
        await page.AddScriptTagAsync(new()
        {
            Path = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "js", "features", "kandidaatinzichten-map.js")
        });

        var result = await page.EvaluateAsync<string>("""
            async () => {
              await window.JobsyCandidateInsightsMap.mount("insights-map", {
                center: [4.3, 52.07],
                density: [],
                branches: []
              });
              const canvas = document.querySelector("#insights-map canvas");
              return JSON.stringify({
                style: window.__insightsStyle,
                width: canvas ? canvas.width : 0,
                csp: window.__csp
              });
            }
            """);

        Assert.Contains("https://tiles.openfreemap.org/styles/liberty", result, StringComparison.Ordinal);
        Assert.DoesNotContain("demotiles", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"width\":320", result, StringComparison.Ordinal);
        Assert.Contains("\"csp\":[]", result, StringComparison.Ordinal);
        Assert.Empty(blocked);
    }

    [Fact]
    public async Task Video_placeholder_does_not_load_a_thumbnail_before_play()
    {
        await using var session = await BrowserAsync();
        var page = session.Page;
        var thumbnails = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("i.ytimg.com", StringComparison.OrdinalIgnoreCase)
                || request.Url.Contains("vimeocdn.com", StringComparison.OrdinalIgnoreCase))
            {
                thumbnails.Add(request.Url);
            }
        };

        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta http-equiv="Content-Security-Policy" content="img-src 'self' data:; frame-src https://www.youtube-nocookie.com https://player.vimeo.com;">
            </head>
            <body>
              <div class="detail-card__video">
                <button type="button" class="detail-card__video-poster" id="play">Video</button>
              </div>
              <p class="detail-card__video-notice">De video laadt van YouTube of Vimeo nadat je op play klikt.</p>
              <script>
                document.getElementById("play").addEventListener("click", function () {
                  var frame = document.createElement("iframe");
                  frame.src = "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ";
                  document.querySelector(".detail-card__video").appendChild(frame);
                });
              </script>
            </body>
            </html>
            """);

        var before = await page.EvaluateAsync<string>("""
            () => JSON.stringify({
              images: document.images.length,
              frames: document.querySelectorAll("iframe").length,
              notice: document.querySelector(".detail-card__video-notice").textContent
            })
            """);
        Assert.Contains("\"images\":0", before, StringComparison.Ordinal);
        Assert.Contains("\"frames\":0", before, StringComparison.Ordinal);
        Assert.Contains("De video laadt van YouTube of Vimeo nadat je op play klikt.", before, StringComparison.Ordinal);

        await page.Locator("#play").ClickAsync();
        var after = await page.Locator("iframe").GetAttributeAsync("src");
        Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", after);
        Assert.Empty(thumbnails);
    }

    private static async Task<BrowserSession> BrowserAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        return new BrowserSession(playwright, browser, page);
    }

    private static string RepoRoot()
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

    private sealed class BrowserSession(IPlaywright playwright, IBrowser browser, IPage page) : IAsyncDisposable
    {
        public IPage Page { get; } = page;

        public async ValueTask DisposeAsync()
        {
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
