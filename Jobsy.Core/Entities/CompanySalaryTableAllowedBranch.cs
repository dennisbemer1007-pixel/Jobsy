namespace Jobsy.Core.Entities;

/// <summary>
/// Vestiging that may use an organization-owned salary table when posting vacancies.
/// </summary>
public class CompanySalaryTableAllowedBranch
{
    public Guid SalaryTableId { get; set; }
    public CompanySalaryTable SalaryTable { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
