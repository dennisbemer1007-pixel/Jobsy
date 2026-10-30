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

    /// <summary>School mobile bottom nav: Dashboard only on <c>/school</c>; Meer on secondary school pages.</summary>
    public static bool IsSchoolBottomNavActive(string currentPath, string labelKey, string href)
    {
        currentPath = NormalizePath(currentPath);
        if (string.Equals(labelKey, "School.Nav.Dashboard", StringComparison.Ordinal))
        {
            return string.Equals(currentPath, "/school", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(labelKey, "School.Nav.More", StringComparison.Ordinal))
        {
            if (string.Equals(currentPath, "/school", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return currentPath.StartsWith("/school/te-doen", StringComparison.OrdinalIgnoreCase)
                   || currentPath.StartsWith("/school/leraren", StringComparison.OrdinalIgnoreCase)
                   || currentPath.StartsWith("/school/gegevens", StringComparison.OrdinalIgnoreCase)
                   || currentPath.StartsWith("/school/privacy", StringComparison.OrdinalIgnoreCase)
                   || currentPath.StartsWith("/school/materiaal", StringComparison.OrdinalIgnoreCase);
        }

        return IsNavActive(currentPath, href);
    }

    /// <summary>Teacher mobile bottom nav: Meer is active on overflow routes (droombanen, testvenster, materiaal).</summary>
    public static bool IsTeacherBottomNavActive(string currentPath, string labelKey, string href, Guid? classId)
    {
        currentPath = NormalizePath(currentPath);
        if (string.Equals(labelKey, "Leraar.Nav.More", StringComparison.Ordinal))
        {
            if (classId is null)
            {
                return false;
            }

            var basePath = $"/leraar/klas/{classId:D}";
            return currentPath.StartsWith($"{basePath}/droombanen", StringComparison.OrdinalIgnoreCase)
                   || currentPath.StartsWith($"{basePath}/testvenster", StringComparison.OrdinalIgnoreCase)
                   || currentPath.StartsWith($"{basePath}/materiaal", StringComparison.OrdinalIgnoreCase);
        }

        return IsNavActive(currentPath, href);
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
