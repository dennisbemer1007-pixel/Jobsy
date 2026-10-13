using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

public sealed class PendingSalesManagerApplicationsSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public PendingSalesManagerApplicationsSource(JobsyDbContext db) => _db = db;

    public string Key => "sales-apps";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.SalesManagerApplications.AsNoTracking()
            .Where(a => a.Status == SalesManagerApplicationStatus.Pending)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => new { a.Id, a.CandidateFullName, a.CreatedAtUtc })
            .Take(50)
            .ToListAsync(cancellationToken);

        return rows.Select(a => new AdminTodoItem(
            Key: $"{Key}:{a.Id}",
            Severity: AdminTodoSeverity.Info,
            TitleKey: "AdminTodo.SalesApp.Title",
            Subtitle: a.CandidateFullName,
            Area: "Gebruikers",
            SinceUtc: a.CreatedAtUtc,
            ActionLabelKey: "AdminTodo.SalesApp.Action",
            Href: "/admin/gebruikers/sales",
            Count: 1)).ToList();
    }
}
