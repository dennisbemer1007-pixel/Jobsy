namespace Jobsy.Core.Entities;

/// <summary>Private attachment for a manual job-search activity (UWV werkmap).</summary>
public class JobSearchActivityAttachment
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public int SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
