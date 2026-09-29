namespace Jobsy.Web.Navigation;

public sealed record AdminNavGroup(
    string Key,
    string LabelKey,
    IReadOnlyList<AdminNavItem> Items);

public sealed record AdminNavItem(
    string Key,
    string LabelKey,
    string Href,
    string Svg,
    IReadOnlyList<string> Aliases,
    bool IsAvailable = true,
    string? CountKey = null);

/// <summary>
/// Single source of truth for admin sidebar labels, hrefs and breadcrumbs (§IA).
/// Items with <see cref="AdminNavItem.IsAvailable"/> false are not rendered until their page ships.
/// </summary>
public static class AdminNav
{
    // Slot for later: Tests & normen under Kandidaten & tests (deferred; not rendered).

    public static readonly IReadOnlyList<AdminNavGroup> Groups =
    [
        new("overview", "AdminNav.Group.Overview",
        [
            new("dashboard", "AdminNav.Dashboard", "/admin", NavIcons.Home,
                ["/admin/cockpit"], IsAvailable: true),
            new("todo", "AdminNav.Todo", "/admin/te-doen", NavIcons.List,
                [], IsAvailable: true, CountKey: "todo"),
            new("feedback", "AdminNav.Feedback", "/admin/feedback", NavIcons.Feedback,
                [], IsAvailable: true, CountKey: "feedback"),
        ]),
        new("users", "AdminNav.Group.Users",
        [
            new("all-users", "AdminNav.AllUsers", "/admin/gebruikers", NavIcons.Users,
                ["/admin/users"], IsAvailable: true),
            new("roles", "AdminNav.Roles", "/admin/gebruikers/rollen", NavIcons.Settings,
                [], IsAvailable: true),
            new("sales", "AdminNav.SalesAmbassadors", "/admin/gebruikers/sales", NavIcons.Shared,
                ["/admin/sales-managers", "/admin/ambassadeurs"], IsAvailable: true),
        ]),
        new("orgs", "AdminNav.Group.Organisations",
        [
            new("companies", "AdminNav.Companies", "/admin/organisaties", NavIcons.Companies,
                ["/admin/companies"], IsAvailable: true),
            new("regions", "AdminNav.Regions", "/admin/organisaties/regios", NavIcons.Regions,
                ["/admin/cnames"], IsAvailable: true),
            new("requests", "AdminNav.Requests", "/admin/organisaties/aanvragen", NavIcons.Applications,
                [], IsAvailable: true, CountKey: "org-requests"),
        ]),
        new("candidates", "AdminNav.Group.Candidates",
        [
            new("candidates", "AdminNav.Candidates", "/admin/kandidaten", NavIcons.Users,
                [], IsAvailable: true),
            // Tests & normen — deferred slot (comment only; no nav item).
        ]),
        new("vacancies", "AdminNav.Group.Vacancies",
        [
            new("vacancies", "AdminNav.Vacancies", "/admin/vacatures", NavIcons.Vacancies,
                ["/admin/vacancies"], IsAvailable: true),
            new("ats", "AdminNav.Ats", "/admin/vacatures/ats", NavIcons.List,
                ["/admin/ats-vacancies"], IsAvailable: true),
            new("moderation", "AdminNav.Moderation", "/admin/vacatures/moderatie", NavIcons.Vacancies,
                ["/admin/moderation"], IsAvailable: true, CountKey: "moderation"),
            new("categories", "AdminNav.CategoriesWages", "/admin/vacatures/categorieen", NavIcons.Masterdata,
                ["/admin/vacancy-categories", "/admin/wages"], IsAvailable: true),
        ]),
        new("finance", "AdminNav.Group.Finance",
        [
            new("revenue", "AdminNav.Revenue", "/admin/financien", NavIcons.Finance,
                ["/admin/finance"], IsAvailable: true),
            new("pricing", "AdminNav.Pricing", "/admin/financien/prijzen", NavIcons.Wages,
                ["/admin/sales"], IsAvailable: true),
            new("goodwill", "AdminNav.Goodwill", "/admin/financien/goodwill", NavIcons.Tokens,
                ["/admin/tokens"], IsAvailable: true),
            new("payouts", "AdminNav.Payouts", "/admin/financien/uitbetalingen", NavIcons.Finance,
                ["/admin/token-finance"], IsAvailable: true, CountKey: "payouts"),
        ]),
        new("content", "AdminNav.Group.Content",
        [
            new("pages", "AdminNav.PagesFlyer", "/admin/content/paginas", NavIcons.Info,
                ["/admin/about", "/admin/marketing-flyer"], IsAvailable: true),
            new("training", "AdminNav.Training", "/admin/content/opleidingen", NavIcons.Masterdata,
                ["/admin/training"], IsAvailable: true),
            new("masterdata", "AdminNav.Masterdata", "/admin/content/stamgegevens", NavIcons.Masterdata,
                ["/admin/masterdata", "/admin/exclusivity"], IsAvailable: true),
            new("emails", "AdminNav.Emails", "/admin/content/emails", NavIcons.Notifications,
                ["/admin/mail-test", "/admin/notifications"], IsAvailable: true),
        ]),
        new("settings", "AdminNav.Group.Settings",
        [
            new("features", "AdminNav.Features", "/admin/instellingen", NavIcons.Settings,
                ["/admin/settings"], IsAvailable: true),
            new("general", "AdminNav.General", "/admin/instellingen/algemeen", NavIcons.Companies,
                ["/admin/company"], IsAvailable: true),
            new("integrations", "AdminNav.Integrations", "/admin/instellingen/integraties", NavIcons.Api,
                ["/admin/integrations", "/admin/api-keys"], IsAvailable: true),
        ]),
        new("security", "AdminNav.Group.Security",
        [
            new("audit", "AdminNav.AuditLog", "/admin/beveiliging", NavIcons.Logging,
                [], IsAvailable: false),
            new("pii", "AdminNav.DataAccess", "/admin/beveiliging/gegevensinzage", NavIcons.Logging,
                ["/admin/personal-data-access-log"], IsAvailable: true),
            new("mfa", "AdminNav.MfaSessions", "/admin/beveiliging/2fa", NavIcons.Settings,
                [], IsAvailable: false),
            new("privacy", "AdminNav.Privacy", "/admin/beveiliging/privacy", NavIcons.Info,
                [], IsAvailable: false),
            new("logs", "AdminNav.SystemLogs", "/admin/beveiliging/systeemlogs", NavIcons.Logging,
                ["/admin/logging"], IsAvailable: true),
        ]),
    ];

