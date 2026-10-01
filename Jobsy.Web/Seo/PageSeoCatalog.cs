using System.Text.RegularExpressions;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Seo;

/// <summary>
/// Metadata for every Blazor route. Public marketing/job pages are indexable;
/// authenticated and tokenized surfaces are noindex.
/// </summary>
public static partial class PageSeoCatalog
{
    public static readonly PageSeoEntry Fallback = new(
        "Seo.SiteName",
        "Seo.PrivateDescription",
        Indexable: false);

    public static PageSeoEntry Resolve(string? path)
    {
        var p = Normalize(path);

        if (IsPublicCompanyPath(p))
        {
            return Exact["/company"];
        }

        if (Exact.TryGetValue(p, out var exact))
        {
            return exact;
        }

        foreach (var (prefix, entry) in Prefixes)
        {
            if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return Fallback;
    }

    public static bool IsIndexable(string? path) => Resolve(path).Indexable;

    public static bool IsPublicCompanyPath(string? path)
    {
        var p = Normalize(path);
        return PublicCompanyPathRegex().IsMatch(p)
               && CompanyPublicPaths.IsValidKvkRouteSegment(p.TrimStart('/').Split('/')[0]);
    }

    public static IReadOnlyList<string> StaticIndexablePaths { get; } =
    [
        "/",
        "/banenkaart",
        "/login",
        "/register",
        "/privacy",
        "/algemene-voorwaarden",
        "/gebruiksvoorwaarden",
        "/wie-zijn-wij",
        "/westland",
        "/lancering",
        "/ontdek",
        "/dna",
        "/partner"
    ];

    /// <summary>
    /// Static sitemap paths for the current feature flags.
    /// When employers are OFF, vacancy/marketing URLs are dropped and home is /ontdek.
    /// </summary>
    public static IReadOnlyList<string> StaticIndexablePathsFor(Jobsy.Core.Features.FeatureFlagSnapshot flags)
    {
        if (flags.EmployersEnabled)
        {
            return StaticIndexablePaths;
        }

        return
        [
            "/ontdek",
            "/login",
            "/privacy",
            "/algemene-voorwaarden",
            "/gebruiksvoorwaarden",
            "/wie-zijn-wij",
            "/dna",
            "/hoe-werkt-lobsy"
        ];
    }

    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var p = path.Trim();
        var q = p.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            p = p[..q];
        }

