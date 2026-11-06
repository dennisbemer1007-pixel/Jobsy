namespace Jobsy.Core.Entities;

/// <summary>Blocks outbound external-vacancy mail to an e-mail address or whole domain.</summary>
public class OutboundRecipientSuppression
{
    public string NormalizedEmail { get; set; } = string.Empty;
    public string NormalizedDomain { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}
