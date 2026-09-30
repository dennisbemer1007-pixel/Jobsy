namespace Jobsy.Core.Contracts.Sales;

/// <summary>
/// Placeholder so the Contracts.Sales namespace exists for privacy reflection guards.
/// Real portal DTOs (e.g. SalesEmployerDto) arrive in later stack files.
/// </summary>
public sealed class SalesEmployerDtoPlaceholder
{
    public Guid CompanyId { get; init; }
    public string DisplayName { get; init; } = "";
    public string? Place { get; init; }
    public DateTime? AttributedAtUtc { get; init; }
}
