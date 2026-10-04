namespace Jobsy.Core.Entities;

/// <summary>Single-use deeplink for a referee. Only the hash is stored.</summary>
public class ReferenceConfirmationToken
{
    public Guid Id { get; set; }
    public Guid ReferenceConfirmationId { get; set; }
    public ReferenceConfirmation ReferenceConfirmation { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
