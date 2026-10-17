using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record BeheerTodoDto(
    Guid Id,
    string Title,
    string Body,
    DateOnly? DueDate,
    BeheerTodoPriority Priority,
    BeheerTodoStatus Status,
    string Category,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    Guid? CreatedByUserId,
    string? CreatedByDisplayName,
    Guid? UpdatedByUserId,
    string? UpdatedByDisplayName);

public sealed record BeheerTodoUpsert(
    string Title,
    string Body,
    DateOnly? DueDate,
    BeheerTodoPriority Priority,
    BeheerTodoStatus Status,
    string Category);

public interface IBeheerTodoService
{
    Task<IReadOnlyList<BeheerTodoDto>> ListAsync(BeheerTodoStatus? statusFilter, CancellationToken cancellationToken);

    Task<BeheerTodoDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<BeheerTodoDto> CreateAsync(BeheerTodoUpsert input, Guid actorUserId, CancellationToken cancellationToken);

    Task<BeheerTodoDto?> UpdateAsync(Guid id, BeheerTodoUpsert input, Guid actorUserId, CancellationToken cancellationToken);

    Task<BeheerTodoDto?> MarkDoneAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
