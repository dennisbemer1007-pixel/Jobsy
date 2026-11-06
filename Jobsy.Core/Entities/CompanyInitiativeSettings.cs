namespace Jobsy.Core.Entities;

/// <summary>Organisation-wide "open for initiative" settings (general entry).</summary>
public class CompanyInitiativeSettings
{
    public Guid Id { get; set; }
    public Guid RootCompanyId { get; set; }
    public Company RootCompany { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactEmailConfirmationCodeHash { get; set; }
    public DateTime? ContactEmailConfirmationExpiresAtUtc { get; set; }
    public int ContactEmailConfirmationFailedAttempts { get; set; }
    public DateTime? ContactEmailConfirmedAtUtc { get; set; }

    /// <summary>Max initiatives per calendar week (Mon–Sun, Europe/Amsterdam).</summary>
    public int MaxPerWeek { get; set; } = 5;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
