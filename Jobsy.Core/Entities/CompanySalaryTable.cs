namespace Jobsy.Core.Entities;

public class CompanySalaryTable
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<CompanySalaryRate> Rates { get; set; } = new List<CompanySalaryRate>();
    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
}
