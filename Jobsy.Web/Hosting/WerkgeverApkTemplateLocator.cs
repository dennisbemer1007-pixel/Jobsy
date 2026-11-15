namespace Jobsy.Web.Hosting;

/// <summary>Resolves mockup HTML under Jobsy.Web/wwwroot/werkgever-apk/templates (also when test host cwd is bin/).</summary>
internal static class WerkgeverApkTemplateLocator
{
    private static string? _cachedRoot;
    private static string? _overrideWebRoot;

    internal static void SetWebContentRoot(string? absoluteWebProjectRoot)
        => _overrideWebRoot = absoluteWebProjectRoot;

    public static string TemplatesRoot
    {
        get
        {
            if (_cachedRoot is not null && Directory.Exists(_cachedRoot))
            {
                return _cachedRoot;
            }

            foreach (var candidate in CandidateRoots())
            {
                if (Directory.Exists(candidate))
                {
                    _cachedRoot = candidate;
                    return candidate;
                }
            }

            _cachedRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "werkgever-apk", "templates");
            return _cachedRoot;
        }
    }

    public static string PathFor(string templateFileName)
        => Path.Combine(TemplatesRoot, templateFileName);

    private static IEnumerable<string> CandidateRoots()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_overrideWebRoot))
        {
            var forced = Path.Combine(_overrideWebRoot, "wwwroot", "werkgever-apk", "templates");
            if (seen.Add(forced))
            {
                yield return forced;
            }
        }

        foreach (var dir in WalkParents(AppContext.BaseDirectory))
        {
            foreach (var rel in new[]
                     {
                         Path.Combine("wwwroot", "werkgever-apk", "templates"),
                         Path.Combine("Jobsy.Web", "wwwroot", "werkgever-apk", "templates"),
                     })
            {
                var full = Path.GetFullPath(Path.Combine(dir, rel));
                if (seen.Add(full))
                {
                    yield return full;
                }
            }
        }

        var cwd = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "werkgever-apk", "templates"));
        if (seen.Add(cwd))
        {
            yield return cwd;
        }
    }

    private static IEnumerable<string> WalkParents(string start)
    {
        var dir = start;
        for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            yield return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
    }
}
