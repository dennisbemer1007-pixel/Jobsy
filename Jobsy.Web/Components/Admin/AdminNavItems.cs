using Jobsy.Web.Navigation;

namespace Jobsy.Web.Components.Admin;

/// <summary>
/// Hub tiles — RoleNavCatalog.Admin plus optional hub-only modules.
/// </summary>
public static class AdminNavItems
{
    public static readonly NavItem[] All =
    [
        ..RoleNavCatalog.Admin,
        new("Nav.Integrations", "/admin/integrations", NavIcons.Api),
        new("Nav.Notifications", "/admin/notifications", NavIcons.Notifications),
    ];

    public static bool IsActive(NavItem item, string relativePath)
        => RoleNavCatalog.IsActive(item, relativePath);
}
