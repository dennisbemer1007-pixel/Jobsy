using System.Net;
using System.Security.Claims;
using System.Text;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Hosting;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;

namespace Jobsy.Web.Scholen;

/// <summary>Minimal HTML document for pupil pages rendered outside Blazor (no _framework).</summary>
public static class PupilLeerlingSsrFrame
{
    public static string RenderDocument(
        string pageTitle,
        string bodyHtml,
        ClaimsPrincipal pupil,
        bool useGroep78Shell = false,
        string? journeyThemeKey = null)
    {
        var lang = "nl";
        var routePath = useGroep78Shell ? "/leerling/droombaan" : "/leerling/droombaan";
        var styles = PageAssetCatalog.StylesheetsForPupilSsr(routePath);
        var chip = BuildChip(pupil);
        var themeClass = useGroep78Shell && PupilJourneyTheme.IsValid(journeyThemeKey)
            ? PupilJourneyTheme.Groep78ShellClass(journeyThemeKey)
            : "";

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
        sb.Append(WwwrootAssetCatalog.UrlLeadingSlash("css/features/errors.css"));
        sb.Append("\" /></head><body>");

        if (useGroep78Shell)
        {
            sb.Append("<div class=\"ll-g78-shell ");
            sb.Append(WebUtility.HtmlEncode(themeClass));
            sb.Append("\" lang=\"");
            sb.Append(WebUtility.HtmlEncode(lang));
            sb.Append("\"><header class=\"top\"><a class=\"brand\" href=\"/leerling/wereld\">");
            sb.Append("<img src=\"/leerling/werelden/img/embleem.png\" width=\"40\" height=\"40\" alt=\"\" />");
            sb.Append("Lobsy<small>");
            sb.Append(WebUtility.HtmlEncode("Groep 7 en 8"));
            sb.Append("</small></a><div class=\"sp\"></div>");
            sb.Append(chip);
            sb.Append("""<form method="post" action="/leerling/pauze"><button type="submit" class="btn-pauze"><svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg><span>Pauze</span></button></form>""");
            sb.Append("</header>");
            sb.Append(bodyHtml);
            sb.Append("<footer><a href=\"");
            sb.Append(PublicRoutes.Privacy);
            sb.Append("\">Privacyverklaring</a><a href=\"");
            sb.Append(PublicRoutes.Accessibility);
            sb.Append("\">Toegankelijkheid</a></footer></div>");
        }
        else
        {
            sb.Append("<div class=\"ll-shell\" lang=\"");
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
            sb.Append("\">Toegankelijkheid</a></footer></div>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string BuildChip(ClaimsPrincipal pupil)
    {
        var cls = pupil.FindFirst(PupilClaimTypes.ClassLabel)?.Value;
        var code = pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value;
        if (string.IsNullOrWhiteSpace(cls) || string.IsNullOrWhiteSpace(code))
        {
            return "";
        }

        var label = WebUtility.HtmlEncode($"{cls} · {code}");
        return $"""<span class="chip-code">{label}</span>""";
    }
}
