using Jobsy.Core.Localization;
using Jobsy.Web.Features;

namespace Jobsy.Web.Seo;

/// <summary>hreflang alternate links for public pages (response-only <c>?lang=</c>, no cookie).</summary>
public static class HreflangLinks
{
    public static IReadOnlyList<(string Hreflang, string Href)> ForPath(string origin, string path)
    {
        var normalizedPath = PageSeoCatalog.Normalize(path);
        var pathPart = normalizedPath == "/" ? "/" : normalizedPath;
        var baseUrl = origin.TrimEnd('/') + (pathPart == "/" ? "/" : pathPart);

        var list = new List<(string, string)>(JobsyLanguages.All.Count + 1);
        foreach (var lang in JobsyLanguages.All)
        {
            var href = pathPart == "/"
                ? origin.TrimEnd('/') + "/?lang=" + lang.Code
                : origin.TrimEnd('/') + pathPart + "?lang=" + lang.Code;
            list.Add((lang.Code, href));
        }

        list.Add(("x-default", baseUrl));
        return list;
    }

    public static bool ShouldEmit(string? path)
        => PageSeoCatalog.Resolve(path).Hreflang;
}
