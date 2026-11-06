using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class BeheerTodoService : IBeheerTodoService
{
    private readonly JobsyDbContext _db;

    public BeheerTodoService(JobsyDbContext db) => _db = db;

    public async Task<IReadOnlyList<BeheerTodoDto>> ListAsync(
        BeheerTodoStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = _db.BeheerTodos.AsNoTracking();
        if (statusFilter is not null)
        {
            query = query.Where(t => t.Status == statusFilter);
        }

        var rows = await query
            .OrderBy(t => t.Status == BeheerTodoStatus.Klaar ? 1 : 0)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapManyAsync(rows, cancellationToken);
    }

    public async Task<BeheerTodoDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.BeheerTodos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return row is null ? null : (await MapManyAsync([row], cancellationToken))[0];
    }

    public async Task<BeheerTodoDto> CreateAsync(
        BeheerTodoUpsert input,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        Validate(input);
        var now = DateTime.UtcNow;
        var row = new BeheerTodo
        {
            Id = Guid.NewGuid(),
            Title = input.Title.Trim(),
            Body = input.Body ?? string.Empty,
            DueDate = input.DueDate,
            Priority = input.Priority,
            Status = input.Status,
            Category = input.Category.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
        };
        _db.BeheerTodos.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapManyAsync([row], cancellationToken))[0];
    }

    public async Task<BeheerTodoDto?> UpdateAsync(
        Guid id,
        BeheerTodoUpsert input,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        Validate(input);
        var row = await _db.BeheerTodos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.Title = input.Title.Trim();
        row.Body = input.Body ?? string.Empty;
        row.DueDate = input.DueDate;
        row.Priority = input.Priority;
        row.Status = input.Status;
        row.Category = input.Category.Trim();
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedByUserId = actorUserId;
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapManyAsync([row], cancellationToken))[0];
    }

    public async Task<BeheerTodoDto?> MarkDoneAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken)
    {
        var row = await _db.BeheerTodos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.Status = BeheerTodoStatus.Klaar;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedByUserId = actorUserId;
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapManyAsync([row], cancellationToken))[0];
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.BeheerTodos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (row is null)
        {
            return false;
        }

        _db.BeheerTodos.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Validate(BeheerTodoUpsert input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new ArgumentException("Titel is verplicht.");
        }

        if (input.Title.Trim().Length > 500)
        {
            throw new ArgumentException("Titel is te lang (max. 500 tekens).");
        }

        if (string.IsNullOrWhiteSpace(input.Category))
        {
            throw new ArgumentException("Categorie is verplicht.");
        }

        if (input.Category.Trim().Length > 64)
        {
            throw new ArgumentException("Categorie is te lang (max. 64 tekens).");
        }

        if (input.Body.Length > 50_000)
        {
            throw new ArgumentException("Tekst is te lang (max. 50.000 tekens).");
        }
    }

    private async Task<IReadOnlyList<BeheerTodoDto>> MapManyAsync(
        IReadOnlyList<BeheerTodo> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var userIds = rows
            .SelectMany(r => new[] { r.CreatedByUserId, r.UpdatedByUserId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var names = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToDictionaryAsync(
                    u => u.Id,
                    u => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName,
                    cancellationToken);

        return rows.Select(r => new BeheerTodoDto(
            r.Id,
            r.Title,
            r.Body,
            r.DueDate,
            r.Priority,
            r.Status,
            r.Category,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.CreatedByUserId,
            r.CreatedByUserId is Guid c && names.TryGetValue(c, out var cn) ? cn : null,
            r.UpdatedByUserId,
            r.UpdatedByUserId is Guid u && names.TryGetValue(u, out var un) ? un : null)).ToList();
    }
}
