using System.Text.RegularExpressions;

namespace Jobsy.Web.Navigation;

/// <summary>Active-state rules for school shell navigation (sidebar + bottom nav).</summary>
public static partial class ScholenNavRouting
{
    [GeneratedRegex(
        "^/leraar/klas/[0-9a-fA-F-]{36}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex TeacherClassOverviewPath();

    public static bool IsNavActive(string currentPath, string href)
    {
        currentPath = NormalizePath(currentPath);
        href = NormalizePath(href);
        if (string.Equals(currentPath, href, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // "/leraar" and "/school" must not stay active on deeper routes.
        if (href is "/leraar" or "/school")
        {
            return false;
        }

        // Class overview tab is only active on the klas root, not codes/groep/etc.
        if (TeacherClassOverviewPath().IsMatch(href))
        {
            return false;
        }

        return currentPath.StartsWith(href + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var p = path.Split('?', 2)[0].Trim();
        if (p.Length > 1 && p.EndsWith('/'))
        {
            p = p.TrimEnd('/');
        }

        return string.IsNullOrEmpty(p) ? "/" : p;
    }
}