        var hash = p.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            p = p[..hash];
        }

        if (p.Length > 1)
        {
            p = p.TrimEnd('/');
        }

        return string.IsNullOrEmpty(p) ? "/" : p.ToLowerInvariant();
    }

    /// <summary>Exact @page routes (and aliases). Used by tests to prove coverage.</summary>
    public static IReadOnlyDictionary<string, PageSeoEntry> Exact { get; } =
        new Dictionary<string, PageSeoEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["/"] = Public("Landing.Seo.Title", "Landing.Seo.Description", hreflang: true),
            ["/banenkaart"] = Public("Page.JobMapTitle", "Seo.HomeDescription"),
            ["/login"] = Public("Login.Title", "Seo.LoginDescription"),
            ["/account-maken"] = Private("Signup.Seo.Title", "Signup.Seo.Description"),
            ["/account-maken/code"] = Private("Signup.Code.Title", "Signup.Seo.Description"),
            ["/wachtwoord-vergeten"] = Private("ForgotPassword.Seo.Title", "ForgotPassword.Seo.Description"),
            ["/account/wachtwoord-instellen"] = Private("SetPassword.Seo.Title", "SetPassword.Seo.Description"),
            ["/account/mail-instellingen"] = Private("MailSettings.Seo.Title", "MailSettings.Seo.Description"),
            ["/toestemming"] = Private("Consent.Seo.Title", "Consent.Seo.Description"),
            ["/mail/afmelden"] = Private("MailUnsub.Seo.Title", "MailUnsub.Seo.Description"),
            ["/melden"] = Private("Report.Seo.Title", "Report.Seo.Description"),
            ["/koppeling/sleutel"] = Private("ApiKeyReveal.Seo.Title", "ApiKeyReveal.Seo.Description"),
            ["/register"] = Public("Page.RegisterTitle", "Seo.RegisterDescription"),
            ["/register/koppelen"] = Private("Wa.Link.Title", "Wa.Link.Lead"),
            ["/register/toegang"] = Private("WaAccess.Title", "WaAccess.Lead"),
            ["/register/toegang"] = Private("WaAccess.Title", "WaAccess.Lead"),
            ["/register/bedrijf"] = Private("Wa.Steps.4.Title", "Wa.Steps.4.Sub"),
            ["/register/verifieren"] = Private("WaVerify.Eyebrow", "WaVerify.Lead"),
            ["/register/verifieren/brief"] = Private("WaVerify.Brief.Title", "WaVerify.Brief.Lead"),
            ["/admin/werkgeververificatie"] = Private("AdminWa.Title", "AdminWa.Lead"),
            ["/privacy"] = Public("Legal.Privacy", "Seo.PrivacyDescription"),
            ["/algemene-voorwaarden"] = Public("Legal.Terms", "Seo.TermsDescription"),
            ["/gebruiksvoorwaarden"] = Public("Legal.Usage", "Seo.UsageDescription"),
            ["/wie-zijn-wij"] = Public("Legal.About", "Seo.AboutDescription"),
            ["/westland"] = Public("Seo.WestlandTitle", "Seo.WestlandDescription"),
            ["/lancering"] = Public("Seo.WestlandTitle", "Seo.WestlandDescription"),
            ["/ontdek"] = Public("GratisDna.Seo.Title", "GratisDna.Seo.Description", hreflang: true),
            ["/dna"] = Public("GratisDna.Seo.Title", "GratisDna.Seo.Description", hreflang: true, CanonicalPath: "/ontdek"),
            ["/partner"] = Public("Partner.Title", "Seo.PartnerDescription"),
            ["/company"] = Public("BranchPage.Title", "Seo.CompanyFallbackDescription"),
            ["/access-denied"] = Private("Page.AccessDeniedTitle", "Seo.PrivateDescription"),
            ["/error"] = Private("Seo.ErrorTitle", "Seo.PrivateDescription"),
            ["/home"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
            ["/hoe-werkt-lobsy"] = Public("HowLobsy.Guest.Title", "HowLobsy.Guest.Lead"),
            ["/candidate/hoe-werkt-lobsy"] = Private("Nav.HowLobsyWorks", "Seo.PrivateDescription"),
            ["/candidate/liked"] = Private("Saved.Title", "Seo.PrivateDescription"),
            ["/candidate/shared"] = Private("Saved.TabShared", "Seo.PrivateDescription"),
            ["/candidate/vacancies"] = Private("Nav.Vacancies", "Seo.PrivateDescription"),
            ["/candidate/applications"] = Private("Nav.MyApplications", "Seo.PrivateDescription"),
            ["/candidate/paspoort"] = Private("Passport.Title", "Seo.PrivateDescription"),
            ["/candidate/ontdekkingsreis"] = Private("Discovery.PageTitle", "Seo.PrivateDescription"),
            ["/candidate/profile"] = Private("Profile.Title", "Seo.PrivateDescription"),
            ["/profiel"] = Private("ProfileHub.Title", "Seo.PrivateDescription"),
            ["/carriere"] = Private("CareerDash.Title", "Seo.PrivateDescription"),
            ["/candidate/competencies"] = Private("Competency.Title", "Seo.PrivateDescription"),
            ["/candidate/culture"] = Private("CultureScan.Title", "Seo.PrivateDescription"),
            ["/candidate/values"] = Private("ValuesScan.Title", "Seo.PrivateDescription"),
            ["/candidate/disc"] = Private("CultureScan.Title", "Seo.PrivateDescription"),
            ["/candidate/career"] = Private("Career.Title", "Seo.PrivateDescription"),
            ["/candidate/deep-analysis/competence"] = Private("Deep.CompetenceTitle", "Seo.PrivateDescription"),
            ["/candidate/deep-analysis/career"] = Private("Deep.CareerTitle", "Seo.PrivateDescription"),
            ["/candidate/deep-analysis/values"] = Private("Deep.ValuesTitle", "Seo.PrivateDescription"),
            ["/candidate/deep-analysis/culture"] = Private("Deep.CultureTitle", "Seo.PrivateDescription"),
            ["/profiel/tests/competence"] = Private("Test.Competence.Title", "Seo.PrivateDescription"),
            ["/profiel/tests/career"] = Private("Test.Career.Title", "Seo.PrivateDescription"),
            ["/profiel/tests/culture"] = Private("Test.Culture.Title", "Seo.PrivateDescription"),
            ["/profiel/tests/values"] = Private("Test.Values.Title", "Seo.PrivateDescription"),
            ["/werkgever/organisatie/profiel?tab=cultuur"] = Private("CultureScan.EmployerTitle", "Seo.PrivateDescription"),
            ["/candidate/deep-analysis/checkout"] = Private("Deep.Checkout", "Seo.PrivateDescription"),
            ["/candidate/talent-contacts"] = Private("Talent.CandidateTitle", "Seo.PrivateDescription"),
            ["/werkgever/talentpool"] = Private("Talent.Title", "Seo.PrivateDescription"),
            ["/werkgever/talentpool?tab=contact"] = Private("Talent.EmployerContactsTitle", "Seo.PrivateDescription"),
            ["/candidate/actions/set-unavailable"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/candidate/actions/withdraw-others"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/werkgever/vacatures"] = Private("Employer.VacanciesTitle", "Seo.PrivateDescription"),
            ["/werkgever/vacatures/nieuw"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/werkgever/sollicitaties"] = Private("Employer.Applicants", "Seo.PrivateDescription"),
            ["/werkgever/sollicitaties/{ApplicationId:guid}"] = Private("Employer.Applicants", "Seo.PrivateDescription"),
            ["/werkgever/tokens"] = Private("Employer.MyTokens", "Seo.PrivateDescription"),
            ["/werkgever/tokens/verbruik"] = Private("WgNav.UsagePerBranch", "Seo.PrivateDescription"),
            ["/werkgever/tokens/mutaties"] = Private("WgNav.Mutations", "Seo.PrivateDescription"),
            ["/werkgever/tokens/facturen"] = Private("WgNav.Invoices", "Seo.PrivateDescription"),
            ["/werkgever/organisatie/vestigingen"] = Private("Employer.Branches", "Seo.PrivateDescription"),
            ["/werkgever/organisatie/vestigingen?tab=regios"] = Private("Employer.Regions", "Seo.PrivateDescription"),
            ["/werkgever/organisatie/team"] = Private("Employer.Users", "Seo.PrivateDescription"),
            ["/werkgever/organisatie/profiel"] = Private("Admin.CompanyDetails", "Seo.PrivateDescription"),
            ["/werkgever/koppelingen"] = Private("WgNav.Integrations", "Seo.PrivateDescription"),
            ["/werkgever/koppelingen?tab=csv"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/werkgever/wervingsmateriaal"] = Private("WgNav.RecruitmentMaterials", "Seo.PrivateDescription"),
            ["/werkgever/organisatie/salaristabellen"] = Private("Employer.SalaryTables", "Seo.PrivateDescription"),
            ["/werkgever/overnames"] = Private("Employer.Takeovers", "Seo.PrivateDescription"),
            ["/employer/onboarding-checkout"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/werkgever/partner"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/werkgever/partner/uitbetalen"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/intermediary"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
            ["/intermediary/team"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/sales"] = Private("Sales.Dashboard", "Seo.PrivateDescription"),
            ["/sales/link"] = Private("Sales.Link.Title", "Seo.PrivateDescription"),
            ["/sales/aanbevelen"] = Private("Sales.Recommend.Title", "Seo.PrivateDescription"),
            ["/sales/aanbevelen/bezwaar"] = Private("Sales.Recommend.Object.Title", "Seo.PrivateDescription"),
            ["/sales/start"] = Private("Sales.Onboarding", "Seo.PrivateDescription"),
            ["/sales/profiel"] = Private("Sales.Profile.Title", "Seo.PrivateDescription"),
            ["/sales/profiel/iban-bevestigen"] = Private("Sales.Help.IbanConfirm.Title", "Seo.PrivateDescription"),
            ["/sales/hulp"] = Private("Sales.Help.Title", "Seo.PrivateDescription"),
            ["/sales/wallet"] = Private("Sales.Invoices", "Seo.PrivateDescription"),
            ["/sales/wallet/uitbetalen"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/sales/werkgevers"] = Private("Sales.Employers.Title", "Seo.PrivateDescription"),
            ["/ambassadeur"] = Private("Ambassadeur.Dashboard", "Seo.PrivateDescription"),
            ["/ambassadeur/toolkit"] = Private("Ambassadeur.Toolkit", "Seo.PrivateDescription"),
            ["/ambassadeur/onboarding"] = Private("Ambassadeur.Onboarding", "Seo.PrivateDescription"),
            ["/ambassadeur/finance"] = Private("Ambassadeur.Finance", "Seo.PrivateDescription"),
            ["/ambassadeur/payout-checkout"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/tokens/checkout-return"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/tokens/checkout-stub"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/privacy/data"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/admin"] = Private("Seo.AdminTitle", "Seo.PrivateDescription"),
            ["/admin/content/paginas"] = Private("AdminNav.PagesFlyer", "Seo.PrivateDescription"),
            ["/admin/organisaties/regios"] = Private("AdminNav.Regions", "Seo.PrivateDescription"),
            ["/admin/organisaties/aanvragen"] = Private("AdminNav.Requests", "Seo.PrivateDescription"),
            ["/admin/organisaties"] = Private("AdminNav.Companies", "Seo.PrivateDescription"),
            ["/admin/instellingen/algemeen"] = Private("AdminNav.General", "Seo.PrivateDescription"),
            ["/admin/feedback"] = Private("AdminNav.Feedback", "Seo.PrivateDescription"),
            ["/admin/financien"] = Private("AdminNav.Revenue", "Seo.PrivateDescription"),
            ["/admin/instellingen/integraties"] = Private("AdminNav.Integrations", "Seo.PrivateDescription"),
            ["/admin/beveiliging/systeemlogs"] = Private("AdminNav.SystemLogs", "Seo.PrivateDescription"),
            ["/admin/beveiliging"] = Private("AdminNav.AuditLog", "Seo.PrivateDescription"),
            ["/admin/beveiliging/2fa"] = Private("AdminNav.MfaSessions", "Seo.PrivateDescription"),
            ["/admin/beveiliging/privacy"] = Private("AdminNav.Privacy", "Seo.PrivateDescription"),
            ["/admin/beveiliging/gegevensinzage"] = Private("AdminNav.DataAccess", "Seo.PrivateDescription"),
            ["/admin/content/emails"] = Private("AdminNav.Emails", "Seo.PrivateDescription"),
            ["/admin/content/stamgegevens"] = Private("AdminNav.Masterdata", "Seo.PrivateDescription"),
            ["/admin/financien/prijzen"] = Private("AdminNav.Pricing", "Seo.PrivateDescription"),
            ["/admin/gebruikers/sales"] = Private("AdminNav.SalesAmbassadors", "Seo.PrivateDescription"),
            ["/admin/gebruikers/rollen"] = Private("AdminNav.Roles", "Seo.PrivateDescription"),
            ["/admin/instellingen"] = Private("AdminNav.Features", "Seo.PrivateDescription"),
            ["/admin/financien/goodwill"] = Private("AdminNav.Goodwill", "Seo.PrivateDescription"),
            ["/admin/financien/uitbetalingen"] = Private("AdminNav.Payouts", "Seo.PrivateDescription"),
            ["/admin/gebruikers"] = Private("AdminNav.AllUsers", "Seo.PrivateDescription"),
            ["/admin/kandidaten"] = Private("AdminNav.Candidates", "Seo.PrivateDescription"),
            ["/admin/vacatures"] = Private("AdminNav.Vacancies", "Seo.PrivateDescription"),
            ["/admin/vacatures/ats"] = Private("AdminNav.Ats", "Seo.PrivateDescription"),
            ["/admin/vacatures/categorieen"] = Private("AdminNav.CategoriesWages", "Seo.PrivateDescription"),
            ["/admin/vacatures/moderatie"] = Private("AdminNav.Moderation", "Seo.PrivateDescription"),
            ["/admin/te-doen"] = Private("AdminNav.Todo", "Seo.PrivateDescription"),
            ["/admin/content/opleidingen"] = Private("AdminNav.Training", "Seo.PrivateDescription"),
            ["/werkgever"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
            ["/werkgever/te-doen"] = Private("WgNav.Todo", "Seo.PrivateDescription"),
            ["/admin/scholen"] = Private("AdminScholen.ListTitle", "Seo.PrivateDescription"),
            ["/admin/scholen/rapportage"] = Private("AdminScholen.Report.Title", "Seo.PrivateDescription"),
            ["/school"] = Private("School.DashboardTitle", "Seo.PrivateDescription"),
            ["/leraar"] = Private("Leraar.DashboardTitle", "Seo.PrivateDescription"),
            ["/leerling"] = Private("Leerling.LoginTitle", "Seo.PrivateDescription"),
            ["/leerling/start"] = Private("Leerling.Start.Title", "Seo.PrivateDescription"),
            ["/leerling/reis"] = Private("Leerling.Reis.Title", "Seo.PrivateDescription"),
            ["/leerling/eiland"] = Private("Leerling.Island.Title", "Seo.PrivateDescription"),
            ["/leerling/stop"] = Private("Leerling.Stop.Title", "Seo.PrivateDescription"),
            ["/leerling/dit-ben-jij"] = Private("LeerlingStory.Title", "Seo.PrivateDescription"),
            ["/leerling/droombaan"] = Private("LeerlingDroom.Title", "Seo.PrivateDescription"),
            ["/leerling/pdf"] = Private("LeerlingStory.Pdf", "Seo.PrivateDescription"),
        };

    private static readonly (string Prefix, PageSeoEntry Entry)[] Prefixes =
    [
        ("/vacancies/", Public("Vacancy.Title", "Seo.VacancyFallbackDescription", "article")),
        ("/partner/", Public("Partner.Title", "Seo.PartnerDescription", index: false, CanonicalPath: "/partner")),
        ("/home/metrics/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/werkgever/organisatie/salaristabellen/", Private("Employer.SalaryTables", "Seo.PrivateDescription")),
        ("/werkgever/sollicitaties/", Private("Employer.Applicants", "Seo.PrivateDescription")),
        ("/werven/", Private("Seo.SiteName", "Seo.PrivateDescription")),
        ("/ambassadeur/ref/", Private("Seo.SiteName", "Seo.PrivateDescription")),
        ("/vestiging/", Private("BranchPage.Title", "Seo.PrivateDescription")),
        ("/admin/", Private("Seo.AdminTitle", "Seo.PrivateDescription")),
        ("/werkgever/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/candidate/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/salesmanager/", Private("Sales.Dashboard", "Seo.PrivateDescription")),
        ("/ambassadeur/", Private("Ambassadeur.Dashboard", "Seo.PrivateDescription")),
        ("/school/", Private("School.DashboardTitle", "Seo.PrivateDescription")),
        ("/leraar/", Private("Leraar.DashboardTitle", "Seo.PrivateDescription")),
        ("/leerling/", Private("Leerling.LoginTitle", "Seo.PrivateDescription")),
        ("/intermediary/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/tokens/", Private("Seo.SiteName", "Seo.PrivateDescription")),
    ];

    private static PageSeoEntry Public(
        string titleKey,
        string descriptionKey,
        string ogType = "website",
        bool hreflang = false,
        string? CanonicalPath = null,
        bool index = true)
        => new(titleKey, descriptionKey, Indexable: index, ogType, Hreflang: hreflang, CanonicalPath: CanonicalPath);

    private static PageSeoEntry Private(string titleKey, string descriptionKey)
        => new(titleKey, descriptionKey, Indexable: false);

    [GeneratedRegex(@"^/\d{8}(?:/\d{1,12})?$", RegexOptions.CultureInvariant)]
    private static partial Regex PublicCompanyPathRegex();
}
