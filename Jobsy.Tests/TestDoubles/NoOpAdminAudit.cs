using System.Security.Claims;
using Jobsy.Api.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;

namespace Jobsy.Tests;

internal sealed class NoOpAdminAuditLog : IAdminAuditLog
{
    public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Stage(AdminAuditEntry entry) { }
}

internal sealed class NoOpAdminAuditContext : IAdminAuditContext
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
    public void SetDetailsObject(object details) { }
    public void SetFieldChange(string field, string? from, string? to) { }
}

internal sealed class FakeUserLookup : IUserLookupService
{
    public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        => Task.FromResult<User?>(null);
}
