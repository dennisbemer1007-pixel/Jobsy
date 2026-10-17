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
        };

        foreach (var href in sheets)
        {
            var path = href.Split('?', 2)[0];
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
              <div class="ob-wizard">
                <div class="ob-wizard__layout">
                  <main class="ob-wizard__main">
                    <header class="ob-wizard__header">
                      <div class="ob-wizard__header-row">
                        <button type="button" class="icon-btn ob-wizard__back" disabled aria-label="Terug">‹</button>
                        <div class="ob-wizard__segments" role="progressbar" aria-valuenow="1" aria-valuemin="1" aria-valuemax="10">
                          <span><i style="--w:33%"></i></span>
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
            </body>
            </html>
            """;
    }

    private static string Link(string baseUrl, string path)
        => $$"""<link rel="stylesheet" href="{{baseUrl}}/{{path}}" />""";
}
