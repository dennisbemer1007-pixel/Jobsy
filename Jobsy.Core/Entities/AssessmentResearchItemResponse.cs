namespace Jobsy.Core.Entities;

public class AssessmentResearchItemResponse
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public AssessmentResearchAttempt? Attempt { get; set; }
    public int ItemId { get; set; }
    public int ResponseRaw { get; set; }
    public int? DurationMs { get; set; }
    public bool ItemUnclear { get; set; }
}
