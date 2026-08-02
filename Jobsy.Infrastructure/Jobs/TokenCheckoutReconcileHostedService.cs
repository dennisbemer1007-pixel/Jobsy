using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Reprocesses paid/partially-fulfilled token checkouts when the Mollie webhook or redirect
/// failed mid-flight (tokens/invoice/commission missing). Safe: fulfillment is idempotent.
/// </summary>
public sealed class TokenCheckoutReconcileHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MinAge = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCheckoutReconcileHostedService> _logger;

    public TokenCheckoutReconcileHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TokenCheckoutReconcileHostedService> logger)
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
                        "Token checkout reconciler repaired {Count} checkout(s).", repaired);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Token checkout reconciler failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task<int> ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var fulfillment = scope.ServiceProvider.GetRequiredService<ITokenPurchaseFulfillmentService>();

        var cutoff = DateTime.UtcNow - MinAge;
        var candidates = await db.TokenPurchaseCheckouts.AsNoTracking()
            .Where(c =>
                c.CreatedAt <= cutoff
                && (c.Status == TokenPurchaseCheckoutStatus.Paid
                    || (c.Status == TokenPurchaseCheckoutStatus.Credited
                        && (c.TokenTransactionId == null || c.TokenPurchaseInvoiceId == null))))
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.Id)
            .Take(25)
            .ToListAsync(cancellationToken);

        var repaired = 0;
        foreach (var checkoutId in candidates)
        {
            try
            {
                var result = await fulfillment.TryFulfillPaidCheckoutAsync(
                    checkoutId,
                    actorUserId: null,
                    allowDevStubMarkPaid: false,
                    cancellationToken);
                if (result is not null)
                {
                    repaired++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Token checkout reconciler skipped checkout {CheckoutId}",
                    checkoutId);
            }
        }

        return repaired;
    }
}
