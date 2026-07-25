namespace Jobsy.Core.Entities;

public class CompanySalaryRate
{
    public Guid Id { get; set; }
    public Guid SalaryTableId { get; set; }
    public CompanySalaryTable SalaryTable { get; set; } = null!;
    public int AgeYears { get; set; }
    public decimal HourlyRate { get; set; }
    public string Label { get; set; } = string.Empty;
}
