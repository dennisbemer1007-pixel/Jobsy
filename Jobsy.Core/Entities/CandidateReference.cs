namespace Jobsy.Core.Entities;

/// <summary>Employer reference / recensie the candidate lists for vacancies that require them.</summary>
public class CandidateReference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string EmployerName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    /// <summary>ReferenceTypeCatalog code. Employer sees neutral role label only.</summary>
    public string? ReferenceType { get; set; }
    /// <summary>Private: where the candidate knows this person from. Never employer-facing.</summary>
    public string? RelationshipContext { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
