using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Real Chromium check for the pauze-eiland grid and the pupil Beginnen button.
/// </summary>
[Collection("PlaywrightSmoke")]
public class SchoolRun6PlaywrightTests
{
    [Fact]
    public async Task Island_card_spans_the_question_column_and_beginnen_is_a_button()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var root = RepoRoot();
        var cssPath = Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl">
            <body>
            <div class="ll-reis ll-island">
              <section class="ll-card ll-island-card" data-testid="island-card">
                <div class="ll-chips__row">
                  <button class="ll-chip" type="button">Dieren</button>
                  <button class="ll-chip" type="button">Natuur</button>
                  <button class="ll-chip" type="button">Sport</button>
                  <button class="ll-chip" type="button">Muziek</button>
                  <button class="ll-chip" type="button">Tekenen</button>
                  <button class="ll-chip" type="button">Koken</button>
                </div>
              </section>
              <div class="ll-lob-zone" data-testid="island-lob"><p class="ll-bubble">Hoi</p></div>
              <div class="ll-lob-zone ll-lob-zone--mobile" data-testid="island-lob-mobile"></div>
            </div>
            <div class="ll-start">
              <section class="ll-card">
                <a class="btn-primary" data-testid="beginnen" href="/leerling/reis">Beginnen</a>
              </section>
            </div>
            </body>
            </html>
            """);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = """
                :root {
                  --accent: #0f2d5c;
                  --space-2: 0.5rem;
                  --space-3: 0.75rem;
                  --space-4: 1rem;
                  --space-5: 1.25rem;
                  --surface: #fff;
                  --border: #ddd;
                  --text: #122033;
                  --muted: #5a6a7d;
                  --brand: #0f2d5c;
                  --brand-deep: #0a2044;
                  --text-sm: 14px;
                  --text-xs: 12px;
                  --text-lg: 18px;
                  --text-xl: 20px;
                }
                * { box-sizing: border-box; }
                html, body { margin: 0; }
                """
        });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });

        await page.SetViewportSizeAsync(1280, 800);
        var wide = await MeasureAsync(page);
        Assert.True(wide.CardWidth > 500, $"1280 card width {wide.CardWidth}");
        Assert.True(wide.CardWidth <= 728, $"1280 card width {wide.CardWidth}");
        Assert.True(wide.LobX > wide.CardX, "mascot should sit to the right of the card");

        await page.SetViewportSizeAsync(1024, 800);
        var desktop = await MeasureAsync(page);
        Assert.True(desktop.CardWidth > 400, $"1024 card width {desktop.CardWidth}");
        Assert.True(desktop.LobX > desktop.CardX, "mascot should sit beside the card at 1024");

        await page.SetViewportSizeAsync(390, 844);
        var phone = await MeasureAsync(page);
        Assert.True(phone.CardWidth > 300, $"390 card width {phone.CardWidth}");
        Assert.False(phone.LobVisible, "desktop mascot column is hidden on a phone");

        var button = await page.EvalOnSelectorAsync<ButtonLook>(
            "[data-testid=beginnen]",
            """
            el => {
              const s = getComputedStyle(el);
              return { background: s.backgroundColor, minHeight: s.minHeight, display: s.display };
            }
            """);
        Assert.Equal("rgb(15, 45, 92)", button.Background);
        Assert.Equal("44px", button.MinHeight);
        Assert.Equal("inline-flex", button.Display);
    }

    private static async Task<IslandBox> MeasureAsync(IPage page)
    {
        return await page.EvaluateAsync<IslandBox>(
            """
            () => {
              const card = document.querySelector('[data-testid=island-card]').getBoundingClientRect();
              const lob = document.querySelector('[data-testid=island-lob]');
              const style = getComputedStyle(lob);
              const visible = style.display !== 'none';
              const box = visible ? lob.getBoundingClientRect() : null;
              return {
                cardWidth: card.width,
                cardX: card.x,
                lobX: box ? box.x : 0,
                lobVisible: visible
              };
            }
            """);
    }

    private sealed class IslandBox
    {
        public double CardWidth { get; set; }

        public double CardX { get; set; }

        public double LobX { get; set; }

        public bool LobVisible { get; set; }
    }

    private sealed class ButtonLook
    {
        public string Background { get; set; } = "";

        public string MinHeight { get; set; } = "";

        public string Display { get; set; } = "";
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
}
