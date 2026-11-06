namespace Jobsy.Web.Applications;

internal static class AfterMatchWerkgeverSsrPaths
{
    internal static bool IsCanonicalOrInternalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var p = path.Split('?', '#')[0];
        return p.Contains("/werkgever/sollicitaties/", StringComparison.OrdinalIgnoreCase)
               && p.Contains("/after-match", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Public URL stays <c>/after-match</c>; middleware may rewrite to *-ssr internally.</summary>
    internal static bool ShouldDisableBlazorAndEnhancedNav(string? path)
        => IsCanonicalOrInternalPath(path);
}
