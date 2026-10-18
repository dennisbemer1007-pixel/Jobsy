using System.Text.Json;
using Jobsy.Core.Media;
using Jobsy.Web.Media;
using Microsoft.Playwright;

namespace Jobsy.Tests;

public class ChatRestyleScreenshotTests
{
    private const string OutDir = "/opt/cursor/artifacts/chat-r4";
    private static readonly Guid PreviewVacancyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Render_chat_restyle_preview_screenshots()
    {
        Directory.CreateDirectory(OutDir);
        var root = FindRoot();
        var www = Path.GetFullPath(Path.Combine(root, "Jobsy.Web/wwwroot"));
        var appCss = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var kbCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/kandidaat-banen.css"));
        var mark = BrandImages.MarkSvg;
        var logo = BrandImages.HorizontalSvg;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var vacancyEntry = VacancyDetailEntryBody(mark, logo);

        var shots = new (string Name, string Body, int W, int H)[]
        {
            ("vacancy-mock-interview-entry-1440", vacancyEntry, 1440, 900),
            ("vacancy-mock-interview-entry-390", vacancyEntry, 390, 844),
        };

        foreach (var (name, body, w, h) in shots)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                BaseURL = "https://jobsy.preview/",
            });
            await RegisterWwwrootRoutesAsync(context, www, PreviewDocument(appCss, kbCss, body));
            var page = await context.NewPageAsync();
            await page.GotoAsync("/chat-restyle", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await WaitForBrandImagesAsync(page);
            await AssertBrandImagesLoadedAsync(page);
            await AssertMockInterviewPillVisibleAsync(page, w);
            await AssertMockInterviewPillMedallionDoesNotOverlapLabelAsync(page, w);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutDir, name + ".png"),
            });
        }
    }

    [Theory]
    [InlineData(1440)]
    [InlineData(390)]
    public async Task Vacancy_entry_mock_interview_pill_is_visible(int viewportWidth)
    {
        var root = FindRoot();
        var www = Path.GetFullPath(Path.Combine(root, "Jobsy.Web/wwwroot"));
        var appCss = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var kbCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/kandidaat-banen.css"));
        var mark = BrandImages.MarkSvg;
        var logo = BrandImages.HorizontalSvg;
        var height = viewportWidth >= 1024 ? 900 : 844;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = viewportWidth, Height = height },
            BaseURL = "https://jobsy.preview/",
        });
        await RegisterWwwrootRoutesAsync(context, www, PreviewDocument(appCss, kbCss, VacancyDetailEntryBody(mark, logo)));
        var page = await context.NewPageAsync();
        await page.GotoAsync("/chat-restyle", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await AssertMockInterviewPillVisibleAsync(page, viewportWidth);
        await AssertMockInterviewPillMedallionDoesNotOverlapLabelAsync(page, viewportWidth);
    }

    [Theory]
    [InlineData(1440)]
    [InlineData(390)]
    public async Task Vacancy_entry_mock_interview_pill_medallion_does_not_overlap_label(int viewportWidth)
    {
        var root = FindRoot();
        var www = Path.GetFullPath(Path.Combine(root, "Jobsy.Web/wwwroot"));
        var appCss = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var kbCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/kandidaat-banen.css"));
        var mark = BrandImages.MarkSvg;
        var logo = BrandImages.HorizontalSvg;
        var height = viewportWidth >= 1024 ? 900 : 844;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = viewportWidth, Height = height },
            BaseURL = "https://jobsy.preview/",
        });
        await RegisterWwwrootRoutesAsync(context, www, PreviewDocument(appCss, kbCss, VacancyDetailEntryBody(mark, logo)));
        var page = await context.NewPageAsync();
        await page.GotoAsync("/chat-restyle", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await AssertMockInterviewPillMedallionDoesNotOverlapLabelAsync(page, viewportWidth);
    }

    [Fact]
    public async Task Chat_restyle_preview_brand_images_have_nonzero_natural_width()
    {
        var root = FindRoot();
        var www = Path.GetFullPath(Path.Combine(root, "Jobsy.Web/wwwroot"));
        var appCss = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var kbCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/kandidaat-banen.css"));
        var mark = BrandImages.MarkSvg;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            BaseURL = "https://jobsy.preview/",
        });
        await RegisterWwwrootRoutesAsync(context, www, PreviewDocument(appCss, kbCss, VacancyDetailOpenBody(mark, BrandImages.HorizontalSvg)));
        var page = await context.NewPageAsync();
        await page.GotoAsync("/chat-restyle", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForBrandImagesAsync(page);
        await AssertBrandImagesLoadedAsync(page);
    }

    private static async Task WaitForBrandImagesAsync(IPage page)
    {
        await page.WaitForFunctionAsync("""
            () => {
              const imgs = Array.from(document.querySelectorAll('img')).filter(i => (i.getAttribute('src') || '').includes('/brand/'));
              return imgs.length > 0 && imgs.every(i => i.complete && i.naturalWidth > 0);
            }
            """, null, new() { Timeout = 15_000 });
    }

    private static async Task AssertBrandImagesLoadedAsync(IPage page)
    {
        var broken = await page.EvaluateAsync<string[]>("""
            () => Array.from(document.querySelectorAll('img'))
              .filter(img => {
                const src = img.getAttribute('src') || '';
                return src.includes('/brand/') && img.naturalWidth === 0;
              })
              .map(img => img.getAttribute('src') || '(no src)')
            """);

        Assert.True(broken.Length == 0, "Broken brand images: " + string.Join(", ", broken));
    }

    private static async Task RegisterWwwrootRoutesAsync(IBrowserContext context, string wwwroot, string previewHtml)
    {
        await context.RouteAsync("https://jobsy.preview/**", async route =>
        {
            var uri = new Uri(route.Request.Url);
            var path = uri.AbsolutePath.TrimStart('/');
            if (path is "" or "chat-restyle")
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    ContentType = "text/html; charset=utf-8",
                    Body = previewHtml,
                });
                return;
            }

            var relative = path.Replace('/', Path.DirectorySeparatorChar);
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
                BodyBytes = await File.ReadAllBytesAsync(file),
            });
        });
    }

    private static string ContentTypeFor(string file)
    {
        return Path.GetExtension(file) switch
        {
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".json" => "application/json",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream",
        };
    }

    private static string PreviewDocument(string appCss, string kbCss, string body) =>
        $"""
        <!DOCTYPE html>
        <html lang="nl">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <style>{appCss}</style>
          <style>{kbCss}</style>
        </head>
        <body>{body}</body>
        </html>
        """;

    private static string MockInterviewPillSlot(int viewportWidth)
        => viewportWidth >= 1024 ? ".lobsy-coach-cta--kb-rail" : ".lobsy-coach-cta--kb-mobile";

    private static async Task AssertMockInterviewPillVisibleAsync(IPage page, int viewportWidth)
    {
        var slot = MockInterviewPillSlot(viewportWidth);
        var pill = page.Locator($"{slot} .lobsy-coach-cta__label");
        await Microsoft.Playwright.Assertions.Expect(pill).ToBeVisibleAsync();
        await Microsoft.Playwright.Assertions.Expect(pill).ToHaveTextAsync("Oefen je sollicitatiegesprek");
        var hiddenSlot = viewportWidth >= 1024 ? ".lobsy-coach-cta--kb-mobile" : ".lobsy-coach-cta--kb-rail";
        await Microsoft.Playwright.Assertions.Expect(page.Locator(hiddenSlot)).ToBeHiddenAsync();
    }

    private static async Task AssertMockInterviewPillMedallionDoesNotOverlapLabelAsync(IPage page, int viewportWidth)
    {
        var slot = MockInterviewPillSlot(viewportWidth);
        var metrics = await page.EvaluateAsync<JsonElement>(
            """
            (slot) => {
              const root = document.querySelector(slot + ' .lobsy-coach-cta__btn--warm');
              const medallion = root?.querySelector('.lobsy-coach-cta__medallion');
              const label = root?.querySelector('.lobsy-coach-cta__label');
              if (!root || !medallion || !label) {
                return { overlaps: true, gapPx: -1 };
              }
              const a = medallion.getBoundingClientRect();
              const b = label.getBoundingClientRect();
              const gapPx = b.left - a.right;
              const overlaps = a.right > b.left + 0.5;
              return { overlaps, gapPx };
            }
            """,
            slot);

        var overlaps = metrics.GetProperty("overlaps").GetBoolean();
        var gapPx = metrics.GetProperty("gapPx").GetDouble();
        Assert.False(overlaps, $"Medallion overlaps label (gap {gapPx}px) at {viewportWidth}px.");
        Assert.InRange(gapPx, 8, 14);
    }

    private static string VacancyFallbackPhoto()
        => VacancyImageUrls.Placeholder(PreviewVacancyId, "Zorg");

    private static string MockInterviewPillHtml(string mark, string placement) => $"""
        <div class="lobsy-coach-cta lobsy-coach-cta--kb-{placement}" data-testid="vacancy-mock-interview-cta">
          <button type="button" class="lobsy-coach-cta__btn lobsy-coach-cta__btn--warm" aria-label="Oefen sollicitatiegesprek">
            <span class="lobsy-coach-cta__medallion" aria-hidden="true">
              <img src="{mark}" srcset="{BrandImages.SrcSet56}" sizes="40px" alt="" width="40" height="40" decoding="async" />
            </span>
            <span class="lobsy-coach-cta__label">Oefen je sollicitatiegesprek</span>
          </button>
        </div>
        """;

    private static string CandidateShellHeader(string logo) => $"""
        <header class="app-header">
          <div class="app-header__brand">
            <a class="brand" href="#" aria-label="Lobsy">
              <img class="brand__logo lobsy-logo lobsy-logo--horizontal" src="{logo}" alt="Lobsy" width="120" height="32" decoding="async" />
              <span class="brand__text"><span class="brand-tagline">Vind werk dat bij je past</span></span>
            </a>
          </div>
          <nav class="app-header__nav candidate-header-nav" aria-label="Hoofdnavigatie" data-testid="candidate-header-nav">
            <a class="candidate-header-nav__link is-active" href="#">Banenkaart</a>
            <a class="candidate-header-nav__link" href="#">Sollicitaties</a>
            <a class="candidate-header-nav__link" href="#">Paspoort</a>
          </nav>
          <div class="app-header__actions">
            <a class="auth-link" href="#">Help</a>
            <a class="auth-link" href="#">Inloggen</a>
          </div>
        </header>
        """;

    private static string VacancyDetailEntryBody(string mark, string logo)
    {
        var photo = VacancyFallbackPhoto();
        var pillMobile = MockInterviewPillHtml(mark, "mobile");
        var pillRail = MockInterviewPillHtml(mark, "rail");
        return $"""
        <div class="app-shell app-shell--candidate has-bottom-nav" style="--bottom-nav-h:64px;min-height:100vh;background:var(--bg,#f5f2ee)">
          {CandidateShellHeader(logo)}
          <main class="app-main">
            <div class="app-main__content">
              <div class="panel-page panel-page--vacancy">
                <article class="detail-card kb-detail" data-testid="kb-detail">
                  <div class="detail-card__header-row">
                    <a class="back-link" href="#">Terug naar kaart</a>
                  </div>
                  <div class="kb-detail__layout">
                    <div class="kb-detail__main-top">
                      <div class="kb-detail__photo">
                        <img class="vacancy-photo detail-card__image" src="{photo}" alt="" width="720" height="480" decoding="async" />
                      </div>
                      <div class="kb-detail__header">
                        <h1>Zorgmedewerker dagbesteding</h1>
                        <p class="kb-detail__company-line company"><span>Kasteelhof Zorg</span><span>· Zaandam</span></p>
                      </div>
                      <div class="kb-facts-grid">
                        <div class="kb-facts-grid__item"><span class="kb-facts-grid__label">Uren</span><p class="kb-facts-grid__value">24–32 uur</p></div>
                        <div class="kb-facts-grid__item"><span class="kb-facts-grid__label">Salaris</span><p class="kb-facts-grid__value">€ 2.450 – € 2.850</p></div>
                        <div class="kb-facts-grid__item"><span class="kb-facts-grid__label">Wanneer</span><p class="kb-facts-grid__value">Per direct</p></div>
                        <div class="kb-facts-grid__item"><span class="kb-facts-grid__label">Start</span><p class="kb-facts-grid__value">In overleg</p></div>
                      </div>
                      {pillMobile}
                    </div>
                    <aside class="kb-detail__rail" data-testid="kb-detail-rail">
                      <div class="kb-travel-card">
                        <p class="kb-travel-card__title">Reistijd</p>
                        <p class="kb-travel-card__lead">Ca. 18 min met de fiets vanaf jouw postcode</p>
                      </div>
                      <div class="kb-detail__actions kb-detail__rail-actions">
                        <a class="btn--primary" href="#apply">Solliciteer</a>
                        <div class="kb-detail__secondary">
                          <button class="kb-detail__icon-btn" type="button" aria-label="Bewaar">♡</button>
                        </div>
                        <p class="kb-detail__passport-hint">Vul je paspoort aan voor een snellere sollicitatie.</p>
                      </div>
                      {pillRail}
                    </aside>
                    <div class="kb-detail__main-bottom">
                      <div class="detail-card__body vacancy-description-html">
                        <p>Als zorgmedewerker dagbesteding begeleid je cliënten bij activiteiten, eetmomenten en rustige ochtenden. Je werkt samen in een team van vaste collega&apos;s.</p>
                        <p>We zoeken iemand met affiniteit met ouderenzorg, geduld en een warme stem.</p>
                      </div>
                      <div id="apply" class="apply-flow">
                        <header class="apply-flow__header">
                          <h2 class="apply-flow__title">Solliciteer</h2>
                          <p class="apply-flow__lead">In drie stappen naar je sollicitatie.</p>
                        </header>
                      </div>
                    </div>
                  </div>
                </article>
              </div>
            </div>
          </main>
          <nav class="bottom-nav" aria-label="Navigatie" style="position:fixed;bottom:0;left:0;right:0;height:64px;background:#fff;border-top:1px solid var(--border,#ddd);display:flex;justify-content:space-around;align-items:center;z-index:40">
            <span>Kaart</span><span>Sollicitaties</span><span>Paspoort</span>
          </nav>
        </div>
        """;
    }

    private static string VacancyDetailOpenBody(string mark, string logo) =>
        VacancyDetailEntryBody(mark, logo) + MockInterviewModalBody(mark);

    private static string MockInterviewModalBody(string mark) => $"""
        <div class="share-modal-backdrop lobsy-dialog-backdrop"></div>
        <div class="mock-interview-modal mock-interview-modal--warm lobsy-dialog" role="dialog" aria-modal="true">
          <header class="mock-interview-modal__header">
            <div class="mock-interview-modal__brand">
              <img class="mock-interview-modal__avatar" src="{mark}" alt="" width="40" height="40" decoding="async" />
              <div>
                <h2 id="mock-interview-title">Oefen je sollicitatiegesprek</h2>
                <p class="mock-interview-modal__lead">Vraag het Lobsy</p>
                <p class="mock-interview-modal__vacancy">Zorgmedewerker dagbesteding · Kasteelhof Zorg</p>
              </div>
            </div>
            <button type="button" class="mock-interview-modal__close" aria-label="Sluiten">×</button>
          </header>
          <p class="mock-interview-modal__disclaimer">Coachingsgesprek op basis van deze vacature. Geen echt gesprek met de werkgever. Deel geen BSN, bankgegevens of wachtwoorden. <a class="auth-link mock-interview-modal__disclaimer-link" href="#">Meer over Lobsy</a></p>
          <p class="mock-interview-progress">Stap 1 · Beantwoord de openingsvraag</p>
          <div class="mock-interview-chat">
            <div class="mock-interview-bubble mock-interview-bubble--assistant">
              <div class="mock-interview-bubble__head">
                <img class="mock-interview-bubble__avatar" src="{mark}" alt="" width="32" height="32" decoding="async" />
                <span class="mock-interview-bubble__role">LOBSY</span>
              </div>
              <div class="mock-interview-block mock-interview-block--question">
                <span class="mock-interview-block__label">Vraag</span>
                <p>In de vacature staat dat je cliënten begeleidt bij dagbesteding. Hoe zou jij iemand op zijn gemak stellen op een drukke ochtend?</p>
              </div>
            </div>
          </div>
          <div class="mock-interview__chips"><span class="mock-interview__chip" role="note">Tip: noem iets dat letterlijk in de vacature staat en zeg waarom dat bij jou past.</span></div>
          <form class="mock-interview-compose">
            <div class="mock-interview-compose__row">
              <textarea id="mock-interview-input" rows="3" placeholder="Typ je antwoord"></textarea>
              <button type="button" class="mock-interview-compose__send" aria-label="Verstuur"><span aria-hidden="true">→</span></button>
            </div>
            <div class="mock-interview-compose__foot">
              <button type="button" class="mock-interview-compose__restart">Opnieuw</button>
            </div>
          </form>
        </div>
        """;

    private static string FindRoot()
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
