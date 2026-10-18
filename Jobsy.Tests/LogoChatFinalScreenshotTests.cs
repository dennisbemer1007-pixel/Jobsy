using Jobsy.Web.Media;
using Microsoft.Playwright;

namespace Jobsy.Tests;

public class LogoChatFinalScreenshotTests
{
    private const string OutDir = "/opt/cursor/artifacts/logo-chat-final";

    [Fact]
    public async Task Render_logo_chat_final_control_captures()
    {
        Directory.CreateDirectory(OutDir);
        var root = FindRoot();
        var www = Path.GetFullPath(Path.Combine(root, "Jobsy.Web/wwwroot"));
        var appCss = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var tokensCss = await File.ReadAllTextAsync(Path.Combine(www, "css/tokens.css"));
        var stijlCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/stijl-a.css"));
        var kbCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/kandidaat-banen.css"));
        var illCss = await File.ReadAllTextAsync(Path.Combine(www, "css/features/candidate-illustrated-app.css"));
        var mark = BrandImages.MarkSvg;
        var logo = BrandImages.HorizontalSvg;
        var fallbackArt = CandidateIllustratedImages.Spot("job-zorg.jpg");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var styles = tokensCss + stijlCss + appCss + kbCss + illCss;
        var entry390 = IllustratedVacancyEntryBody(mark, logo, fallbackArt);
        var entry1440 = entry390;
        var open390 = entry390 + MockInterviewModalBody(mark);

        var shots = new (string Name, string Body, int W, int H, bool ClipHeader)[]
        {
            ("header-with-logo-1440", CandidateShellHeaderOnly(logo), 1440, 220, true),
            ("header-with-logo-390", CandidateShellHeaderOnly(logo), 390, 200, true),
            ("vacancy-chat-entry-1440", entry1440, 1440, 900, false),
            ("vacancy-chat-entry-390", entry390, 390, 844, false),
            ("mock-interview-open-390", open390, 390, 844, false),
        };

        foreach (var (name, body, w, h, clipHeader) in shots)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                BaseURL = "https://jobsy.preview/",
            });
            await RegisterWwwrootRoutesAsync(context, www, PreviewDocument(styles, body));
            var page = await context.NewPageAsync();
            await page.GotoAsync("/logo-chat-final", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (name.StartsWith("vacancy", StringComparison.Ordinal))
            {
                await AssertMockInterviewPillVisibleAsync(page, w);
            }

            if (clipHeader)
            {
                var header = page.Locator(".app-header");
                await header.ScreenshotAsync(new() { Path = Path.Combine(OutDir, name + ".png") });
            }
            else
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, name + ".png"), FullPage = name.StartsWith("vacancy", StringComparison.Ordinal) });
            }
        }

        File.Copy(
            Path.Combine(www, "brand/png/lobsy-apple-touch-icon-180.png"),
            Path.Combine(OutDir, "pwa-apple-touch-icon-180.png"),
            overwrite: true);
        File.Copy(
            Path.Combine(www, "brand/png/lobsy-favicon-32.png"),
            Path.Combine(OutDir, "favicon-32.png"),
            overwrite: true);
    }

    private static async Task AssertMockInterviewPillVisibleAsync(IPage page, int viewportWidth)
    {
        var slot = viewportWidth >= 1024 ? ".lobsy-coach-cta--kb-rail" : ".lobsy-coach-cta--kb-mobile";
        await Microsoft.Playwright.Assertions.Expect(page.Locator($"{slot} .lobsy-coach-cta__label")).ToBeVisibleAsync();
    }

    private static string CandidateShellHeaderOnly(string logo) => $"""
        <div class="app-shell app-shell--candidate">
          <header class="app-header">
            <div class="app-header__brand">
              <a class="brand" href="#" aria-label="Lobsy">
                <img class="brand__logo lobsy-logo lobsy-logo--horizontal" src="{logo}" alt="Lobsy" width="120" height="32" decoding="async" />
                <span class="brand__text"><span class="brand-tagline">Vind werk dat bij je past</span></span>
              </a>
            </div>
            <nav class="app-header__nav candidate-header-nav" aria-label="Hoofdnavigatie">
              <a class="candidate-header-nav__link is-active" href="#">Banenkaart</a>
              <a class="candidate-header-nav__link" href="#">Sollicitaties</a>
              <a class="candidate-header-nav__link" href="#">Paspoort</a>
            </nav>
            <div class="app-header__actions"><a class="auth-link" href="#">Help</a></div>
          </header>
        </div>
        """;

    private static string IllustratedVacancyEntryBody(string mark, string logo, string fallbackArt)
    {
        var pillMobile = MockInterviewPillHtml(mark, "mobile");
        var pillRail = MockInterviewPillHtml(mark, "rail");
        return $"""
        <div class="app-shell app-shell--candidate has-bottom-nav panel-page--cand-ill" style="--bottom-nav-h:64px;min-height:100vh;background:var(--bg,#f5f2ee)">
          {CandidateShellHeaderOnly(logo)}
          <main class="app-main">
            <div class="app-main__content">
              <div class="panel-page panel-page--vacancy panel-page--cand-ill kb-detail--illustrated">
                <article class="detail-card kb-detail kb-detail--warm" data-testid="kb-detail">
                  <div class="detail-card__header-row"><a class="back-link" href="#">Terug naar kaart</a></div>
                  <div class="kb-detail__layout">
                    <div class="kb-detail__main-top">
                      <div class="kb-detail__hero">
                        <div class="kb-detail__photo kb-detail__photo--illustrated" style="--kb-detail-fallback:url('{fallbackArt}')"></div>
                        <div class="kb-detail__hero-body">
                          <div class="kb-detail__header">
                            <h1>Zorgmedewerker dagbesteding</h1>
                            <p class="kb-detail__company-line company"><span>Kasteelhof Zorg</span><span>· Zaandam</span></p>
                          </div>
                          <div class="kb-detail-chips">
                            <div class="kb-detail-chip"><span class="kb-detail-chip__label">Uren</span><span class="kb-detail-chip__value">24–32 uur</span></div>
                            <div class="kb-detail-chip"><span class="kb-detail-chip__label">Salaris</span><span class="kb-detail-chip__value">€ 2.450 – € 2.850</span></div>
                          </div>
                        </div>
                        <div class="kb-detail__hero-accent" aria-hidden="true"></div>
                      </div>
                      {pillMobile}
                    </div>
                    <aside class="kb-detail__rail kb-detail__rail-stack">
                      <div class="kb-detail__actions kb-detail__rail-actions">
                        <a class="btn--primary" href="#apply">Solliciteer</a>
                      </div>
                      {pillRail}
                    </aside>
                    <div class="kb-detail__main-bottom">
                      <section class="kb-detail__section kb-detail__section--copy">
                        <div class="detail-card__body vacancy-description-html"><p>Als zorgmedewerker dagbesteding begeleid je cliënten bij activiteiten en rustige ochtenden.</p></div>
                      </section>
                    </div>
                  </div>
                </article>
              </div>
            </div>
          </main>
        </div>
        """;
    }

    private static string MockInterviewPillHtml(string mark, string placement) => $"""
        <div class="lobsy-coach-cta lobsy-coach-cta--kb-{placement}" data-testid="vacancy-mock-interview-cta">
          <button type="button" class="lobsy-coach-cta__btn lobsy-coach-cta__btn--warm" aria-label="Oefen sollicitatiegesprek">
            <span class="lobsy-coach-cta__medallion" aria-hidden="true">
              <img src="{mark}" alt="" width="40" height="40" decoding="async" />
            </span>
            <span class="lobsy-coach-cta__label">Oefen je sollicitatiegesprek</span>
          </button>
        </div>
        """;

    private static string MockInterviewModalBody(string mark) => $"""
        <div class="share-modal-backdrop lobsy-dialog-backdrop"></div>
        <div class="mock-interview-modal mock-interview-modal--warm lobsy-dialog" role="dialog" aria-modal="true">
          <header class="mock-interview-modal__header">
            <div class="mock-interview-modal__brand">
              <img class="mock-interview-modal__avatar" src="{mark}" alt="" width="40" height="40" decoding="async" />
              <div><h2>Oefen je sollicitatiegesprek</h2><p class="mock-interview-modal__lead">Vraag het Lobsy</p></div>
            </div>
            <button type="button" class="mock-interview-modal__close" aria-label="Sluiten">×</button>
          </header>
          <p class="mock-interview-modal__disclaimer">Coachingsgesprek op basis van deze vacature. <a class="auth-link mock-interview-modal__disclaimer-link" href="#">Meer over Lobsy</a></p>
          <div class="mock-interview-chat">
            <div class="mock-interview-bubble mock-interview-bubble--assistant">
              <div class="mock-interview-bubble__head">
                <img class="mock-interview-bubble__avatar" src="{mark}" alt="" width="32" height="32" decoding="async" />
                <span class="mock-interview-bubble__role">LOBSY</span>
              </div>
              <p>Hoe zou jij iemand op zijn gemak stellen op een drukke ochtend?</p>
            </div>
          </div>
        </div>
        """;

    private static async Task RegisterWwwrootRoutesAsync(IBrowserContext context, string wwwroot, string previewHtml)
    {
        await context.RouteAsync("https://jobsy.preview/**", async route =>
        {
            var path = new Uri(route.Request.Url).AbsolutePath.TrimStart('/');
            if (path is "" or "logo-chat-final")
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

            var ext = Path.GetExtension(file);
            var contentType = ext switch
            {
                ".css" => "text/css",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream",
            };
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = contentType,
                BodyBytes = await File.ReadAllBytesAsync(file),
            });
        });
    }

    private static string PreviewDocument(string css, string body) =>
        $"""<!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/><style>{css}</style></head><body>{body}</body></html>""";

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
