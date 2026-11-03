using System.Net;
using System.Security.Claims;
using System.Text;
using Jobsy.Core.Authorization;
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
        => RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = pageTitle,
            BodyHtml = bodyHtml,
            Pupil = pupil,
            RoutePath = useGroep78Shell ? "/leerling/reis" : "/leerling/droombaan",
            UseGroep78Shell = useGroep78Shell,
            JourneyThemeKey = journeyThemeKey,
            IncludeJourneyScripts = true,
        });

    public static string RenderDocument(PupilLeerlingSsrDocumentOptions options)
    {
        var lang = "nl";
        var routePath = options.RoutePath ?? "/leerling/reis";
        var styles = PageAssetCatalog.StylesheetsForPupilSsr(routePath);
        var chip = BuildChip(options.Pupil);
        var themeClass = options.UseGroep78Shell
            ? PupilJourneyTheme.Groep78ShellClass(PupilJourneyTheme.NormalizeOrDefault(options.JourneyThemeKey))
            : PupilJourneyTheme.Groep78ShellClass(PupilJourneyTheme.ZeeExpeditie);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>");
        sb.Append(WebUtility.HtmlEncode(options.PageTitle));
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

        if (options.UseMboHboShell)
        {
            RenderMboHboShell(sb, options, chip, lang);
        }
        else if (options.UseGroep78Shell)
        {
            sb.Append("<div class=\"ll-g78-shell ");
            sb.Append(WebUtility.HtmlEncode(themeClass));
            sb.Append("\" lang=\"");
            sb.Append(WebUtility.HtmlEncode(lang));
            sb.Append("\" data-testid=\"leerling-g78-shell\"><header class=\"top\"><a class=\"brand\" href=\"/leerling/wereld\">");
            sb.Append("<img src=\"/leerling/werelden/img/embleem.png\" width=\"40\" height=\"40\" alt=\"\" />");
            sb.Append("Lobsy<small>");
            sb.Append(WebUtility.HtmlEncode(BrandSmallLabel(options)));
            sb.Append("</small></a><div class=\"sp\"></div>");
            sb.Append(chip);
            if (options.ShowPause)
            {
                sb.Append(options.PauseFormHtml);
            }

            sb.Append("</header>");
            sb.Append(options.BodyHtml);
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
            if (options.ShowPause)
            {
                sb.Append(options.PauseFormHtml);
            }

            sb.Append("</header><main class=\"ll-main\">");
            sb.Append(options.BodyHtml);
            sb.Append("</main><footer class=\"ll-legal\"><a href=\"");
            sb.Append(PublicRoutes.Privacy);
            sb.Append("\">Privacy</a><a href=\"");
            sb.Append(PublicRoutes.Accessibility);
            sb.Append("\">Toegankelijkheid</a></footer></div>");
        }

        if (options.IncludeJourneyScripts)
        {
            sb.Append("<script src=\"");
            sb.Append(WwwrootAssetCatalog.UrlLeadingSlash("js/read-aloud.js"));
            sb.Append("\" defer></script><script src=\"");
            sb.Append(WwwrootAssetCatalog.UrlLeadingSlash("js/leerling-journey-ssr.js"));
            sb.Append("\" defer></script>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void RenderMboHboShell(StringBuilder sb, PupilLeerlingSsrDocumentOptions options, string chip, string lang)
    {
        var path = PageSeoCatalog.Normalize(options.RoutePath);
        var showOverview = !string.Equals(path, "/leerling/mbo-hbo/overzicht", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(path, "/leerling/mbo-hbo/uitleg", StringComparison.OrdinalIgnoreCase);
        sb.Append("<div class=\"mhbo-root\" lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><div class=\"mhbo-top\"><a href=\"/leerling/mbo-hbo/overzicht\">");
        sb.Append("<img src=\"/brand/svg/lobsy-logo-horizontaal.svg\" alt=\"Lobsy\" /></a><span class=\"sp\"></span>");
        if (showOverview)
        {
            sb.Append("<a class=\"mhbo-lnk\" href=\"/leerling/mbo-hbo/overzicht\">Naar overzicht</a>");
        }

        if (!string.IsNullOrEmpty(chip))
        {
            var code = options.Pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value;
            sb.Append("<span class=\"mhbo-chip\">Code ");
            sb.Append(WebUtility.HtmlEncode(code));
            sb.Append("</span>");
        }

        sb.Append("</div>");
        sb.Append(options.BodyHtml);
        sb.Append("</div>");
    }

    public static string PauseForm(
        string antiforgeryField,
        string label,
        string? part = null,
        bool groep78 = false,
        string? journeyThemeKey = null,
        string? journeyNodeKey = null,
        int? answeredCount = null,
        int? totalItems = null,
        bool journeyCompleted = false)
    {
        var sb = new StringBuilder();
        sb.Append("""<form method="post" action="/leerling/pauze" class="ll-topbar__pause-form" data-ll-pause-form>""");
        sb.Append(antiforgeryField);
        if (!string.IsNullOrEmpty(part))
        {
            sb.Append("<input type=\"hidden\" name=\"part\" value=\"");
            sb.Append(WebUtility.HtmlEncode(part));
            sb.Append("\" />");
        }

        if (groep78)
        {
            sb.Append("""<input type="hidden" name="g78" value="1" />""");
            if (PupilJourneyTheme.IsValid(journeyThemeKey))
            {
                sb.Append("<input type=\"hidden\" name=\"theme\" value=\"");
                sb.Append(WebUtility.HtmlEncode(journeyThemeKey));
                sb.Append("\" />");
            }

            if (!string.IsNullOrWhiteSpace(journeyNodeKey))
            {
                sb.Append("<input type=\"hidden\" name=\"jn\" value=\"");
                sb.Append(WebUtility.HtmlEncode(journeyNodeKey));
                sb.Append("\" />");
            }

            if (answeredCount is int answered)
            {
                sb.Append("<input type=\"hidden\" name=\"ja\" value=\"");
                sb.Append(answered.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("\" />");
            }

            if (totalItems is int total)
            {
                sb.Append("<input type=\"hidden\" name=\"jt\" value=\"");
                sb.Append(total.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("\" />");
            }

            if (journeyCompleted)
            {
                sb.Append("""<input type="hidden" name="jc" value="1" />""");
            }
        }

        sb.Append("""<button type="submit" class="btn-pauze"><svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg><span>""");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</span></button></form>");
        return sb.ToString();
    }

    private static string BrandSmallLabel(PupilLeerlingSsrDocumentOptions options)
    {
        if (string.Equals(options.RoutePath, "/leerling/wereld", StringComparison.OrdinalIgnoreCase))
        {
            return "Groep 7 en 8";
        }

        if (options.UseGroep78Shell && PupilJourneyTheme.IsValid(options.JourneyThemeKey))
        {
            return options.JourneyThemeKey switch
            {
                PupilJourneyTheme.Eilandtuin => "Eilandtuin",
                PupilJourneyTheme.Sterrenkust => "Sterrenkust",
                _ => "Expeditie",
            };
        }

        return "Groep 7 en 8";
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

public sealed class PupilLeerlingSsrDocumentOptions
{
    public required string PageTitle { get; init; }
    public required string BodyHtml { get; init; }
    public required ClaimsPrincipal Pupil { get; init; }
    public string? RoutePath { get; init; }
    public bool UseGroep78Shell { get; init; }
    public bool UseMboHboShell { get; init; }
    public string? JourneyThemeKey { get; init; }
    public bool IncludeJourneyScripts { get; init; } = true;
    public bool ShowPause { get; init; } = true;
    public string PauseFormHtml { get; init; } = """<form method="post" action="/leerling/pauze"><button type="submit" class="btn-pauze"><svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg><span>Pauze</span></button></form>""";
}
