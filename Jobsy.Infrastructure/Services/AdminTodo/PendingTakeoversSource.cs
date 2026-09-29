using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

public sealed class PendingTakeoversSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public PendingTakeoversSource(JobsyDbContext db) => _db = db;

    public string Key => "takeovers";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.EstablishmentTakeoverRequests.AsNoTracking()
            .Where(t => t.Status == TakeoverRequestStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.CreatedAt,
                Applicant = t.Registration.EstablishmentName,
                Target = t.TargetCompany.Name
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new AdminTodoItem(
            Key: $"{Key}:{t.Id}",
            Severity: AdminTodoSeverity.Warn,
            TitleKey: "AdminTodo.Takeover.Title",
            Subtitle: $"{t.Applicant} → {t.Target}",
            Area: "Organisaties",
            SinceUtc: t.CreatedAt,
            ActionLabelKey: "AdminTodo.Takeover.Action",
            Href: "/admin/organisaties?filter=takeover",
            Count: 1)).ToList();
    }
}
