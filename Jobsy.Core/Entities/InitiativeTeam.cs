namespace Jobsy.Core.Entities;

/// <summary>Team entry point on the public company page. Candidates see team names, never staff names.</summary>
public class InitiativeTeam
{
    public Guid Id { get; set; }
    public Guid RootCompanyId { get; set; }
    public Company RootCompany { get; set; } = null!;

    /// <summary>Vestiging (branch) company row when scoped to one location.</summary>
    public Guid? BranchCompanyId { get; set; }
    public Company? BranchCompany { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactEmailConfirmationCodeHash { get; set; }
    public DateTime? ContactEmailConfirmationExpiresAtUtc { get; set; }
    public int ContactEmailConfirmationFailedAttempts { get; set; }
    public DateTime? ContactEmailConfirmedAtUtc { get; set; }

    public int MaxPerWeek { get; set; } = 5;
    public bool IsOpen { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
