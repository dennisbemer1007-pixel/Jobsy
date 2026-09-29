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
        new("Nav.CompanyDetails", "/werkgever/organisatie/profiel", NavIcons.Companies),
        new("Nav.Branches", "/werkgever/organisatie/vestigingen", NavIcons.Branches, ["/werkgever/overnames"]),
        new("Nav.Regions", "/werkgever/organisatie/vestigingen?tab=regios", NavIcons.Regions),
        new("Nav.SalaryTables", "/werkgever/organisatie/salaristabellen", NavIcons.Wages),
        new("Nav.CsvImport", "/werkgever/koppelingen?tab=csv", NavIcons.Batch),
        new("Nav.Takeovers", "/werkgever/overnames", NavIcons.Branches)
    ];
}
