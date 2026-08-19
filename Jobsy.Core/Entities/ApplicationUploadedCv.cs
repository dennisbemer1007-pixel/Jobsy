namespace Jobsy.Core.Entities;

/// <summary>Snapshot of the candidate-uploaded CV at apply time (employer download after Accept).</summary>
public class ApplicationUploadedCv
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public int SizeBytes { get; set; }
}
