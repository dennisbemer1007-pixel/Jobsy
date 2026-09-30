using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Reconciles pending deep-test Mollie checkouts and completes paid rows missing invoice/receipt.
/// </summary>
public sealed class DeepTestCheckoutReconcileHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MinAge = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MaxPendingAge = TimeSpan.FromHours(48);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeepTestCheckoutReconcileHostedService> _logger;

    public DeepTestCheckoutReconcileHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeepTestCheckoutReconcileHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var repaired = await ReconcileOnceAsync(stoppingToken);
                if (repaired > 0)
                {
                    _logger.LogInformation(
                        "Deep-test checkout reconciler repaired {Count} checkout(s).", repaired);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Deep-test checkout reconciler failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task<int> ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var payments = scope.ServiceProvider.GetRequiredService<IDeepTestPaymentService>();

        var now = DateTime.UtcNow;
        var cutoff = now - MinAge;
        var expireBefore = now - MaxPendingAge;

        var agedPending = await db.DeepAnalysisCheckouts
            .Where(c => c.Status == DeepAnalysisCheckoutStatus.Pending
                        && !c.IsStub
                        && c.CreatedAtUtc <= expireBefore)
            .Take(50)
            .ToListAsync(cancellationToken);
        foreach (var row in agedPending)
        {
            row.Status = DeepAnalysisCheckoutStatus.Expired;
            row.FailedAtUtc = now;
        }

        if (agedPending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var candidates = await db.DeepAnalysisCheckouts.AsNoTracking()
            .Where(c =>
                c.CreatedAtUtc <= cutoff
                && ((c.Status == DeepAnalysisCheckoutStatus.Pending && !c.IsStub)
                    || (c.Status == DeepAnalysisCheckoutStatus.Paid
                        && (c.InvoiceId == null || c.ReceiptSentAtUtc == null))))
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => c.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        var repaired = agedPending.Count;
        foreach (var checkoutId in candidates)
        {
            try
            {
                var result = await payments.TryFulfillAsync(
                    checkoutId,
                    DeepTestFulfillSource.Reconcile,
                    cancellationToken);
                if (result.Changed)
                {
                    repaired++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Deep-test checkout reconciler skipped checkout {CheckoutId}",
                    checkoutId);
            }
        }

        return repaired;
    }
}
