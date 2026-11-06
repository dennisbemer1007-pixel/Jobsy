using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Chromium checks for the warm pupil shell: read-aloud tap size, coach clearance,
/// dream-job labels, the info button and the Groep 8 radio.
/// </summary>
[Collection("PlaywrightSmoke")]
public class SchoolRun8PlaywrightTests
{
    [Fact]
    public async Task Pupil_pages_keep_the_coach_off_controls_at_390px()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 390, Height = 844 } });

        foreach (var name in new[] { "login", "start", "reis", "island", "part", "story", "dream", "stop", "already" })
        {
            await page.SetContentAsync(Shell(name));
            await page.AddStyleTagAsync(new PageAddStyleTagOptions
            {
                Content = """
                    * { box-sizing: border-box; }
                    html, body { margin: 0; }
                    :root {
                      --space-1: 4px; --space-2: 8px; --space-3: 12px; --space-4: 16px;
                      --text-sm: 14px; --muted: #5c5348; --text: #3d342b; --bg: #f6efe4;
                      --surface: #fffaf3; --border: #e6d7c3; --brand: #c4552a; --radius-md: 8px;
                    }
                    """
            });
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });

            var overlap = await page.EvaluateAsync<string[]>(
                """
                () => {
                  const coach = document.querySelector('[data-ll-coach]');
                  const cr = coach.getBoundingClientRect();
                  const nodes = [...document.querySelectorAll('button, input, a.btn, a.btn-primary, a.btn-secondary')]
                    .filter((el) => !el.closest('[data-ll-coach]'));
                  const hits = [];
                  for (const el of nodes) {
                    const r = el.getBoundingClientRect();
                    if (r.width < 1 || r.height < 1) continue;
                    const clear = r.right <= cr.left || r.left >= cr.right || r.bottom <= cr.top || r.top >= cr.bottom;
                    if (!clear) hits.push((el.getAttribute('aria-label') || el.className || el.tagName) + '');
                  }
                  return hits;
                }
                """);
            Assert.Empty(overlap);

            var scroll = await page.EvaluateAsync<int>(
                "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
            Assert.True(scroll <= 1, $"{name} horizontal overflow {scroll}");

            var read = page.Locator("main [data-read-aloud]");
            Assert.True(await read.CountAsync() >= 1);
            var box = await read.First.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.Width >= 44 && box.Height >= 44, $"{name} read-aloud {box.Width}x{box.Height}");
            Assert.Equal("Lees voor", await read.First.GetAttributeAsync("aria-label"));
        }
    }

    [Fact]
    public async Task Info_button_dream_label_and_groep_8_radio_are_usable()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 390, Height = 844 } });
        await page.SetContentAsync(Details());
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = """
                * { box-sizing: border-box; }
                html, body { margin: 0; }
                :root {
                  --space-1: 4px; --space-3: 12px; --space-4: 16px; --text-sm: 14px;
                  --muted: #5c5348; --text: #3d342b; --surface: #fffaf3; --border: #e6d7c3;
                  --brand: #c4552a; --radius-sm: 6px; --success: #2f7d4a; --success-soft: #e7f6ec;
                }
                """
        });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });

        var info = page.Locator(".ll-info-btn");
        Assert.Equal("Meer uitleg", await info.GetAttributeAsync("aria-label"));
        Assert.Equal("false", await info.GetAttributeAsync("aria-expanded"));
        Assert.Equal("ll-imagine-9001", await info.GetAttributeAsync("aria-controls"));
        Assert.Equal(1, await page.Locator("#ll-imagine-9001").CountAsync());

        var undecided = PupilDreamJobText.Label(NlCulture(), "weet-ik-nog-niet");
        var fire = PupilDreamJobText.Label(NlCulture(), "brandweer");
        Assert.Equal(undecided, (await page.Locator("[data-label=Droombaan]").First.InnerTextAsync()).Trim());
        Assert.Equal(fire, (await page.Locator("[data-label=Droombaan]").Nth(1).InnerTextAsync()).Trim());
        Assert.DoesNotContain("weet-ik-nog-niet", await page.Locator("body").InnerTextAsync(), StringComparison.OrdinalIgnoreCase);

        var groep8 = page.Locator("label.sch-seg__opt", new PageLocatorOptions { HasText = "Groep 8" });
        var box = await groep8.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.Height >= 44 && box.Width >= 44);
        await groep8.ClickAsync();
        Assert.True(await page.Locator("input[value='8']").IsCheckedAsync());
    }

    private static CultureState NlCulture()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(new Anonymous());
        services.AddSingleton<Microsoft.JSInterop.IJSRuntime, NoJs>();
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
        return services.BuildServiceProvider().GetRequiredService<CultureState>();
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1366, 900)]
    public async Task Coach_figure_and_bubble_stay_off_controls_while_scrolling(int width, int height)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });

        foreach (var name in new[]
        {
            "login", "start", "reis", "reis-open", "eiland", "part", "story",
            "dream", "dream-result", "stop", "offline"
        })
        {
            await page.SetContentAsync(TallShell(name));
            await page.AddStyleTagAsync(new PageAddStyleTagOptions
            {
                Content = """
                    * { box-sizing: border-box; }
                    html, body { margin: 0; }
                    :root {
                      --space-1: 4px; --space-2: 8px; --space-3: 12px; --space-4: 16px; --space-5: 20px;
                      --space-8: 32px;
                      --text-xs: 12px; --text-sm: 14px; --text-lg: 18px; --text-xl: 22px;
                      --muted: #5c5348; --text: #3d342b; --bg: #f6efe4;
                      --surface: #fffaf3; --border: #e6d7c3; --brand: #c4552a; --brand-deep: #8a3418;
                      --accent: #c4552a; --accent-soft: #f3e2d4; --gold-light: #f6e7c1;
                      --radius-md: 8px; --success: #2f7d4a;
                    }
                    """
            });
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });

            var hits = await page.EvaluateAsync<string[]>(
                """
                () => {
                  const parts = [...document.querySelectorAll('.ll-coach__figure, .ll-coach__bubble')];
                  const nodes = [...document.querySelectorAll('button, input, select, a, label')]
                    .filter((el) => !el.closest('[data-ll-coach]'));
                  const max = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
                  const positions = [];
                  for (let y = 0; y <= max; y += 250) positions.push(y);
                  if (positions.length === 0 || positions[positions.length - 1] !== max) positions.push(max);
                  const hits = [];
                  for (const y of positions) {
                    window.scrollTo(0, y);
                    for (const part of parts) {
                      const cr = part.getBoundingClientRect();
                      if (cr.width < 1 || cr.height < 1) continue;
                      for (const el of nodes) {
                        const r = el.getBoundingClientRect();
                        if (r.width < 1 || r.height < 1) continue;
                        const clear = r.right <= cr.left || r.left >= cr.right || r.bottom <= cr.top || r.top >= cr.bottom;
                        if (!clear) {
                          hits.push(part.className + '@' + y + ':' + (el.getAttribute('aria-label') || el.textContent || el.tagName));
                        }
                      }
                    }
                  }
                  return hits.slice(0, 6);
                }
                """);
            Assert.True(hits.Length == 0, $"{name} {width}x{height} overlap: {string.Join(" | ", hits)}");
        }
    }

    private static string TallShell(string name)
    {
        var popup = name == "reis-open"
            ? """
              <div class="ll-info is-open">
                <p><strong>Stel je voor…</strong></p>
                <p>je komt in een nieuwe klas.</p>
                <button type="button" aria-label="Sluiten">×</button>
              </div>
              """
            : "";
        var answers = name is "reis" or "reis-open"
            ? """
              <label><input type="radio" name="likert" /> Best wel</label>
              <label><input type="radio" name="likert" /> Soms</label>
              <label><input type="radio" name="likert" /> Ja!</label>
              """
            : """
              <label>Klas
                <select aria-label="Klas"><option>7A</option></select>
              </label>
              <input type="text" aria-label="Code" />
              """;
        var action = name switch
        {
            "eiland" => """<button type="button">Klaar, verder!</button>""",
            "offline" => """<button type="button">Probeer opnieuw</button>""",
            "dream-result" => """<a href="/leerling/pdf">Bewaar als PDF</a>""",
            "story" => """<a class="btn-primary" href="/leerling/droombaan">Check je droombaan</a>""",
            "part" => """<a class="btn-primary" href="/leerling/reis">Verder met deel 2</a>""",
            _ => """<a class="btn-primary" href="/leerling/reis">Verder</a>"""
        };
        var controls = $$"""
            <p class="ll-coach-inline">Hoi! Fijn dat je er bent.</p>
            <h1>{{name}}</h1>
            <button type="button" class="read-aloud" data-read-aloud aria-label="Lees voor">Lees voor</button>
            {{popup}}
            {{answers}}
            {{action}}
            <div style="height:520px"></div>
            <button type="button">Midden</button>
            <a href="/leerling/pauze">Pauze</a>
            <div style="height:520px"></div>
            <button type="button">Onderaan</button>
            """;
        var body = name is "reis" or "reis-open" or "story" or "dream" or "dream-result" or "offline" or "part"
            ? $"""
              <div class="ll-reis">
                <aside class="ll-rail"><p>Reis</p></aside>
                <section class="ll-card">{controls}</section>
                <aside class="ll-lob-zone"></aside>
              </div>
              """
            : name == "eiland"
                ? $"""
                  <div class="ll-reis ll-island">
                    <section class="ll-card ll-island-card">{controls}</section>
                    <aside class="ll-lob-zone"></aside>
                  </div>
                  """
                : $"""
                  <div class="{(name == "start" ? "ll-start" : name == "stop" ? "ll-stop" : "ll-login")}">
                    <section class="ll-card">{controls}</section>
                  </div>
                  """;
        return $$"""
            <!DOCTYPE html>
            <html lang="nl"><body>
            <div class="ll-shell">
              <main class="ll-main">
                {{body}}
              </main>
              <footer class="ll-legal">
                <a href="/privacy">Privacy</a>
                <a href="/cookies">Cookies</a>
                <a href="/hulp">Hulp</a>
              </footer>
              <div class="ll-coach" data-ll-coach>
                <div class="ll-coach__bubble">
                  <p>Hoi! Fijn dat je er bent.</p>
                  <button type="button" class="ll-coach__close" aria-label="Tip sluiten">×</button>
                </div>
                <div class="ll-coach__figure"></div>
              </div>
            </div>
            </body></html>
            """;
    }

    private static string Shell(string name)
    {
        var info = name == "reis"
            ? "<button type=\"button\" class=\"ll-info-btn\" aria-label=\"Meer uitleg\" aria-expanded=\"false\" aria-controls=\"ll-imagine-9001\">i</button>"
            : "";
        return $$"""
            <!DOCTYPE html>
            <html lang="nl"><body>
            <div class="ll-shell">
              <main class="ll-main" style="display:flex;flex-direction:column">
                <section class="ll-card" style="margin-top:auto">
                  <h1>{{name}}</h1>
                  {{info}}
                  <button type="button" class="read-aloud" data-read-aloud aria-label="Lees voor">Lees voor</button>
                  <input type="text" aria-label="Code" />
                  <a class="btn-primary" href="/leerling/reis">Verder</a>
                </section>
              </main>
              <div class="ll-coach" data-ll-coach>
                <div class="ll-coach__bubble">
                  <p>Hoi! Fijn dat je er bent.</p>
                  <button type="button" class="read-aloud" data-read-aloud aria-label="Lees voor">Lees voor</button>
                  <button type="button" class="ll-coach__close" aria-label="Tip sluiten">×</button>
                </div>
                <div class="ll-coach__figure"></div>
              </div>
            </div>
            </body></html>
            """;
    }

    private static string Details()
    {
        var culture = NlCulture();
        var undecided = PupilDreamJobText.Label(culture, "weet-ik-nog-niet");
        var fire = PupilDreamJobText.Label(culture, "brandweer");
        return $$"""
            <!DOCTYPE html>
            <html lang="nl"><body>
            <button type="button" class="ll-info-btn" aria-label="Meer uitleg" aria-expanded="false" aria-controls="ll-imagine-9001">i</button>
            <div id="ll-imagine-9001" hidden>Stel je voor</div>
            <table>
              <tr><td data-label="Droombaan">{{undecided}}</td></tr>
              <tr><td data-label="Droombaan">{{fire}}</td></tr>
            </table>
            <div class="sch-seg" role="radiogroup">
              <label class="sch-seg__opt">
                <input type="radio" name="groep" value="7" checked /> Groep 7
              </label>
              <label class="sch-seg__opt">
                <input type="radio" name="groep" value="8" /> Groep 8
              </label>
            </div>
            </body></html>
            """;
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }

    private sealed class Anonymous : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())));
    }

    private sealed class NoJs : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }
}
