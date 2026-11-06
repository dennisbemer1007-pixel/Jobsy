using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class ApplicationOutcome
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public AfterMatchOutcomeKind Outcome { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool ToneCheckOk { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}
