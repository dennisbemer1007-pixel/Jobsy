using Jobsy.Web.Seo;

namespace Jobsy.Web.Hosting;

public static partial class PageAssetCatalog
{
    public const string LeerlingGroep78ThemeStylesheetPath = "css/features/leerling-werelden-kies.css";

    /// <summary>
    /// Groep 7/8 journey pages that need the wereld mockup stylesheet
    /// (<c>leerling-werelden-kies.css</c>). Kept in one place for App.razor, SSR, and tests.
    /// </summary>
    public static bool IncludesLeerlingGroep78ThemeStyles(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        if (string.IsNullOrEmpty(path)
            || !path.StartsWith("/leerling", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.Equals("/leerling", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/leerling?", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/leerling/pdf", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/leerling/pdf?", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/leerling/pauze", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/leerling/vo-paspoort", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.Equals("/leerling/wereld", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/start", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/reis", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/vakken", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/eiland", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/paspoort", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/leerling/droombaan", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/leerling/puzzel", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> LeerlingGroep78ThemeStylesheets(string? rawPath)
        => IncludesLeerlingGroep78ThemeStyles(rawPath)
            ? [WwwrootAssetCatalog.UrlLeadingSlash(LeerlingGroep78ThemeStylesheetPath)]
            : [];
}
