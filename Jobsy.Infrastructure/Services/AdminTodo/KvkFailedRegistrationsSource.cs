using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

public sealed class KvkFailedRegistrationsSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public KvkFailedRegistrationsSource(JobsyDbContext db) => _db = db;

    public string Key => "kvk-failed";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var registrations = await _db.CompanyRegistrations.AsNoTracking()
            .Where(r => r.KvkVerificationStatus == KvkVerificationStatus.Failed)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new { r.Id, r.EstablishmentName, r.CreatedAt })
            .Take(50)
            .ToListAsync(cancellationToken);

        var companies = await _db.Companies.AsNoTracking()
            .Where(c => c.KvkVerificationStatus == KvkVerificationStatus.Failed)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                Since = c.KvkLastVerificationAttemptAtUtc ?? c.KvkVerifiedAtUtc
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        var items = new List<AdminTodoItem>();
        foreach (var r in registrations)
        {
            items.Add(new AdminTodoItem(
                Key: $"{Key}:reg:{r.Id}",
                Severity: AdminTodoSeverity.Danger,
                TitleKey: "AdminTodo.KvkFailed.Title",
                Subtitle: r.EstablishmentName,
                Area: "Organisaties",
                SinceUtc: r.CreatedAt,
                ActionLabelKey: "AdminTodo.KvkFailed.Action",
                // 04 builds /admin/organisaties/aanvragen?filter=kvk; until then companies list filtered.
                Href: "/admin/organisaties?kvk=failed",
                Count: 1));
        }

        var fallbackSince = DateTime.UtcNow;
        foreach (var c in companies)
        {
            items.Add(new AdminTodoItem(
                Key: $"{Key}:co:{c.Id}",
                Severity: AdminTodoSeverity.Danger,
                TitleKey: "AdminTodo.KvkFailed.Title",
                Subtitle: c.Name,
                Area: "Organisaties",
                SinceUtc: c.Since ?? fallbackSince,
                ActionLabelKey: "AdminTodo.KvkFailed.Action",
                Href: "/admin/organisaties?kvk=failed",
                Count: 1));
        }

        return items;
    }
}
