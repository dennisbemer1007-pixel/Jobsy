using Jobsy.Web.Seo;

namespace Jobsy.Web.Hosting;

public static partial class PageAssetCatalog
{
    public const string LeerlingMboHboStylesheetPath = "css/features/leerling-mbo-hbo.css";

    public static bool IncludesLeerlingMboHboStyles(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return !string.IsNullOrEmpty(path)
               && (path.StartsWith("/leerling/mbo-hbo", StringComparison.OrdinalIgnoreCase)
                   || path.EndsWith("/mbo-hbo", StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<string> LeerlingMboHboStylesheets(string? rawPath)
        => IncludesLeerlingMboHboStyles(rawPath)
            ? [WwwrootAssetCatalog.Url(LeerlingMboHboStylesheetPath)]
            : [];
}
