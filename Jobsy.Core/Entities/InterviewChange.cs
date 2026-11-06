using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class InterviewChange
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public InterviewChangeKind Kind { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
