using System.Globalization;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.AdminTodo;

/// <summary>
/// Open salesmanager self-billing invoices (Issued — the ones mark-paid acts on).
/// Admin has no separate payout-approve step; me/payouts/checkout is self-service.
/// </summary>
public sealed class OpenPayoutsSource : IAdminTodoSource
{
    private readonly JobsyDbContext _db;

    public OpenPayoutsSource(JobsyDbContext db) => _db = db;

    public string Key => "open-payouts";

    public async Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var open = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.Status == SelfBillingInvoiceStatus.Issued)
            .Select(i => new { i.Id, i.TotalInclVat, i.CreatedAt, i.IssuedAt })
            .ToListAsync(cancellationToken);

        if (open.Count == 0)
        {
            return [];
        }

        var sum = open.Sum(i => i.TotalInclVat);
        var since = open.Min(i => i.IssuedAt ?? i.CreatedAt);
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var sumText = sum.ToString("C", culture);

        return
        [
            new AdminTodoItem(
                Key: Key,
                Severity: AdminTodoSeverity.Warn,
                TitleKey: "AdminTodo.OpenPayouts.Title",
                Subtitle: string.Format(culture, "{0} facturen · {1}", open.Count, sumText),
                Area: "Financiën",
                SinceUtc: since,
                ActionLabelKey: "AdminTodo.OpenPayouts.Action",
                Href: "/admin/financien/uitbetalingen?tab=uitbetalingen",
                Count: open.Count)
        ];
    }
}
