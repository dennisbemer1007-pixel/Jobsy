using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class InterviewSlot
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public InterviewSlotProposer ProposedBy { get; set; }
    public string? Reason { get; set; }
    public bool IsSelected { get; set; }
}
