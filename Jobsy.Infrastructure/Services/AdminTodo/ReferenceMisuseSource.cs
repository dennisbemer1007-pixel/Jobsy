using Jobsy.Core.Admin;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

public sealed class ReferenceMisuseSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public ReferenceMisuseSource(JobsyDbContext db) => _db = db;

    public string Key => "reference-misuse";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.ReferenceMisuseReports.AsNoTracking()
            .Where(r => r.HandledAtUtc == null)
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => r.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        return
        [
            new AdminTodoItem(
                Key: Key,
                Severity: AdminTodoSeverity.Warn,
                TitleKey: "AdminTodo.Misuse.Title",
                Subtitle: $"{rows.Count} open",
                Area: "Beveiliging",
                SinceUtc: rows.Min(),
                ActionLabelKey: "AdminTodo.Misuse.Action",
                Href: "/admin/beveiliging/referent-misbruik",
                Count: rows.Count)
        ];
    }
}
