using Jobsy.Web.Media;
using Microsoft.Playwright;

namespace Jobsy.Tests;

public class ChatRestyleScreenshotTests
{
    private const string OutDir = "/opt/cursor/artifacts/chat-r2";

    [Fact]
    public async Task Render_chat_restyle_preview_screenshots()
    {
        Directory.CreateDirectory(OutDir);
        var root = FindRoot();
        var www = Path.GetFullPath(Path.Combine(root, "Jobsy.Web/wwwroot"));
        var appCss = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var kbCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/kandidaat-banen.css"));
        var mark = BrandImages.MarkSvg;
        var sil = BrandImages.ChatSilhouetteSvg;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var vacancyEntry = VacancyDetailEntryBody(mark);
        var vacancyOpen = VacancyDetailOpenBody(mark);

        var shots = new (string Name, string Body, int W, int H)[]
        {
            ("vacancy-mock-interview-entry-1440", vacancyEntry, 1440, 900),
            ("vacancy-mock-interview-entry-390", vacancyEntry, 390, 844),
            ("vacancy-mock-interview-open-1440", vacancyOpen, 1440, 900),
            ("vacancy-mock-interview-open-390", vacancyOpen, 390, 844),
            ("candidate-sollicitaties-1440-closed", WarmBody(mark, false), 1440, 900),
            ("candidate-sollicitaties-1440-open", WarmBody(mark, true), 1440, 900),
            ("candidate-sollicitaties-390-closed", WarmBody(mark, false), 390, 844),
            ("candidate-sollicitaties-390-open", WarmBody(mark, true), 390, 844),
            ("employer-1440-closed", BusinessBody(sil, false), 1440, 900),
            ("employer-1440-open", BusinessBody(sil, true), 1440, 900),
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
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutDir, name + ".png"),
            });
        }
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
        await RegisterWwwrootRoutesAsync(context, www, PreviewDocument(appCss, kbCss, VacancyDetailOpenBody(mark) + WarmBody(mark, true)));
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

    private static string VacancyDetailEntryBody(string mark) => $"""
        <div class="app-shell app-shell--candidate has-bottom-nav" style="--bottom-nav-h:64px;min-height:100vh;background:var(--bg,#f5f2ee)">
          <main class="app-main">
            <div class="app-main__content">
              <div class="panel-page panel-page--vacancy">
                <article class="detail-card kb-detail" data-testid="kb-detail">
                  <div class="detail-card__header-row">
                    <a class="back-link" href="#">Terug naar kaart</a>
                  </div>
                  <div class="kb-detail__layout">
                    <div class="kb-detail__main-top">
                      <div class="kb-detail__photo" role="img" aria-label="Vacaturefoto" style="background:linear-gradient(135deg,#d4e4f7 0%,#f0e6dc 100%)"></div>
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
                    </aside>
                    <div class="kb-detail__main-bottom">
                      <div class="detail-card__body vacancy-description-html">
                        <p>Als zorgmedewerker dagbesteding begeleid je cliënten bij activiteiten, eetmomenten en rustige ochtenden. Je werkt samen in een team van vaste collega&apos;s.</p>
                        <p>We zoeken iemand met affiniteit met ouderenzorg, geduld en een warme stem.</p>
                      </div>
                      <div class="lobsy-coach-cta">
                        <button type="button" class="lobsy-coach-cta__btn lobsy-coach-cta__btn--warm" aria-label="Oefen sollicitatiegesprek">
                          <span class="lobsy-coach-cta__medallion" aria-hidden="true">
                            <img src="{mark}" srcset="{BrandImages.SrcSet56}" sizes="56px" alt="" width="56" height="56" decoding="async" />
                          </span>
                          <span class="lobsy-coach-cta__label">Oefen je sollicitatiegesprek</span>
                        </button>
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

    private static string VacancyDetailOpenBody(string mark) =>
        VacancyDetailEntryBody(mark) + MockInterviewModalBody(mark);

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
          <p class="mock-interview-modal__disclaimer">Coachingsgesprek op basis van deze vacature. Geen echt gesprek met de werkgever. Deel geen BSN, bankgegevens of wachtwoorden. <a class="auth-link" href="#">Meer over Lobsy</a></p>
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

    private static string WarmBody(string mark, bool open)
    {
        var pageName = "Sollicitaties";
        var greeting = "Hoi Sanne. Je hebt dinsdag een gesprek bij Bakkerij De Korenaar, mooi zeg. Zal ik je helpen het voor te bereiden?";
        var openPanel = open
            ? $"""
        <div class="lobsy-assistant is-open lobsy-assistant--skin-warm lobsy-assistant--coach has-bottom-nav">
          <div id="lobsy-assistant-panel" class="lobsy-assistant__panel">
            <header class="lobsy-assistant__header">
              <div class="lobsy-assistant__brand">
                <img class="lobsy-assistant__avatar" src="{mark}" width="40" height="40" alt="" decoding="async" />
                <div><h2>Lobsy</h2><p class="lobsy-assistant__lead">Vraag het Lobsy</p></div>
              </div>
              <div class="lobsy-assistant__header-actions"><button class="lobsy-assistant__icon-btn" type="button">+</button><button class="lobsy-assistant__icon-btn" type="button">×</button></div>
            </header>
            <p class="lobsy-assistant__context"><span class="lobsy-assistant__context-dot"></span>Je bekijkt nu {pageName}, ik denk met je mee</p>
            <div class="lobsy-assistant__chat">
              <div class="lobsy-assistant__bubble lobsy-assistant__bubble--assistant">
                <div class="lobsy-assistant__bubble-head">
                  <img class="lobsy-assistant__bubble-logo" src="{mark}" width="32" height="32" alt="" decoding="async" />
                  <span>Lobsy</span>
                </div>
                <p>{greeting}</p>
              </div>
            </div>
            <div class="lobsy-assistant__chips">
              <button type="button" class="lobsy-assistant__chip">Ja, help me voorbereiden</button>
              <button type="button" class="lobsy-assistant__chip">Wat betekent &apos;Bekeken&apos;?</button>
            </div>
            <form class="lobsy-assistant__compose">
              <div class="lobsy-assistant__compose-row"><textarea placeholder="Typ je vraag"></textarea><button type="button" class="lobsy-assistant__send">→</button></div>
            </form>
            <p class="lobsy-assistant__disclaimer">Lobsy is een AI-assistent en geeft geen persoonlijk advies. <a href="#">Meer over Lobsy</a></p>
          </div>
        </div>
        """
            : "";
        return $"""
        <div class="app-shell has-bottom-nav" style="--bottom-nav-h:64px;min-height:900px;background:#f5f2ee">
          <main class="app-main"><div class="app-main__content"><div class="card" style="max-width:720px;margin:2rem auto;padding:1rem;background:#fff;border-radius:12px"><h1>Sollicitaties</h1></div></div></main>
          <nav class="bottom-nav" style="position:fixed;bottom:0;left:0;right:0;height:64px;background:#fff;border-top:1px solid #ddd;display:flex;justify-content:space-around;align-items:center;z-index:50"><span>Home</span><span>Sollicitaties</span><span>Paspoort</span></nav>
          {openPanel}
          <div class="lobsy-coach-dock" data-lobsy-coach>
            <button id="lobsy-coach-btn" type="button" class="lobsy-coach-dock__btn lobsy-coach-dock__btn--warm">
              <span class="lobsy-coach-dock__medallion"><img src="{mark}" width="56" height="56" alt="" decoding="async" /></span>
              <span class="lobsy-coach-dock__label">Vraag het Lobsy</span>
            </button>
          </div>
        </div>
        """;
    }

    private static string BusinessBody(string sil, bool open)
    {
        var launcher = open
            ? ""
            : $"""<div class="lobsy-assistant-launcher lobsy-assistant-launcher--business has-bottom-nav"><button type="button" class="lobsy-assistant-launcher__btn"><img src="{sil}" width="28" height="28" alt="" decoding="async" /></button></div>""";
        var panel = open
            ? $"""
        <div class="lobsy-assistant is-open lobsy-assistant--skin-business lobsy-assistant--business-panel has-bottom-nav">
          <div class="lobsy-assistant__panel">
            <header class="lobsy-assistant__header">
              <div class="lobsy-assistant__brand">
                <span class="lobsy-assistant__avatar lobsy-assistant__avatar--business"><img src="{sil}" width="24" height="24" alt="" decoding="async" /></span>
                <div><h2>Lobsy-assistent</h2><p class="lobsy-assistant__lead">Antwoord op je vragen over Lobsy</p></div>
              </div>
            </header>
            <div class="lobsy-assistant__chat">
              <div class="lobsy-assistant__bubble lobsy-assistant__bubble--assistant">
                <div class="lobsy-assistant__bubble-head">
                  <span class="lobsy-assistant__avatar lobsy-assistant__avatar--business"><img src="{sil}" width="20" height="20" alt="" decoding="async" /></span>
                  <span>Lobsy</span>
                </div>
                <p>Hoi Sanne. Ik help je met vragen over vacatures en sollicitaties.</p>
              </div>
            </div>
            <form class="lobsy-assistant__compose"><div class="lobsy-assistant__compose-row"><textarea placeholder="Typ je vraag"></textarea><button type="button" class="lobsy-assistant__send">→</button></div></form>
          </div>
        </div>
        """
            : "";
        return $"""
        <div class="wg-shell wg-has-bottom-nav" style="--bottom-nav-h:64px;min-height:900px;background:#eef1f5">
          <main class="wg-main" style="padding:1rem"><h1>Vacatures</h1></main>
          {launcher}
          {panel}
        </div>
        """;
    }

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
