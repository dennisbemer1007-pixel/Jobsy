namespace Jobsy.Web.Models;

public sealed class SampleAssessmentReportPreviewState
{
    public string FileName { get; set; } = "";
    public int TotalPages { get; set; }
    public List<string> PagePngBase64 { get; set; } = [];
}

public sealed class AssessmentAdjustmentState
{
    public int Used { get; set; }
    public int Remaining { get; set; }
    public int Max { get; set; }
    public bool HasDraft { get; set; }
    public DateTime? LastAdjustedAtUtc { get; set; }
}

public sealed class AssessmentRetakeStartResult
{
    public Guid AttemptId { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
}

public sealed class AssessmentHistoryItem
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class AssessmentAdjustmentLimitClientException : Exception
{
    public AssessmentAdjustmentLimitClientException(string body)
        : base("assessment_adjustment_limit")
    {
        Body = body;
    }

    public string Body { get; }
}
