using System.Text.Json;
using Jobsy.Core.Admin;

namespace Jobsy.Api.Admin;

/// <summary>
/// Per-request bag for enriching / suppressing the automatic <see cref="AdminAuditFilter"/> write.
/// </summary>
public interface IAdminAuditContext
{
    string? Reason { get; set; }
    string? TargetLabel { get; set; }
    string? TargetId { get; set; }
    string? TargetType { get; set; }
    string? DetailsJson { get; set; }
    string? ActionOverride { get; set; }
    string? ResultOverride { get; set; }
    bool SuppressAutoWrite { get; set; }
    /// <summary>When true, filter still writes even if the caller is not Admin (e.g. self-delete).</summary>
    bool ForceWrite { get; set; }

    void SetDetailsObject(object details);
    void SetFieldChange(string field, string? from, string? to);
}

public sealed class AdminAuditContext : IAdminAuditContext
{
    public string? Reason { get; set; }
    public string? TargetLabel { get; set; }
    public string? TargetId { get; set; }
    public string? TargetType { get; set; }
    public string? DetailsJson { get; set; }
    public string? ActionOverride { get; set; }
    public string? ResultOverride { get; set; }
    public bool SuppressAutoWrite { get; set; }
    public bool ForceWrite { get; set; }

    public void SetDetailsObject(object details)
        => DetailsJson = JsonSerializer.Serialize(details);

    public void SetFieldChange(string field, string? from, string? to)
        => DetailsJson = JsonSerializer.Serialize(new { field, from, to });
}
