namespace Jobsy.Core.Entities;

public class CompanySalaryTableChangeLog
{
    public Guid Id { get; set; }
    public Guid SalaryTableId { get; set; }
    public CompanySalaryTable SalaryTable { get; set; } = null!;

    /// <summary>Created or Updated.</summary>
    public string Action { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
