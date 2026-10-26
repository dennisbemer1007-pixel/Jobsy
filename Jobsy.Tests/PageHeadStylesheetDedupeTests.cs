using Jobsy.Web.Hosting;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

/// <summary>
/// Mirrors <c>App.razor</c> head stylesheet emission so regressions (duplicate <c>href</c>) are caught without a full host render.
/// </summary>
public sealed class PageHeadStylesheetDedupeTests
{
    public static TheoryData<string, bool, bool, bool, bool, bool> CandidateHeadCases =>
        new()
        {
            { "/candidate/paspoort", false, false, true, false, false },
            { "/candidate/kompas", false, false, true, false, false },
            { "/banenkaart", false, true, true, true, false },
            { OnboardingRoutes.ClassicStartPath, false, false, true, false, true },
            { "/candidate/start/preview-strand", false, false, false, false, false },
        };

    [Theory]
    [MemberData(nameof(CandidateHeadCases))]
    public void Logged_in_candidate_shell_emits_unique_stylesheet_hrefs(
        string path,
        bool loadBlazor,
        bool includeBanenkaartClientScript,
        bool includeCandidateNoBlazorChromeCss,
        bool banenkaartAuthenticatedShell,
        bool includeGratisDnaScript)
    {
        var hrefs = CollectAppHeadStylesheets(
            path,
            loadBlazor,
            includeBanenkaartClientScript,
            includeCandidateNoBlazorChromeCss,
            banenkaartAuthenticatedShell,
            includeGratisDnaScript);

        var normalized = hrefs.Select(NormalizeHref).ToList();
        var dupes = normalized
            .GroupBy(h => h, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(
            dupes.Count == 0,
            $"{path}: duplicate stylesheet href(s): {string.Join(", ", dupes)}");
    }

    [Fact]
    public void Preview_strand_route_includes_onboarding_rustig_css()
    {
        var path = "/candidate/start/preview-strand";
        var areas = PageAssetCatalog.ResolveAreas(path);
        var list = PageAssetCatalog.StylesheetsFor(areas, path);
        Assert.Contains(
            list,
            u => u.Contains("onboarding-rustig.css", StringComparison.Ordinal));
    }

    [Fact]
    public void PageAssetCatalog_StylesheetsFor_has_no_internal_duplicates()
    {
        foreach (var path in new[]
                 {
                     "/candidate/paspoort",
                     "/candidate/kompas",
                     "/banenkaart",
                     OnboardingRoutes.ClassicStartPath,
                     "/candidate/start/preview-strand",
                 })
        {
            var areas = PageAssetCatalog.ResolveAreas(path);
            var list = PageAssetCatalog.StylesheetsFor(areas, path);
            var dupes = list.GroupBy(u => NormalizeHref(u), StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            Assert.True(dupes.Count == 0, $"{path}: {string.Join(", ", dupes)}");
        }
    }

    private static string NormalizeHref(string href)
    {
        var q = href.IndexOf('?', StringComparison.Ordinal);
        var path = q >= 0 ? href[..q] : href;
        return path.TrimStart('/');
    }

    private static List<string> CollectAppHeadStylesheets(
        string path,
        bool loadBlazor,
        bool includeBanenkaartClientScript,
        bool includeCandidateNoBlazorChromeCss,
        bool banenkaartAuthenticatedShell,
        bool includeGratisDnaScript)
    {
        var areas = PageAssetCatalog.ResolveAreas(path);
        var pageStylesheets = PageAssetCatalog.StylesheetsFor(areas, path);
        var usesFullRouteStylesheetBundle =
            loadBlazor
            || (includeCandidateNoBlazorChromeCss && !includeBanenkaartClientScript);

        var hrefs = new List<string>(48);

        if (!loadBlazor && includeGratisDnaScript && !usesFullRouteStylesheetBundle)
        {
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/gratis-dna.css"));
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/questionnaire.css"));
        }

        hrefs.Add(WwwrootAssetCatalog.Url("css/critical.css"));
        if (!usesFullRouteStylesheetBundle)
        {
            hrefs.Add(WwwrootAssetCatalog.Url("css/tokens.css"));
        }

        hrefs.Add(WwwrootAssetCatalog.Url("css/features/public-theme.css"));
        if (!usesFullRouteStylesheetBundle)
        {
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/stijl-a.css"));
        }

        hrefs.Add(WwwrootAssetCatalog.Url("css/features/auth.css"));
        hrefs.Add(WwwrootAssetCatalog.Url("css/features/landing.css"));
        if (!usesFullRouteStylesheetBundle)
        {
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/candidate-illustrated-decorative-layers.css"));
        }

        hrefs.Add(WwwrootAssetCatalog.Url("css/features/candidate-illustrated-public.css"));
        hrefs.Add(WwwrootAssetCatalog.Url("css/features/public-pages.css"));
        if (!usesFullRouteStylesheetBundle)
        {
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/errors.css"));
        }

        if (!loadBlazor && includeBanenkaartClientScript)
        {
            hrefs.AddRange(PageAssetCatalog.BanenkaartSsrStylesheets());
            if (banenkaartAuthenticatedShell)
            {
                hrefs.AddRange(PageAssetCatalog.BanenkaartAuthenticatedShellCss());
            }

            if (includeCandidateNoBlazorChromeCss)
            {
                hrefs.AddRange(PageAssetCatalog.CandidateNoBlazorChromeCss());
            }
        }

        if (!loadBlazor && includeCandidateNoBlazorChromeCss && !includeBanenkaartClientScript)
        {
            hrefs.AddRange(pageStylesheets);
            hrefs.AddRange(PageAssetCatalog.CandidateNoBlazorChromeCss());
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/errors.css"));
        }

        if (loadBlazor)
        {
            hrefs.AddRange(pageStylesheets);
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/lobsy-coach-pill.css"));
            hrefs.Add(WwwrootAssetCatalog.Url("css/features/errors.css"));
        }

        return hrefs;
    }
}
