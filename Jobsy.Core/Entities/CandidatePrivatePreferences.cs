namespace Jobsy.Core.Entities;

/// <summary>
/// Candidate-only private preferences (dislikes). Separate from PreferencesJson so
/// employer-facing readers never see this data by design.
/// </summary>
public class CandidatePrivatePreferences
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>JSON array of catalog dislike codes.</summary>
    public string DislikesJson { get; set; } = "[]";

    /// <summary>JSON array of free-text custom dislikes (max 5 × 40).</summary>
    public string CustomDislikesJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; }
}
