using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Route-scoped stylesheets and optional scripts so login, banenkaart and other hot paths
/// do not download every feature bundle.
/// </summary>
public static class PageAssetCatalog
{
    [Flags]
    public enum Area
    {
        None = 0,
        CandidatePublic = 1 << 0,
        CandidateApp = 1 << 1,
        Werkgever = 1 << 2,
        Beheer = 1 << 3,
        Sales = 1 << 4,
        School = 1 << 5,
        Account = 1 << 6,
        GratisDna = 1 << 7,
    }

    /// <summary>Every interactive Blazor page.</summary>
    public static readonly string[] BlazorCoreCss =
    [
        WwwrootAssetCatalog.Url("css/tokens.css"),
        WwwrootAssetCatalog.Url("css/features/candidate-header-strip.css"),
        WwwrootAssetCatalog.Url("css/features/lobsy-assistant-panel.css"),
        WwwrootAssetCatalog.Url("css/app.min.css"),
        WwwrootAssetCatalog.Url("css/features/stijl-a.css"),
        WwwrootAssetCatalog.Url("css/features/form-fields.css"),
        WwwrootAssetCatalog.Url("css/features/candidate-illustrated-decorative-layers.css"),
        WwwrootAssetCatalog.Url("css/features/nav-feedback.css"),
        WwwrootAssetCatalog.Url("css/features/candidate-header-tablet.css"),
    ];

    public static readonly string[] CandidatePublicCss =
    [
        WwwrootAssetCatalog.Url("css/features/banenkaart.css"),
        WwwrootAssetCatalog.Url("css/features/banenkaart-illustrated.css"),
        WwwrootAssetCatalog.Url("css/features/bedrijvenkaart.css"),
        WwwrootAssetCatalog.Url("css/features/candidate-illustrated-app.css"),
        WwwrootAssetCatalog.Url("css/features/kandidaat-banen.css"),
        WwwrootAssetCatalog.Url("css/features/match-desktop.css"),
                WwwrootAssetCatalog.Url("css/features/applications.css"),
        WwwrootAssetCatalog.Url("css/features/kb-external-vacancy.css"),
    ];

    private static readonly string[] UwvMockupRouteCss =
    [
        WwwrootAssetCatalog.Url("css/features/uwv-mockups.css"),
        WwwrootAssetCatalog.Url("css/features/banenkaart.css"),
        WwwrootAssetCatalog.Url("css/features/banenkaart-illustrated.css"),
    ];

    public static readonly string[] CandidateAppCss =
    [
        WwwrootAssetCatalog.Url("css/features/notifications.css"),
        WwwrootAssetCatalog.Url("css/features/choice-tiles.css"),
        WwwrootAssetCatalog.Url("css/features/onboarding-wizard.css"),
        WwwrootAssetCatalog.Url("css/features/onboarding-illustrated-step.css"),
        WwwrootAssetCatalog.Url("css/features/onboarding-test-blocks.css"),
        WwwrootAssetCatalog.Url("css/features/onboarding-wizard-mobile-bar.css"),
        WwwrootAssetCatalog.Url("css/features/onboarding-wizard-richting-a.css"),
        WwwrootAssetCatalog.Url("css/features/onboarding-work-history.css"),
        WwwrootAssetCatalog.Url("css/features/questionnaire.css"),
        WwwrootAssetCatalog.Url("css/features/mijn-dna.css"),
        WwwrootAssetCatalog.Url("css/features/gratis-dna.css"),
        WwwrootAssetCatalog.Url("css/features/kandidaatinzichten.css"),
        WwwrootAssetCatalog.Url("css/features/testresultaten.css"),
        WwwrootAssetCatalog.Url("css/features/mijn-paspoort.css"),
        WwwrootAssetCatalog.Url("css/features/kompas-illustrated.css"),
        WwwrootAssetCatalog.Url("css/features/reference-verification-hero.css"),
        WwwrootAssetCatalog.Url("css/features/reference-verification.css"),
        WwwrootAssetCatalog.Url("css/features/ontdekkingsreis.css"),
        WwwrootAssetCatalog.Url("css/features/tests.css"),
        WwwrootAssetCatalog.Url("css/features/carriere.css"),
        WwwrootAssetCatalog.Url("css/features/opleidingen.css"),
        WwwrootAssetCatalog.Url("css/features/occupation-day.css"),
        WwwrootAssetCatalog.Url("css/features/honest-advice.css"),
        WwwrootAssetCatalog.Url("css/features/future-jobs.css"),
        WwwrootAssetCatalog.Url("css/features/proactive-matching.css"),
        WwwrootAssetCatalog.Url("css/features/candidate-hub.css"),
        WwwrootAssetCatalog.Url("css/features/na-de-match.css"),
    ];

