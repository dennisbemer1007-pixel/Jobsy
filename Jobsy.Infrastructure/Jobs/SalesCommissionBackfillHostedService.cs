using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Idempotent one-shot: activate CommissionStartsAtUtc + year-2/3 snapshots for legacy attributed orgs.
/// Does not recompute ledger lines.
/// </summary>
public sealed class SalesCommissionBackfillHostedService : BackgroundService
{
    public const string MarkerCategory = "sales.commission.backfill";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SalesCommissionBackfillHostedService> _logger;

    public SalesCommissionBackfillHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SalesCommissionBackfillHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        try
        {
            await RunAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sales commission backfill failed.");
        }
    }

    public static async Task<SalesCommissionBackfillSummary> RunOnceAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var already = await db.PlatformLogs.AsNoTracking()
            .AnyAsync(l => l.Category == MarkerCategory, cancellationToken);
        if (already)
        {
            return new SalesCommissionBackfillSummary(0, 0, 0, Skipped: true);
        }

        var settings = await db.SalesCommercialSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var year2 = settings?.Year2DirectCommissionRate
                    ?? SalesCommissionRules.DefaultYear2DirectCommissionRate;
        var year3 = settings?.Year3DirectCommissionRate
                    ?? SalesCommissionRules.DefaultYear3DirectCommissionRate;

        var attributed = await db.Companies
            .Where(c => c.ReferredBySalesManagerUserId != null || c.ReferredByAmbassadeurUserId != null)
            .Select(c => new
            {
                c.Id,
                c.ParentCompanyId,
                c.CommissionStartsAtUtc,
                c.CommissionDirectRateSnapshot,
                c.CommissionIndirectRateSnapshot,
                c.CommissionDurationDaysSnapshot,
                c.CommissionYear2RateSnapshot,
                c.CommissionYear3RateSnapshot,
                c.CommissionTermsSnapshottedAtUtc,
                c.CommissionIndirectSalesManagerUserId
            })
            .ToListAsync(cancellationToken);

        var byRoot = attributed
            .GroupBy(c => SalesCommercialUnit.RootIdOf(c.Id, c.ParentCompanyId))
            .ToList();

        var activated = 0;
        var withoutPurchases = 0;
        var vestigingMismatch = 0;

        foreach (var group in byRoot)
        {
            var rootId = group.Key;
            var root = await db.Companies.FirstOrDefaultAsync(c => c.Id == rootId, cancellationToken);
            if (root is null)
            {
                continue;
            }

            var memberIds = group.Select(g => g.Id).Append(rootId).Distinct().ToList();
            var firstPaid = await db.TokenPurchaseCheckouts.AsNoTracking()
                .Where(c => memberIds.Contains(c.CompanyId)
                            && c.Status == TokenPurchaseCheckoutStatus.Credited)
                .OrderBy(c => c.CreditedAt ?? c.CreatedAt)
                .Select(c => (DateTime?)(c.CreditedAt ?? c.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);

            if (firstPaid is null)
            {
                withoutPurchases++;
                continue;
            }

            if (root.CommissionStartsAtUtc is null)
            {
                root.CommissionStartsAtUtc = firstPaid.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(firstPaid.Value, DateTimeKind.Utc)
                    : firstPaid.Value.ToUniversalTime();
                activated++;
            }

            // Keep existing direct/indirect/duration; fill year 2/3 from current settings.
            root.CommissionYear2RateSnapshot ??= year2;
            root.CommissionYear3RateSnapshot ??= year3;

            foreach (var vestiging in group.Where(g => g.Id != rootId))
            {
                if (vestiging.CommissionDirectRateSnapshot is decimal vDirect
                    && root.CommissionDirectRateSnapshot is decimal rDirect
                    && vDirect != rDirect)
                {
                    vestigingMismatch++;
                }
            }

            if (root.CommissionTermsSnapshottedAtUtc is null
                && root.CommissionDirectRateSnapshot is not null)
            {
                root.CommissionTermsSnapshottedAtUtc = root.CommissionStartsAtUtc;
            }
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = MarkerCategory,
            Message = "Sales commission window backfill completed",
            DetailsJson = JsonSerializer.Serialize(new
            {
                unitsActivated = activated,
                unitsWithoutPurchases = withoutPurchases,
                vestigingSnapshotMismatch = vestigingMismatch
            }),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return new SalesCommissionBackfillSummary(activated, withoutPurchases, vestigingMismatch, Skipped: false);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var summary = await RunOnceAsync(db, cancellationToken);
        if (!summary.Skipped)
        {
            _logger.LogInformation(
                "Sales commission backfill: activated={Activated}, withoutPurchases={Without}, vestigingMismatch={Mismatch}",
                summary.UnitsActivated, summary.UnitsWithoutPurchases, summary.VestigingSnapshotMismatch);
        }
    }
}

public sealed record SalesCommissionBackfillSummary(
    int UnitsActivated,
    int UnitsWithoutPurchases,
    int VestigingSnapshotMismatch,
    bool Skipped);
