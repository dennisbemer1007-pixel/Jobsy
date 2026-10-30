using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

/// <summary>Builds a styled HTML shell matching /candidate/start CSS bundles for screenshot exports.</summary>
public static class PersonalStepScreenshotExporter
{
    public const string ExportRoute = "/candidate/start";

    public static string BuildDocument(string fieldsInnerHtml, string staticBaseUrl)
    {
        var baseUrl = staticBaseUrl.TrimEnd('/');
        var areas = PageAssetCatalog.ResolveAreas(ExportRoute);
        var sheets = PageAssetCatalog.StylesheetsFor(areas);

        var links = new List<string>
        {
            Link(baseUrl, "css/critical.css"),
            Link(baseUrl, "css/features/onboarding-wizard-mobile-bar.css"),
            Link(baseUrl, "css/features/onboarding-wizard-richting-a.css"),
            Link(baseUrl, "css/features/onboarding-wizard-vroeg-resultaat.css"),
        };

        foreach (var href in sheets)
        {
            var path = href.Split('?', 2)[0];
            if (links.Any(l => l.Contains(path, StringComparison.Ordinal)))
            {
                continue;
            }

            links.Add(Link(baseUrl, path));
        }

        var linkTags = string.Join("\n  ", links);

        return $$"""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              {{linkTags}}
            </head>
            <body>
              <div class="app-shell app-shell--questionnaire">
              <div class="ob-wizard ob-wizard--richting-a">
                <div class="ob-wizard__layout">
                  <aside class="ob-wizard__sidebar">
                    <div class="lobsy-logo">
                      <img class="lobsy-logo__img" src="{{baseUrl}}/images/brand/lobsy-128.png" alt="" />
                    </div>
                    <div class="ob-wizard__side-item is-current">
                      <span>1</span>
                      <div>
                        <strong>Over jou</strong>
                        <small>3 stappen</small>
                      </div>
                    </div>
                    <div class="ob-wizard__side-item">
                      <span>2</span>
                      <div>
                        <strong>Je achtergrond</strong>
                        <small>optioneel</small>
                      </div>
                    </div>
                    <div class="ob-wizard__side-item">
                      <span>3</span>
                      <div>
                        <strong>Je droombaan</strong>
                        <small>1 stap</small>
                      </div>
                    </div>
                    <div class="ob-wizard__side-item">
                      <span>4</span>
                      <div>
                        <strong>Wie ben jij</strong>
                        <small>4 korte tests</small>
                      </div>
                    </div>
                    <div class="ob-wizard__side-item">
                      <span>5</span>
                      <div>
                        <strong>Jouw Kompas</strong>
                        <small>bijna klaar</small>
                      </div>
                    </div>
                    <p class="ob-wizard__saved">✓ Alles bewaard</p>
                    <button type="button" class="ob-wizard__ghost" disabled>Later verder</button>
                  </aside>
                  <main class="ob-wizard__main">
                    <header class="ob-wizard__header">
                      <div class="ob-wizard__header-row">
                        <button type="button" class="icon-btn ob-wizard__back" disabled aria-label="Terug">‹</button>
                        <div class="ob-wizard__segments" role="progressbar" aria-valuenow="1" aria-valuemin="1" aria-valuemax="10">
                          <span><i style="--w:10%"></i></span>
                          <span><i style="--w:0%"></i></span>
                          <span><i style="--w:0%"></i></span>
                          <span><i style="--w:0%"></i></span>
                        </div>
                        <button type="button" class="ob-wizard__later">Later verder</button>
                      </div>
                      <div class="ob-wizard__status-row">
                        <span><strong>Over jou</strong> · stap 1 van 10 · nog ± 6 min</span>
                        <span>✓ Bewaard</span>
                      </div>
                    </header>
                    <section class="ob-wizard__content">
                      {{fieldsInnerHtml}}
                    </section>
                  </main>
                </div>
              </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string Link(string baseUrl, string path)
        => $$"""<link rel="stylesheet" href="{{baseUrl}}/{{path}}" />""";
}
