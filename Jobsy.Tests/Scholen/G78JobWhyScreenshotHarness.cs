using Jobsy.Web.Hosting;

namespace Jobsy.Tests.Scholen;

internal static class G78JobWhyScreenshotHarness
{
    public static string InlinePaspoortStyles(string repoRoot)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web/wwwroot");
        var blobs = new List<string>();
        foreach (var href in PageAssetCatalog.StylesheetsForPupilSsr("/leerling/paspoort"))
        {
            var rel = href.TrimStart('/').Split('?')[0];
            var path = Path.Combine(wwwroot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                blobs.Add(File.ReadAllText(path));
            }
        }

        return string.Join('\n', blobs);
    }

    public static string BuildPaspoortJobCardHtml(
        string jobTitle,
        string domainLabel,
        string whyBodyHtml,
        int maxCardWidth)
    {
        var jobKey = jobTitle.ToLowerInvariant().Replace(' ', '-');
        return $$"""
            <body class="ll-g78-shell" style="margin:0;background:#f5f2ee;padding:16px">
            <div class="lp-g78" style="max-width:{{maxCardWidth}}px;margin:0 auto">
            <article class="lp-g78__card lp-g78__job" style="position:relative;padding:12px 14px">
            <div class="g78-job-why-wrap">
            <button type="button" class="ll-info-btn g78-job-why-btn" aria-label="Meer info over {{jobTitle}}" aria-expanded="true">i</button>
            <div id="g78-job-why-{{jobKey}}" class="ll-info g78-job-why-panel is-open">
            <button type="button" class="ll-info__close" aria-label="Sluiten">×</button>
            <p class="g78-job-why-panel__title"><b>{{jobTitle}}</b></p>
            <div class="g78-job-why-panel__body">{{whyBodyHtml}}</div>
            </div></div>
            <div class="lp-g78__job-k">{{domainLabel}}</div>
            <h4>{{jobTitle}}</h4>
            </article></div></body>
            """;
    }

    public static string FullHtmlDocument(string repoRoot, string bodyInner)
    {
        var css = InlinePaspoortStyles(repoRoot);
        return $$"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8" />
            <link rel="preconnect" href="https://fonts.googleapis.com" />
            <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
            <link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,700&amp;family=Lexend:wght@400;600&amp;display=swap" rel="stylesheet" />
            <style>{{css}}</style>
            </head>
            {{bodyInner}}
            </html>
            """;
    }
}
