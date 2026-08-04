using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IExclusivitySettingService
{
    Task<IReadOnlyList<ExclusivitySettingDto>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<ExclusivitySettingDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ExclusivitySettingDto> CreateAsync(
        ExclusivitySettingUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<ExclusivitySettingDto> UpdateAsync(
        Guid id,
        ExclusivitySettingUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}

public sealed record ExclusivityEducationDto(Guid Id, string Name, int SortOrder, bool IsActive);

public sealed record ExclusivitySettingDto(
    Guid Id,
    string Name,
    string? SchoolDomain,
    string? StudentNumberPattern,
    bool IsActive,
    bool IsOpenOption,
    int SortOrder,
    IReadOnlyList<ExclusivityEducationDto> Educations);

public sealed record ExclusivitySettingUpsertRequest(
    string Name,
    string? SchoolDomain,
    string? StudentNumberPattern,
    bool IsActive,
    bool IsOpenOption,
    int SortOrder,
    IReadOnlyList<string>? Educations = null);