    public static readonly string[] WerkgeverCss =
    [
        WwwrootAssetCatalog.Url("css/features/notifications.css"),
        WwwrootAssetCatalog.Url("css/features/choice-tiles.css"),
        WwwrootAssetCatalog.Url("css/features/werkgever.css"),
        WwwrootAssetCatalog.Url("css/features/vacancy-wizard.css"),
        WwwrootAssetCatalog.Url("css/features/werkgever-aanmelding.css"),
        WwwrootAssetCatalog.Url("css/features/enterprise-ui.css"),
        WwwrootAssetCatalog.Url("css/features/na-de-match.css"),
    ];

    public static readonly string[] KandidatenkaartCss =
    [
        WwwrootAssetCatalog.Url("css/features/kandidaatinzichten.css"),
        WwwrootAssetCatalog.Url("css/features/kandidatenkaart.css"),
    ];

    public static readonly string[] BeheerCss =
    [
        WwwrootAssetCatalog.Url("css/features/admin.css"),
        WwwrootAssetCatalog.Url("css/features/enterprise-ui.css"),
    ];

    public static readonly string[] SalesCss =
    [
        WwwrootAssetCatalog.Url("css/features/sales.css"),
    ];

    public static readonly string[] SchoolCss =
    [
        WwwrootAssetCatalog.Url("css/features/scholen.css"),
    ];

    public static readonly string[] AccountCss =
    [
        WwwrootAssetCatalog.Url("css/features/account-mfa.css"),
    ];

    public static readonly string[] GratisDnaCss =
    [
        WwwrootAssetCatalog.Url("css/features/gratis-dna.css"),
        WwwrootAssetCatalog.Url("css/features/questionnaire.css"),
    ];

    public static Area ResolveAreas(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        if (string.IsNullOrEmpty(path))
        {
            return Area.None;
        }

        if (path is "/" or "/partner" or "/westland" or "/lancering" or "/werkgevers")
        {
            return Area.None;
        }

        if (path is "/register"
            || path.StartsWith("/register/", StringComparison.OrdinalIgnoreCase))
        {
            return Area.Werkgever;
        }

        if (path is "/login")
        {
            return Area.None;
        }

        if (path is "/account-maken" or "/account-maken/code")
        {
            return Area.None;
        }

        if (path is "/gratis-dna")
        {
            return Area.GratisDna;
        }

        if (path is "/banenkaart" or "/banen" or "/bedrijvenkaart")
        {
            return Area.CandidatePublic;
        }

        if (path.Length == 8 && path.All(char.IsDigit))
        {
            return Area.CandidatePublic;
        }

        if (path is "/notifications")
        {
            return Area.CandidatePublic | Area.CandidateApp;
        }

        if (path.StartsWith("/account/", StringComparison.Ordinal))
        {
            return Area.Account;
        }

        if (path.StartsWith("/dev/na-de-match", StringComparison.OrdinalIgnoreCase))
        {
            return Area.CandidatePublic | Area.CandidateApp | Area.Werkgever;
        }

        if (path.StartsWith("/preview/uwv-mockups", StringComparison.OrdinalIgnoreCase))
        {
            return Area.CandidatePublic | Area.CandidateApp;
        }

        if (path.StartsWith("/preview/na-de-match", StringComparison.OrdinalIgnoreCase))
        {
            return Area.CandidatePublic | Area.CandidateApp;
        }

        if (path.StartsWith("/candidate/", StringComparison.Ordinal)
            || path.StartsWith("/vacancies/", StringComparison.Ordinal)
            || path is "/home" or "/ontdek" or "/candidate/match")
        {
            return Area.CandidatePublic | Area.CandidateApp;
        }

        if (path is "/werkgever"
            || path.StartsWith("/werkgever/", StringComparison.Ordinal)
            || path.StartsWith("/register/werkgever", StringComparison.OrdinalIgnoreCase))
        {
            return Area.Werkgever;
        }

        if (path.StartsWith("/admin/", StringComparison.Ordinal))
        {
            return Area.Beheer;
        }

        if (path.StartsWith("/sales/", StringComparison.Ordinal))
        {
            return Area.Sales;
        }

        if (path.StartsWith("/scholen", StringComparison.Ordinal)
            || path.StartsWith("/leerling", StringComparison.Ordinal)
            || path.StartsWith("/leraar", StringComparison.Ordinal)
            || path.StartsWith("/school", StringComparison.Ordinal))
        {
            return Area.School;
        }

        // Authenticated shell pages (partner, status, …) — lean candidate bundles.
        return Area.CandidatePublic | Area.CandidateApp;
    }