    public static (AdminNavGroup Group, AdminNavItem Item)? Resolve(string relativePath)
    {
        var path = Normalize(relativePath);
        AdminNavGroup? bestGroup = null;
        AdminNavItem? bestItem = null;
        var bestScore = -1;

        foreach (var group in Groups)
        {
            foreach (var item in group.Items)
            {
                var href = Normalize(item.Href);
                if (string.Equals(path, href, StringComparison.OrdinalIgnoreCase))
                {
                    return (group, item);
                }

                foreach (var alias in item.Aliases)
                {
                    if (string.Equals(path, Normalize(alias), StringComparison.OrdinalIgnoreCase))
                    {
                        return (group, item);
                    }
                }

                if (href is not "/" && path.StartsWith(href + "/", StringComparison.OrdinalIgnoreCase))
                {
                    var score = href.Length;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestGroup = group;
                        bestItem = item;
                    }
                }
            }
        }

        return bestGroup is null || bestItem is null ? null : (bestGroup, bestItem);
    }

    public static IReadOnlyList<(string LabelKey, string? Href, bool IsCurrent)> Crumbs(
        string relativePath,
        string? detail = null)
    {
        var crumbs = new List<(string LabelKey, string? Href, bool IsCurrent)>
        {
            ("AdminShell.Beheer", "/admin", false)
        };

        var resolved = Resolve(relativePath);
        if (resolved is null)
        {
            if (!string.IsNullOrWhiteSpace(detail))
            {
                crumbs.Add((detail, null, true));
            }
            else if (crumbs.Count > 0)
            {
                crumbs[^1] = (crumbs[^1].LabelKey, crumbs[^1].Href, true);
            }

            return crumbs;
        }

        var (group, item) = resolved.Value;
        crumbs.Add((group.LabelKey, null, false));
        var itemIsCurrent = string.IsNullOrWhiteSpace(detail);
        crumbs.Add((item.LabelKey, itemIsCurrent ? null : item.Href, itemIsCurrent));
        if (!string.IsNullOrWhiteSpace(detail))
        {
            crumbs.Add((detail, null, true));
        }

        return crumbs;
    }

    public static IEnumerable<AdminNavItem> AvailableItems()
        => Groups.SelectMany(g => g.Items).Where(i => i.IsAvailable);

    public static string Normalize(string relativePath)
    {
        var path = relativePath.Split('?', 2)[0].Split('#', 2)[0].Trim('/');
        return string.IsNullOrEmpty(path) ? "/" : "/" + path;
    }
}
