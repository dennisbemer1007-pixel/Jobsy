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
        "/login",
        "/register",
        "/privacy",
        "/algemene-voorwaarden",
        "/gebruiksvoorwaarden",
        "/wie-zijn-wij",
        "/westland",
        "/lancering",
        "/partner"
    ];

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
            ["/"] = Public("Page.JobMapTitle", "Seo.HomeDescription"),
            ["/banen"] = Private("Page.JobMapTitle", "Seo.HomeDescription"),
            ["/login"] = Public("Login.Title", "Seo.LoginDescription"),
            ["/register"] = Public("Page.RegisterTitle", "Seo.RegisterDescription"),
            ["/register/activate"] = Private("Page.ActivateTitle", "Seo.ActivateDescription"),
            ["/privacy"] = Public("Legal.Privacy", "Seo.PrivacyDescription"),
            ["/algemene-voorwaarden"] = Public("Legal.Terms", "Seo.TermsDescription"),
            ["/gebruiksvoorwaarden"] = Public("Legal.Usage", "Seo.UsageDescription"),
            ["/wie-zijn-wij"] = Public("Legal.About", "Seo.AboutDescription"),
            ["/westland"] = Public("Seo.WestlandTitle", "Seo.WestlandDescription"),
            ["/lancering"] = Public("Seo.WestlandTitle", "Seo.WestlandDescription"),
            ["/partner"] = Public("Partner.Title", "Seo.PartnerDescription"),
            ["/company"] = Public("BranchPage.Title", "Seo.CompanyFallbackDescription"),
            ["/access-denied"] = Private("Page.AccessDeniedTitle", "Seo.PrivateDescription"),
            ["/error"] = Private("Seo.ErrorTitle", "Seo.PrivateDescription"),
            ["/home"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
            ["/hoe-werkt-lobsy"] = Private("Nav.HowLobsyWorks", "Seo.PrivateDescription"),
            ["/candidate/hoe-werkt-lobsy"] = Private("Nav.HowLobsyWorks", "Seo.PrivateDescription"),
            ["/candidate/liked"] = Private("Saved.Title", "Seo.PrivateDescription"),
            ["/candidate/shared"] = Private("Saved.TabShared", "Seo.PrivateDescription"),
            ["/candidate/applications"] = Private("Nav.MyApplications", "Seo.PrivateDescription"),
            ["/candidate/profile"] = Private("Profile.Title", "Seo.PrivateDescription"),
            ["/candidate/actions/set-unavailable"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/candidate/actions/withdraw-others"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/employer/vacancies"] = Private("Employer.VacanciesTitle", "Seo.PrivateDescription"),
            ["/branch/vacancies"] = Private("Employer.VacanciesTitle", "Seo.PrivateDescription"),
            ["/branch/vacancies/new"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/branch/applicants"] = Private("Employer.Applicants", "Seo.PrivateDescription"),
            ["/employer/tokens"] = Private("Employer.MyTokens", "Seo.PrivateDescription"),
            ["/branch/tokens"] = Private("Employer.MyTokens", "Seo.PrivateDescription"),
            ["/employer/branches"] = Private("Employer.Branches", "Seo.PrivateDescription"),
            ["/regional/branches"] = Private("Employer.Branches", "Seo.PrivateDescription"),
            ["/employer/regions"] = Private("Employer.Regions", "Seo.PrivateDescription"),
            ["/employer/users"] = Private("Employer.Users", "Seo.PrivateDescription"),
            ["/employer/company"] = Private("Admin.CompanyDetails", "Seo.PrivateDescription"),
            ["/employer/csv-import"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/employer/salary-tables"] = Private("Employer.SalaryTables", "Seo.PrivateDescription"),
            ["/employer/takeovers"] = Private("Employer.Takeovers", "Seo.PrivateDescription"),
            ["/employer/onboarding-checkout"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/employer/organization"] = Private("Nav.Organization", "Seo.PrivateDescription"),
            ["/employer/sales"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/employer/sales/payout-checkout"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/regional"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
            ["/regional/tokens"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/intermediary"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
            ["/intermediary/team"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/salesmanager"] = Private("Sales.Dashboard", "Seo.PrivateDescription"),
            ["/salesmanager/toolkit"] = Private("Sales.Toolkit", "Seo.PrivateDescription"),
            ["/salesmanager/referrals"] = Private("Sales.Referrals", "Seo.PrivateDescription"),
            ["/salesmanager/onboarding"] = Private("Sales.Onboarding", "Seo.PrivateDescription"),
            ["/salesmanager/invoices"] = Private("Sales.Invoices", "Seo.PrivateDescription"),
            ["/salesmanager/payout-checkout"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/ambassadeur"] = Private("Ambassadeur.Dashboard", "Seo.PrivateDescription"),
            ["/ambassadeur/toolkit"] = Private("Ambassadeur.Toolkit", "Seo.PrivateDescription"),
            ["/ambassadeur/onboarding"] = Private("Ambassadeur.Onboarding", "Seo.PrivateDescription"),
            ["/ambassadeur/finance"] = Private("Ambassadeur.Finance", "Seo.PrivateDescription"),
            ["/ambassadeur/payout-checkout"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/tokens/checkout-return"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/tokens/checkout-stub"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/privacy/data"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/admin"] = Private("Seo.AdminTitle", "Seo.PrivateDescription"),
            ["/admin/cockpit"] = Private("Seo.AdminTitle", "Seo.PrivateDescription"),
            ["/admin/about"] = Private("Admin.About", "Seo.PrivateDescription"),
            ["/admin/ambassadeurs"] = Private("Admin.Ambassadeurs", "Seo.PrivateDescription"),
            ["/admin/api-keys"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/admin/cnames"] = Private("Admin.Cnames", "Seo.PrivateDescription"),
            ["/admin/companies"] = Private("Admin.Companies", "Seo.PrivateDescription"),
            ["/admin/company"] = Private("Admin.CompanyDetails", "Seo.PrivateDescription"),
            ["/admin/exclusivity"] = Private("Admin.Exclusivity", "Seo.PrivateDescription"),
            ["/admin/feedback"] = Private("Admin.Feedback", "Seo.PrivateDescription"),
            ["/admin/finance"] = Private("Admin.Finance", "Seo.PrivateDescription"),
            ["/admin/integrations"] = Private("Admin.Integrations", "Seo.PrivateDescription"),
            ["/admin/logging"] = Private("Admin.Logging", "Seo.PrivateDescription"),
            ["/admin/mail-test"] = Private("Admin.MailTest", "Seo.PrivateDescription"),
            ["/admin/marketing-flyer"] = Private("Admin.MarketingFlyer", "Seo.PrivateDescription"),
            ["/admin/masterdata"] = Private("Admin.Masterdata", "Seo.PrivateDescription"),
            ["/admin/moderation"] = Private("Admin.Moderation", "Seo.PrivateDescription"),
            ["/admin/notifications"] = Private("Admin.Notifications", "Seo.PrivateDescription"),
            ["/admin/sales"] = Private("Admin.SalesCommercial", "Seo.PrivateDescription"),
            ["/admin/sales-managers"] = Private("Admin.SalesManagers", "Seo.PrivateDescription"),
            ["/admin/settings"] = Private("Admin.Settings", "Seo.PrivateDescription"),
            ["/admin/tokens"] = Private("Admin.Tokens", "Seo.PrivateDescription"),
            ["/admin/token-finance"] = Private("Seo.SiteName", "Seo.PrivateDescription"),
            ["/admin/users"] = Private("Admin.Users", "Seo.PrivateDescription"),
            ["/admin/vacancies"] = Private("Admin.Vacancies", "Seo.PrivateDescription"),
            ["/admin/vacancy-categories"] = Private("Admin.VacancyCategories", "Seo.PrivateDescription"),
            ["/admin/wages"] = Private("Admin.Wages", "Seo.PrivateDescription"),
            ["/branch"] = Private("Seo.DashboardTitle", "Seo.PrivateDescription"),
        };

    private static readonly (string Prefix, PageSeoEntry Entry)[] Prefixes =
    [
        ("/vacancies/", Public("Vacancy.Title", "Seo.VacancyFallbackDescription", "article")),
        ("/partner/", Public("Partner.Title", "Seo.PartnerDescription")),
        ("/home/metrics/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/employer/salary-tables/", Private("Employer.SalaryTables", "Seo.PrivateDescription")),
        ("/werven/", Private("Seo.SiteName", "Seo.PrivateDescription")),
        ("/ambassadeur/ref/", Private("Seo.SiteName", "Seo.PrivateDescription")),
        ("/vestiging/", Private("BranchPage.Title", "Seo.PrivateDescription")),
        ("/admin/", Private("Seo.AdminTitle", "Seo.PrivateDescription")),
        ("/employer/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/branch/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/candidate/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/salesmanager/", Private("Sales.Dashboard", "Seo.PrivateDescription")),
        ("/ambassadeur/", Private("Ambassadeur.Dashboard", "Seo.PrivateDescription")),
        ("/intermediary/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/regional/", Private("Seo.DashboardTitle", "Seo.PrivateDescription")),
        ("/tokens/", Private("Seo.SiteName", "Seo.PrivateDescription")),
    ];

    private static PageSeoEntry Public(string titleKey, string descriptionKey, string ogType = "website")
        => new(titleKey, descriptionKey, Indexable: true, ogType);

    private static PageSeoEntry Private(string titleKey, string descriptionKey)
        => new(titleKey, descriptionKey, Indexable: false);

    [GeneratedRegex(@"^/\d{8}(?:/\d{1,12})?$", RegexOptions.CultureInvariant)]
    private static partial Regex PublicCompanyPathRegex();
}
