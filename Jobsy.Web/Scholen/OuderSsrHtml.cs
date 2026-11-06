using System.Net;
using System.Text;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Scholen;

/// <summary>SSR HTML for GET /ouder (parent passport portal).</summary>
public static class OuderSsrHtml
{
    public const string PagePath = "/ouder";

    public static string RenderDocument(CultureState culture, string pageTitle, string bodyHtml)
    {
        var styles = PageAssetCatalog.StylesheetsForPupilSsr(PagePath);
        var lang = culture.Language;
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

        sb.Append("</head><body><div class=\"ll-shell\" lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><header class=\"ll-topbar\"><a class=\"ll-topbar__brand\" href=\"");
        sb.Append(PagePath);
        sb.Append("\"><img class=\"ll-topbar__logo\" src=\"/");
        sb.Append(Jobsy.Web.Media.BrandImages.Webp64);
        sb.Append("\" width=\"28\" height=\"28\" alt=\"\" /><span>Lobsy</span></a></header><main class=\"ll-main\">");
        sb.Append(bodyHtml);
        sb.Append("</main><footer class=\"ll-legal\"><a href=\"");
        sb.Append(PublicRoutes.Privacy);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"");
        sb.Append(PublicRoutes.Accessibility);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a></footer></div></body></html>");
        return sb.ToString();
    }

    public static string RenderFeatureDisabled(CultureState culture)
        => $"""
            <div class="ll-login"><section class="ll-card"><p class="state-message">{E(culture["Ouder.FeatureDisabled"])}</p></section></div>
            """;

    public static string RenderLoginForm(CultureState culture, string? errorCode, string antiforgeryHtml)
    {
        var error = MapError(culture, errorCode);
        var sb = new StringBuilder();
        sb.Append("<div class=\"ll-login\"><section class=\"ll-card\" data-testid=\"ouder-login\"><h1>");
        sb.Append(E(culture["Ouder.Page.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["Ouder.Page.Lead"]));
        sb.Append("</p>");
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">");
            sb.Append(E(error));
            sb.Append("</p>");
        }

        sb.Append("<form method=\"post\" action=\"/ouder/kijk\" class=\"ll-form\">");
        sb.Append(antiforgeryHtml);
        sb.Append("""
            <label class="ll-field"><span>
            """);
        sb.Append(E(culture["Ouder.Form.Code"]));
        sb.Append("""
            </span><input name="code" type="text" maxlength="7" autocomplete="off" autocapitalize="characters" spellcheck="false" inputmode="text" placeholder="K7Q-M2P" required /></label>
            <button type="submit" class="btn-primary ll-login__submit">
            """);
        sb.Append(E(culture["Ouder.Form.Submit"]));
        sb.Append("</button></form></section></div>");
        return sb.ToString();
    }

    public static string RenderPassport(CultureState culture, PupilG78PassportWebDto passport, string? languageQuery)
    {
        var passportHtml = PupilG78PassportSsrHtml.Render(passport, culture);
        var passportPdf = PdfHref("/ouder/pdf", languageQuery);
        var answersPdf = PdfHref("/ouder/antwoorden/pdf", languageQuery);
        return $"""
            <div class="ll-reis" data-testid="leerling-paspoort-page">
            {passportHtml}
            <nav class="lp-g78-paspoort-actions ouder-downloads" aria-label="{E(culture["Ouder.Downloads.Aria"])}">
            <a class="btn-secondary" href="/ouder/uitloggen">{E(culture["Ouder.Passport.OtherCode"])}</a>
            <a class="btn-secondary" href="{E(passportPdf)}">{E(culture["Ouder.Passport.Pdf"])}</a>
            <a class="btn-secondary" href="{E(answersPdf)}">{E(culture["Ouder.Answers.Pdf"])}</a>
            </nav></div>
            """;
    }

    public static string RenderInvalidSession(CultureState culture)
        => $"""
            <div class="ll-login"><section class="ll-card"><p class="ll-error" role="alert">{E(culture["Ouder.Error.Invalid"])}</p>
            <a class="btn-secondary" href="/ouder/uitloggen">{E(culture["Ouder.Passport.OtherCode"])}</a></section></div>
            """;

    public static string MapError(CultureState culture, string? errorCode)
        => errorCode switch
        {
            "cooldown" => culture["Ouder.Error.Cooldown"],
            "retry" => culture["Ouder.Error.Retry"],
            "staff" => culture["Ouder.Error.Staff"],
            "expired" => culture["Ouder.Error.Expired"],
            null or "" => "",
            _ => culture["Ouder.Error.Invalid"],
        };

    private static string PdfHref(string path, string? languageQuery)
    {
        if (string.IsNullOrWhiteSpace(languageQuery))
        {
            return path;
        }

        return $"{path}?lang={Uri.EscapeDataString(languageQuery)}";
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
