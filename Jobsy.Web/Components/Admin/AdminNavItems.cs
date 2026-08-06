using Jobsy.Web.Navigation;

namespace Jobsy.Web.Components.Admin;

/// <summary>
/// Secondary admin modules under Settings (bottom-nav ExtraActivePaths).
/// Hub-only shortcuts that used to live on Home.
/// </summary>
public static class AdminNavItems
{
    public static readonly NavItem[] SettingsModules =
    [
        new("Nav.Settings", "/admin/settings", NavIcons.Settings),
        new("Nav.Cnames", "/admin/cnames", NavIcons.Settings),
        new("Nav.CompanyDetails", "/admin/company", NavIcons.Companies),
        new("Nav.AboutPage", "/admin/about", NavIcons.Info),
        new("Nav.Masterdata", "/admin/masterdata", NavIcons.Masterdata),
        new("Nav.VacancyCategories", "/admin/vacancy-categories", NavIcons.Masterdata),
        new("Nav.Exclusivity", "/admin/exclusivity", NavIcons.Masterdata),
        new("Nav.Integrations", "/admin/integrations", NavIcons.Api),
        new("Nav.ApiKeys", "/admin/api-keys", NavIcons.Api),
        new("Nav.Notifications", "/admin/notifications", NavIcons.Notifications),
        new("Nav.Users", "/admin/users", NavIcons.Users),
        new("Nav.Logging", "/admin/logging", NavIcons.Logging),
        new("Nav.Wages", "/admin/wages", NavIcons.Wages),
    ];

    public static bool IsActive(NavItem item, string relativePath)
        => RoleNavCatalog.IsActive(item, relativePath);
}
