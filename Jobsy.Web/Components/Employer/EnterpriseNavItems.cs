using Jobsy.Web.Navigation;

namespace Jobsy.Web.Components.Employer;

/// <summary>
/// Desktop organization modules for Bedrijfsmanager (EnterpriseManager).
/// Surfaced via the Organization hub and subnav — hidden from mobile bottom-nav.
/// </summary>
public static class EnterpriseNavItems
{
    public static readonly NavItem[] OrganizationModules =
    [
        new("Nav.Organization", "/employer/organization", NavIcons.Settings),
        new("Nav.CompanyDetails", "/employer/company", NavIcons.Companies),
        new("Nav.Branches", "/employer/branches", NavIcons.Branches, ["/employer/takeovers"]),
        new("Nav.Regions", "/employer/regions", NavIcons.Regions),
        new("Nav.SalaryTables", "/employer/salary-tables", NavIcons.Wages),
        new("Nav.CsvImport", "/employer/csv-import", NavIcons.Batch),
        new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches)
    ];

    public static bool IsOrganizationPath(string relativePath)
    {
        var path = RoleNavCatalog.NormalizePath(relativePath);
        return OrganizationModules.Any(m =>
            RoleNavCatalog.IsActive(m, path.TrimStart('/'), OrganizationModules));
    }
}
