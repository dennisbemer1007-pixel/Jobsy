namespace Jobsy.Core.Interfaces;

public interface IAmbassadeurSettingsService
{
    Task<AmbassadeurSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<AmbassadeurSettingsDto> UpdateAsync(
        AmbassadeurSettingsUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task SetCommissionOverrideAsync(
        Guid ambassadeurUserId,
        decimal? percentageOverride,
        CancellationToken cancellationToken = default);
}

public sealed record AmbassadeurSettingsDto(
    int CandidateThreshold,
    decimal PercentPerThreshold,
    decimal MaxCommissionPercentage,
    DateTime UpdatedAtUtc);

public sealed record AmbassadeurSettingsUpdateRequest(
    int CandidateThreshold,
    decimal PercentPerThreshold,
    decimal MaxCommissionPercentage);
