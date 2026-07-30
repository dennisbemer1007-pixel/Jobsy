namespace Jobsy.Core.Entities;

/// <summary>
/// Per-company API credential for external vacancy integrations.
/// Only a one-way hash of the secret is stored — never plaintext.
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>SHA-256 hex hash of the full API key (for O(1) lookup).</summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>Human-readable label (e.g. "ATS koppeling").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Non-secret prefix shown in UI (e.g. "lobsy_ab12…").</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
