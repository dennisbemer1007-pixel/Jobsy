namespace Jobsy.Core.Entities;

public class MinimumWageRate
{
    public Guid Id { get; set; }
    public int AgeYears { get; set; }
    public decimal HourlyRate { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
}