    public static IReadOnlyList<string> StylesheetsFor(Area areas, string? rawPath = null)
    {
        var list = new List<string>(BlazorCoreCss.Length + 24);
        list.AddRange(BlazorCoreCss);
        AppendUnique(list, areas, Area.CandidatePublic, CandidatePublicCss);
        AppendUnique(list, areas, Area.CandidateApp, CandidateAppCss);
        AppendUnique(list, areas, Area.Werkgever, WerkgeverCss);
        AppendUnique(list, areas, Area.Beheer, BeheerCss);
        AppendUnique(list, areas, Area.Sales, SalesCss);
        AppendUnique(list, areas, Area.School, SchoolCss);
        AppendUnique(list, areas, Area.Account, AccountCss);
        AppendUnique(list, areas, Area.GratisDna, GratisDnaCss);
        foreach (var url in RouteStylesheets(rawPath))
        {
            if (!list.Contains(url, StringComparer.Ordinal))
            {
                list.Add(url);
            }
        }

        return list;
    }

    internal static bool RouteUsesCompactHeroCss(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        if (path == OnboardingRoutes.ClassicStartPath
            || path.Equals("/carriere", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/carriere/", StringComparison.OrdinalIgnoreCase)
            || path == PassportRedirects.PassportPath
            || path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/liked", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/settings", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/bedrijvenkaart", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/ontdekkingsreis", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/profiel/tests", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/__test/capture-test-block", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/competencies", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/career-interests", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/culture", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/values", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static IReadOnlyList<string> RouteStylesheets(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        if (path.Equals("/werkgever/kandidatenkaart", StringComparison.OrdinalIgnoreCase))
        {
            return KandidatenkaartCss;
        }

        if (path.StartsWith("/werkgever/", StringComparison.OrdinalIgnoreCase))
        {
            return [WwwrootAssetCatalog.Url("css/features/bedrijvenkaart.css")];
        }

        if (path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                WwwrootAssetCatalog.Url("css/features/candidate-header-strip.css"),
                WwwrootAssetCatalog.Url("css/features/kandidaat-banen.css"),
                WwwrootAssetCatalog.Url("css/features/banenkaart-illustrated.css"),
                WwwrootAssetCatalog.Url("css/features/sollicitaties-page.css"),
            ];
        }

        if (path.Length is >= 8 and <= 13 && path.Trim('/').All(c => char.IsDigit(c) || c == '/'))
        {
            return [WwwrootAssetCatalog.Url("css/features/bedrijvenkaart.css")];
        }

        if (path.StartsWith("/werk-referentie/", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                WwwrootAssetCatalog.Url("css/features/reference-verification-hero.css"),
                WwwrootAssetCatalog.Url("css/features/reference-verification.css")
            ];
        }

        if (path.StartsWith("/preview/uwv-mockups", StringComparison.OrdinalIgnoreCase))
        {
            return UwvMockupRouteCss;
        }

        if (path.Equals("/banenkaart", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/banen", StringComparison.OrdinalIgnoreCase))
        {
            return RouteUsesCompactHeroCss(path)
                ? [WwwrootAssetCatalog.Url("css/features/candidate-hero-compact-preview.css")]
                : [];
        }

        if (path.Equals("/carriere", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/carriere/leren", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/ondernemerscheck", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/wat-drijft-jou", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/adviseur-werk", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/deel/uwv-adviseur/", StringComparison.OrdinalIgnoreCase))
        {
            var uwvCss = new List<string>(UwvMockupRouteCss);
            if (RouteUsesCompactHeroCss(path))
            {
                uwvCss.Add(WwwrootAssetCatalog.Url("css/features/candidate-hero-compact-preview.css"));
            }

            return uwvCss;
        }

        if (path == OnboardingRoutes.ClassicStartPath)
        {
            return
            [
                WwwrootAssetCatalog.Url("css/features/candidate-hero-compact-preview.css"),
                WwwrootAssetCatalog.Url("css/features/onboarding-rustig.css"),
                WwwrootAssetCatalog.Url("css/features/onboarding-client-nav.css"),
            ];
        }

        if (path.Equals("/candidate/start/preview-strand", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                WwwrootAssetCatalog.Url("css/features/onboarding-rustig.css"),
            ];
        }

        if (RouteUsesCompactHeroCss(path))
        {
            return [WwwrootAssetCatalog.Url("css/features/candidate-hero-compact-preview.css")];
        }

        return [];
    }

    public static bool IncludeMapPreconnect(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path is "/banenkaart" or "/banen" or "/bedrijvenkaart" or "/werkgever/kandidatenkaart"
               || (path.Length == 8 && path.All(char.IsDigit));
    }

    public static bool IncludeLandingScript(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path is "/" or "/partner" or "/westland" or "/lancering" or "/werkgevers";
    }

    public static bool IncludeLeerlingLoginScript(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path.StartsWith("/leerling", StringComparison.Ordinal);
    }

    public static bool IncludeGratisDnaScript(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path is "/gratis-dna"
            || path.StartsWith(OnboardingRoutes.ClassicStartPath, StringComparison.Ordinal)
            || path.StartsWith("/account-maken", StringComparison.Ordinal);
    }

    /// <summary>Static signup shells skip app.min.css — ship login-form + provider-btn rules locally.</summary>
    public static bool IncludeSignupStaticCss(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path is "/account-maken" or "/account-maken/code"
            || path.StartsWith("/account-maken/", StringComparison.Ordinal);
    }

    public static bool IncludeOnboardingClientScript(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path.StartsWith(OnboardingRoutes.ClassicStartPath, StringComparison.Ordinal);
    }

    public static bool IncludeBanenkaartClientScript(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path is "/banenkaart" or "/banen";
    }

    /// <summary>Idle prefetch of banenkaart catalog + map assets (candidate paspoort home).</summary>
    public static bool IncludeBanenkaartPrefetch(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path is "/candidate/paspoort";
    }

    /// <summary>Lean styles for <c>[NoBlazorRuntime]</c> banenkaart SSR (skip render-blocking app.min.css).</summary>
    public static IReadOnlyList<string> BanenkaartSsrStylesheets() =>
    [
        WwwrootAssetCatalog.UrlLeadingSlash("css/features/candidate-header-strip.css"),
        WwwrootAssetCatalog.UrlLeadingSlash("css/features/banenkaart-page.css"),
        WwwrootAssetCatalog.UrlLeadingSlash("css/features/kandidaat-banen.css"),
        WwwrootAssetCatalog.UrlLeadingSlash("css/features/banenkaart.css"),
        WwwrootAssetCatalog.UrlLeadingSlash("css/features/banenkaart-illustrated.css"),
        WwwrootAssetCatalog.UrlLeadingSlash("css/features/banenkaart-lagen.css"),
    ];

    /// <summary>Candidate header + shell chrome when banenkaart SSR runs without full Blazor app.min.css.</summary>
    public static IReadOnlyList<string> BanenkaartAuthenticatedShellCss() =>
    [
        WwwrootAssetCatalog.Url("css/features/banenkaart-auth-chrome.css"),
        WwwrootAssetCatalog.Url("css/features/lobsy-assistant-panel.css"),
        WwwrootAssetCatalog.Url("css/features/form-fields.css"),
        WwwrootAssetCatalog.Url("css/features/nav-feedback.css"),
        WwwrootAssetCatalog.Url("css/features/candidate-header-tablet.css"),
    ];

    /// <summary>Primary nav + bottom bar styles for logged-in candidate NoBlazor SSR pages.</summary>
    public static IReadOnlyList<string> CandidateNoBlazorChromeCss() =>
    [
        WwwrootAssetCatalog.Url("css/features/candidate-no-blazor-chrome.css"),
        WwwrootAssetCatalog.Url("css/features/lobsy-coach-pill.css"),
    ];

    public static bool IncludeVacancyWizardScript(string? rawPath)
    {
        var path = PageSeoCatalog.Normalize(rawPath);
        return path.StartsWith("/werkgever/vacatures", StringComparison.Ordinal);
    }

    private static void AppendUnique(List<string> list, Area areas, Area flag, string[] urls)
    {
        if ((areas & flag) == 0)
        {
            return;
        }

        foreach (var url in urls)
        {
            if (!list.Contains(url, StringComparer.Ordinal))
            {
                list.Add(url);
            }
        }
    }
}
