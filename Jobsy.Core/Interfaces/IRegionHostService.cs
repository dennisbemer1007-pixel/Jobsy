using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IRegionHostService
{
    Task<IReadOnlyList<RegionHost>> ListAsync(CancellationToken cancellationToken = default);

    Task<RegionHost?> FindByHostnameAsync(string hostname, CancellationToken cancellationToken = default);

    Task<RegionHost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RegionHost> CreateAsync(RegionHostUpsert upsert, CancellationToken cancellationToken = default);

    Task<RegionHost?> UpdateAsync(Guid id, RegionHostUpsert upsert, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record RegionHostUpsert(
    string Hostname,
    string DisplayName,
    string? Slogan,
    string? AddressLabel,
    double? Latitude,
    double? Longitude,
    string? BackgroundImageUrl,
    bool IsActive = true);
