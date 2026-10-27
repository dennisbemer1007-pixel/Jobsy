using Jobsy.Web.Seo;

namespace Jobsy.Web.Bedrijvenkaart;

internal static class BedrijvenkaartSsrPaths
{
    internal static bool IsHostPath(string? rawPath)
        => string.Equals(
            PageSeoCatalog.Normalize(rawPath),
            BedrijvenkaartSsrPreview.PreviewPath,
            StringComparison.OrdinalIgnoreCase);
}
