using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Navigation;

public sealed record SalesNavGroup(
    string Key,
    string LabelKey,
    IReadOnlyList<SalesNavItem> Items);

public sealed record SalesNavItem(
    string Key,
    string LabelKey,
    string Href,
    string Svg,
    IReadOnlyList<string> Roles,
    bool IsAvailable = true,
    bool RequiresCanRecruit = false,
    bool ShowInBottomNav = false,
    string? BottomLabelKey = null,
    int BottomOrder = 0);

/// <summary>
/// Single catalog for Lobsy Partner sidebar, breadcrumbs and mobile bottom nav (§IA).
/// </summary>
public static class SalesNav
{
    public static readonly IReadOnlyList<SalesNavGroup> Groups =
    [
        new("overview", "Sales.Nav.Group.Overview",
        [
            new("dashboard", "Sales.Nav.Dashboard", "/sales", NavIcons.Home,
                [JobsyRoles.SalesManager], IsAvailable: true, ShowInBottomNav: true,
                BottomLabelKey: "Sales.Nav.Bottom.Overview", BottomOrder: 1),
        ]),
        new("sell", "Sales.Nav.Group.Sell",
        [
            new("link", "Sales.Nav.Link", "/sales/link", NavIcons.Shared,
                [JobsyRoles.SalesManager], IsAvailable: true, ShowInBottomNav: true,
                BottomLabelKey: "Sales.Nav.Bottom.Link", BottomOrder: 2),
            new("employers", "Sales.Nav.Employers", "/sales/werkgevers", NavIcons.Companies,
                [JobsyRoles.SalesManager], IsAvailable: false, ShowInBottomNav: true,
                BottomLabelKey: "Sales.Nav.Bottom.Employers", BottomOrder: 3),
            new("recommend", "Sales.Nav.Recommend", "/sales/aanbevelen", NavIcons.Users,
                [JobsyRoles.SalesManager], IsAvailable: true, RequiresCanRecruit: true),
        ]),
        new("money", "Sales.Nav.Group.Money",
        [
            new("wallet", "Sales.Nav.Wallet", "/sales/wallet", NavIcons.Tokens,
                [JobsyRoles.SalesManager], IsAvailable: true, ShowInBottomNav: true,
                BottomLabelKey: "Sales.Nav.Bottom.Wallet", BottomOrder: 4),
        ]),
        new("account", "Sales.Nav.Group.Account",
        [
            new("profile", "Sales.Nav.Profile", "/sales/profiel", NavIcons.Profile,
                [JobsyRoles.SalesManager], IsAvailable: false),
            new("help", "Sales.Nav.Help", "/sales/hulp", NavIcons.Info,
                [JobsyRoles.SalesManager], IsAvailable: false),
        ]),
    ];

    public static IEnumerable<SalesNavItem> VisibleItems(
        bool canRecruit,
        IReadOnlySet<string>? roles = null)
    {
        roles ??= new HashSet<string>(StringComparer.Ordinal) { JobsyRoles.SalesManager };
        foreach (var group in Groups)
        {
            foreach (var item in group.Items)
            {
                if (!item.IsAvailable)
                {
                    continue;
                }

                if (item.RequiresCanRecruit && !canRecruit)
                {
                    continue;
                }

                if (!item.Roles.Any(roles.Contains))
                {
                    continue;
                }

                yield return item;
            }
        }
    }

    public static IReadOnlyList<SalesNavItem> BottomNavItems(bool canRecruit)
        => VisibleItems(canRecruit)
            .Where(i => i.ShowInBottomNav)
            .OrderBy(i => i.BottomOrder)
            .ToList();

    public static (SalesNavGroup? Group, SalesNavItem? Item) Match(string path)
    {
        var clean = path.Split('?', '#')[0].TrimEnd('/');
        if (string.IsNullOrEmpty(clean))
        {
            clean = "/";
        }

        foreach (var group in Groups)
        {
            foreach (var item in group.Items)
            {
                if (string.Equals(item.Href.TrimEnd('/'), clean, StringComparison.OrdinalIgnoreCase)
                    || (item.Href == "/sales" && clean == "/sales"))
                {
                    return (group, item);
                }
            }
        }

        return (null, null);
    }
}

/// <summary>Legacy salesmanager URLs → new /sales/* (301).</summary>
public static class SalesLegacyRoutes
{
    public static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/salesmanager"] = "/sales",
            ["/salesmanager/toolkit"] = "/sales/link",
            ["/salesmanager/referrals"] = "/sales/aanbevelen",
            ["/salesmanager/onboarding"] = "/sales/start",
            ["/salesmanager/invoices"] = "/sales/wallet?tab=facturen",
            ["/salesmanager/payout-checkout"] = "/sales/wallet/uitbetalen",
        };
}
