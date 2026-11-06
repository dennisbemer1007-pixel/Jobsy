using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

public sealed class NewFeedbackSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public NewFeedbackSource(JobsyDbContext db) => _db = db;

    public string Key => "feedback";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.PlatformFeedbacks.AsNoTracking()
            .Where(f => f.Status == FeedbackStatus.New)
            .OrderBy(f => f.CreatedAtUtc)
            .Select(f => new { f.Id, f.CreatedAtUtc, f.Type })
            .Take(100)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var bugCount = rows.Count(f => f.Type == FeedbackType.Bug);
        var subtitle = bugCount > 0
            ? $"Waarvan {bugCount} met label 'bug'"
            : $"{rows.Count} open";

        return
        [
            new AdminTodoItem(
                Key: Key,
                Severity: AdminTodoSeverity.Info,
                TitleKey: "AdminTodo.Feedback.Title",
                Subtitle: subtitle,
                Area: "Overzicht",
                SinceUtc: rows.Min(f => f.CreatedAtUtc),
                ActionLabelKey: "AdminTodo.Feedback.Action",
                Href: "/admin/feedback",
                Count: rows.Count)
        ];
    }
}
