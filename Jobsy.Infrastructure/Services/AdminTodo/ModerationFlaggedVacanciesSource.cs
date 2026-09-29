using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

public sealed class ModerationFlaggedVacanciesSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public ModerationFlaggedVacanciesSource(JobsyDbContext db) => _db = db;

    public string Key => "moderation";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        // No soft-delete column; exclude archived/fulfilled as effectively gone.
        var flagged = await _db.Vacancies.AsNoTracking()
            .Where(v => !v.ContentModerationPassed
                        && v.Status != VacancyStatus.Archived
                        && v.Status != VacancyStatus.Fulfilled)
            .OrderBy(v => v.CreatedAtUtc)
            .Select(v => new { v.Id, v.Title, v.CreatedAtUtc })
            .Take(100)
            .ToListAsync(cancellationToken);

        if (flagged.Count == 0)
        {
            return [];
        }

        var oldest = flagged.Min(v => v.CreatedAtUtc);
        var sample = flagged[0].Title;
        return
        [
            new AdminTodoItem(
                Key: Key,
                Severity: AdminTodoSeverity.Warn,
                TitleKey: "AdminTodo.Moderation.Title",
                Subtitle: sample,
                Area: "Vacatures",
                SinceUtc: oldest,
                ActionLabelKey: "AdminTodo.Moderation.Action",
                Href: "/admin/vacatures/moderatie",
                Count: flagged.Count)
        ];
    }
}
