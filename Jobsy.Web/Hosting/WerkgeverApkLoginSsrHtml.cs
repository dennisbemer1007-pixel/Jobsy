using System.Net;
using System.Text;
using Jobsy.Web.Media;

namespace Jobsy.Web.Hosting;

/// <summary>SSR login for /loopbaan-apk — code only, leerling-style chrome + Loopbaan-APK huisstijl.</summary>
public static class WerkgeverApkLoginSsrHtml
{
    public static string RenderDocument(string? error, string antiforgeryHtml)
    {
        var styles = PageAssetCatalog.StylesheetsForPupilSsr("/loopbaan-apk")
            .Concat(
            [
                WwwrootAssetCatalog.UrlLeadingSlash("werkgever-apk/css/shared.css"),
                WwwrootAssetCatalog.UrlLeadingSlash("werkgever-apk/css/extra.css"),
            ])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"nl\"><head>");
        sb.Append("<meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>Loopbaan-APK inloggen · Lobsy</title>");
        foreach (var href in styles)
        {
            sb.Append("<link rel=\"stylesheet\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" />");
        }

        sb.Append("</head><body class=\"welcome login\"><div class=\"app\">");
        sb.Append("<header class=\"topbar\"><a class=\"brand\" href=\"/loopbaan-apk\">");
        sb.Append("<span class=\"logo-tile\"><img src=\"/");
        sb.Append(BrandImages.Webp64);
        sb.Append("\" width=\"32\" height=\"32\" alt=\"\" /></span>");
        sb.Append("<span class=\"brand-name\">Lobsy</span></a></header>");
        sb.Append("<main class=\"main ll-main\"><section class=\"ll-card ll-login\"><h1>Jouw Loopbaan-APK</h1>");
        sb.Append("<p class=\"muted\">Vul je persoonlijke code in. Je kunt dit thuis in je eigen tijd doen.</p>");
        if (!string.IsNullOrWhiteSpace(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(error));
            sb.Append("</p>");
        }

        sb.Append("<form method=\"post\" action=\"/loopbaan-apk/login\" class=\"ll-form\">");
        sb.Append(antiforgeryHtml);
        sb.Append("<label class=\"ll-field\"><span>Jouw code</span>");
        sb.Append("<input name=\"code\" type=\"text\" maxlength=\"12\" autocomplete=\"off\" autocapitalize=\"characters\" ");
        sb.Append("spellcheck=\"false\" inputmode=\"text\" placeholder=\"Bijv. K7Q-M2P\" required /></label>");
        sb.Append("<p class=\"ll-privacy\">We slaan geen naam op. Alleen jouw code telt.</p>");
        sb.Append("<button type=\"submit\" class=\"btn-primary ll-login__submit\">Start →</button>");
        sb.Append("</form></section></main>");
        sb.Append("""
<button type="button" class="coach" data-ssr-coach-open aria-label="Vraag hulp aan Lobsy">
  <span class="bubble">Waar kan ik je bij helpen?</span>
  <img class="full" src="/werkgever-apk/img/lobsy-coach-b2.png" alt="">
</button>
""");
        sb.Append("</div><script src=\"");
        sb.Append(WwwrootAssetCatalog.UrlLeadingSlash("js/lobsy-coach-ssr.js"));
        sb.Append("\" defer></script></body></html>");
        return sb.ToString();
    }
}