using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Platform-level API credentials for external integrations (admin settings tiles).
/// </summary>
public class IntegrationCredential
{
    public Guid Id { get; set; }
    public IntegrationKey Key { get; set; }
    public string? ApiKey { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public string? Model { get; set; }
    public string? BaseUrl { get; set; }
    public string? FromAddress { get; set; }
    public bool? LastPingOk { get; set; }
    public string? LastPingMessage { get; set; }
    public DateTime? LastPingAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
