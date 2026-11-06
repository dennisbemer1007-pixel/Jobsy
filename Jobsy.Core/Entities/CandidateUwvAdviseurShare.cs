namespace Jobsy.Core.Entities;

/// <summary>Time-limited share link for UWV adviseur printout (token stored as hash only).</summary>
public sealed class CandidateUwvAdviseurShare
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    /// <summary>JSON array of section keys (<see cref="Uwv.UwvAdviseurSectionKeys"/>).</summary>
    public string SectionsJson { get; set; } = "[]";

    /// <summary>Two-letter language (nl, en).</summary>
    public string Language { get; set; } = "nl";

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
}
