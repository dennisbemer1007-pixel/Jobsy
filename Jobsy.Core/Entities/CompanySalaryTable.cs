namespace Jobsy.Core.Entities;

/// <summary>
/// Organization-owned salary scale. Vestigingen use it via <see cref="AllowedBranches"/>
/// (system WML is available to all vestigingen under the organization).
/// </summary>
public class CompanySalaryTable
{
    public Guid Id { get; set; }

    /// <summary>Owning organization (root company), not a vestiging.</summary>
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Platform WML mirror; rates sync from MinimumWageRates; not company-editable.</summary>
    public bool IsSystemWml { get; set; }

    public ICollection<CompanySalaryRate> Rates { get; set; } = new List<CompanySalaryRate>();
    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
    public ICollection<CompanySalaryTableAllowedBranch> AllowedBranches { get; set; } = new List<CompanySalaryTableAllowedBranch>();
    public ICollection<CompanySalaryTableChangeLog> ChangeLogs { get; set; } = new List<CompanySalaryTableChangeLog>();
}
