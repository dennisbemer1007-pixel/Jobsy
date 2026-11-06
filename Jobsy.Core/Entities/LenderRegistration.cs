namespace Jobsy.Core.Entities;

/// <summary>
/// History row for a bureau's uitleenregistratie (Waadi / Wtta). The latest row decides (intermediair §D).
/// </summary>
public class LenderRegistration
{
    public Guid Id { get; set; }

    /// <summary>Bureau organisation (Type Intermediary) company id.</summary>
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary><c>NotChecked</c> / <c>Pending</c> / <c>Verified</c> / <c>Rejected</c>.</summary>
    public string Status { get; set; } = Interfaces.LenderRegistrationStatuses.NotChecked;

    /// <summary><c>WaadiKvk</c> / <c>WttaNau</c> / <c>AdminManual</c>.</summary>
    public string? Source { get; set; }

    public string? Reference { get; set; }
    public DateTime? CheckedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public string? Note { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
