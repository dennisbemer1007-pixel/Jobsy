using System.Net;
using System.Security.Claims;
using System.Text;
using Jobsy.Core.Authorization;
using Jobsy.Web.Hosting;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;

namespace Jobsy.Web.Scholen;

/// <summary>Minimal HTML document for pupil pages served outside Blazor (no _framework).</summary>
public static class PupilLeerlingSsrFrame
{
    public static string RenderDocument(
        string pageTitle,
        string bodyHtml,
        ClaimsPrincipal pupil)
    {
        var lang = "nl";
        var styles = PageAssetCatalog.StylesheetsForPupilSsr("/leerling/droombaan");
        var chip = "";
        var cls = pupil.FindFirst(PupilClaimTypes.ClassLabel)?.Value;
        var code = pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value;
        if (!string.IsNullOrWhiteSpace(cls) && !string.IsNullOrWhiteSpace(code))
        {
            chip = $"""<span class="ll-topbar__chip">{WebUtility.HtmlEncode($"{cls} · {code}")}</span>""";
        }

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>");
        sb.Append(WebUtility.HtmlEncode(pageTitle));
        sb.Append(" · Lobsy</title>");
        foreach (var href in styles)
        {
            sb.Append("<link rel=\"stylesheet\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" />");
        }

        sb.Append("<link rel=\"stylesheet\" href=\"");
        sb.Append(WwwrootAssetCatalog.Url("css/features/errors.css"));
        sb.Append("\" /></head><body><div class=\"ll-shell\" lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><header class=\"ll-topbar\"><a class=\"ll-topbar__brand\" href=\"/leerling\">");
        sb.Append("<img class=\"ll-topbar__logo\" src=\"/");
        sb.Append(Jobsy.Web.Media.BrandImages.Webp64);
        sb.Append("\" width=\"28\" height=\"28\" alt=\"\" /><span>Lobsy</span></a>");
        sb.Append(chip);
        sb.Append("</header><main class=\"ll-main\">");
        sb.Append(bodyHtml);
        sb.Append("</main><footer class=\"ll-legal\"><a href=\"");
        sb.Append(PublicRoutes.Privacy);
        sb.Append("\">Privacy</a><a href=\"");
        sb.Append(PublicRoutes.Accessibility);
        sb.Append("\">Toegankelijkheid</a></footer></div></body></html>");
        return sb.ToString();
    }
}
