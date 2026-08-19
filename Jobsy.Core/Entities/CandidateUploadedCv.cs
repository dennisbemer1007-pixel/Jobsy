namespace Jobsy.Core.Entities;

/// <summary>One candidate-uploaded CV file (PDF/DOCX). Replaced on re-upload.</summary>
public class CandidateUploadedCv
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public int SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public DateTime? ExtractedAtUtc { get; set; }

    /// <summary>JSON array of Dutch field labels filled from the CV (no PII values).</summary>
    public string? FilledFieldsJson { get; set; }
}
