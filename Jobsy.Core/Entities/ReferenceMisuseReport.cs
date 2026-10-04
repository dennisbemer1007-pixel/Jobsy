namespace Jobsy.Core.Entities;

/// <summary>A referee says this request should not have been sent. Deleted with the candidate.</summary>
public class ReferenceMisuseReport
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ReferenceConfirmationId { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
