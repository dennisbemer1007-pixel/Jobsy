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
        "css/tokens.css?v=20261018-06",
        "css/app.min.css?v=20261019-13",
        "css/features/stijl-a.css?v=20261018-03",
        "css/features/form-fields.css?v=20261019-01",
        "css/features/candidate-illustrated-decorative-layers.css?v=20261020-01",
        "css/features/nav-feedback.css?v=20261017-01",
    ];

    public static readonly string[] CandidatePublicCss =
    [
        "css/features/banenkaart.css?v=20261019-01",
        "css/features/banenkaart-illustrated.css?v=20261018-14",
        "css/features/bedrijvenkaart.css?v=20261019-10",
        "css/features/candidate-illustrated-app.css?v=20261018-91",
        "css/features/kandidaat-banen.css?v=20261019-01",
        "css/features/match-desktop.css?v=20261018-02",
        "css/features/applications.css?v=20261004-10",
        "css/features/kb-external-vacancy.css?v=20261011-03",
    ];

    private static readonly string[] UwvMockupRouteCss =
    [
        "css/features/uwv-mockups.css?v=20261019-r8",
        "css/features/banenkaart.css?v=20261018-02",
        "css/features/banenkaart-illustrated.css?v=20261018-14",
    ];

    public static readonly string[] CandidateAppCss =
    [
        "css/features/notifications.css?v=20261017-14",
        "css/features/choice-tiles.css?v=20261015-06",
        "css/features/onboarding-wizard.css?v=20261018-02",
        "css/features/onboarding-illustrated-step.css?v=20261019-r24",
        "css/features/onboarding-test-blocks.css?v=20261018-r2",
        "css/features/onboarding-wizard-mobile-bar.css?v=20261016-02",
        "css/features/onboarding-wizard-richting-a.css?v=20261019-r25",
        "css/features/onboarding-work-history.css?v=20261018-08",
        "css/features/questionnaire.css?v=20261004-10",
        "css/features/mijn-dna.css?v=20261004-10",
        "css/features/gratis-dna.css?v=20261004-10",
        "css/features/kandidaatinzichten.css?v=20261004-10",
        "css/features/testresultaten.css?v=20261019-r5billus",
        "css/features/mijn-paspoort.css?v=20261018-06",
        "css/features/kompas-illustrated.css?v=20261021-01",
        "css/features/reference-verification-hero.css?v=20261018-16",
        "css/features/reference-verification.css?v=20261018-16",
        "css/features/ontdekkingsreis.css?v=20261021-08",
        "css/features/tests.css?v=20261020-r8",
        "css/features/candidate-illustrated-app.css?v=20261018-91",
        "css/features/carriere.css?v=20261018-03",
        "css/features/opleidingen.css?v=20261018-r12",
        "css/features/occupation-day.css?v=20261005-23",
        "css/features/honest-advice.css?v=20261005-07",
        "css/features/future-jobs.css?v=20261006-02",
        "css/features/proactive-matching.css?v=20261012-03",
        "css/features/candidate-hub.css?v=20261012-01",
        "css/features/na-de-match.css?v=20261020-01",
    ];

    public static readonly string[] WerkgeverCss =
    [
        "css/features/notifications.css?v=20261017-14",
        "css/features/choice-tiles.css?v=20261015-06",
        "css/features/werkgever.css?v=20261017-19",
        "css/features/vacancy-wizard.css?v=20261017-02",
        "css/features/werkgever-aanmelding.css?v=20261016-06",
        "css/features/enterprise-ui.css?v=20261004-10",
        "css/features/na-de-match.css?v=20261020-01",
    ];

    public static readonly string[] KandidatenkaartCss =
    [
        "css/features/kandidaatinzichten.css?v=20261004-10",
        "css/features/kandidatenkaart.css?v=20261018-22",
    ];

    public static readonly string[] BeheerCss =
    [
        "css/features/admin.css?v=20261018-11",
        "css/features/enterprise-ui.css?v=20261004-10",
    ];

    public static readonly string[] SalesCss =
    [
        "css/features/sales.css?v=20261004-10",
    ];

    public static readonly string[] SchoolCss =
    [
        "css/features/scholen.css?v=20261004-14",
    ];

    public static readonly string[] AccountCss =
    [
        "css/features/account-mfa.css?v=20261004-10",
    ];

    public static readonly string[] GratisDnaCss =
    [
        "css/features/gratis-dna.css?v=20261004-10",
        "css/features/questionnaire.css?v=20261004-10",
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

        if (path.StartsWith("/candidate/", StringComparison.Ordinal)
            || path.StartsWith("/vacancies/", StringComparison.Ordinal)
            || path is "/home" or "/ontdek" or "/candidate/match")
        {
            return Area.CandidatePublic | Area.CandidateApp;
        }

        if (path.StartsWith("/werkgever/", StringComparison.Ordinal)
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
            return ["css/features/bedrijvenkaart.css?v=20261019-10"];
        }

        if (path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase))
        {
            var appsCss = new List<string> { "css/features/bedrijvenkaart.css?v=20261019-10" };
            if (RouteUsesCompactHeroCss(path))
            {
                appsCss.Add("css/features/candidate-hero-compact-preview.css?v=20261021-r16");
            }

            return appsCss;
        }

        if (path.Length is >= 8 and <= 13 && path.Trim('/').All(c => char.IsDigit(c) || c == '/'))
        {
            return ["css/features/bedrijvenkaart.css?v=20261019-10"];
        }

        if (path.StartsWith("/werk-referentie/", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "css/features/reference-verification-hero.css?v=20261018-16",
                "css/features/reference-verification.css?v=20261018-16"
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
                ? ["css/features/candidate-hero-compact-preview.css?v=20261021-r16"]
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
                uwvCss.Add("css/features/candidate-hero-compact-preview.css?v=20261021-r16");
            }

            return uwvCss;
        }

        if (RouteUsesCompactHeroCss(path))
        {
            return ["css/features/candidate-hero-compact-preview.css?v=20261021-r16"];
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
