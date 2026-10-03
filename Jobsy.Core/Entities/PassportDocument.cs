namespace Jobsy.Core.Entities;

public class PassportDocument
{
    public Guid Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public Guid CandidateUserId { get; set; }
    public User? Candidate { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string PrimaryLanguage { get; set; } = "nl";
    public string SecondaryLanguage { get; set; } = "nl";
    public bool IncludesPage2 { get; set; }
    public bool IncludesContact { get; set; }
    public Guid? PassportPartnerId { get; set; }
    /// <summary>Filled in step 05. No FK yet.</summary>
    public Guid? ShareLinkId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}

public class PassportTextTranslation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string SourceHash { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}
