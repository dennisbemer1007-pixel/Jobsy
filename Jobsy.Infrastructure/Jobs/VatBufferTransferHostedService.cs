using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Processes pending BTW-buffer transfer orders toward the configured Knab BTW IBAN.
/// Omschrijving/kenmerk is always the related token-purchase invoice number.
/// </summary>
public sealed class VatBufferTransferHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VatBufferTransferHostedService> _logger;

    public VatBufferTransferHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<VatBufferTransferHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var transfers = scope.ServiceProvider.GetRequiredService<IVatBufferTransferService>();
                var count = await transfers.ProcessPendingAsync(stoppingToken);
                if (count > 0)
                {
                    _logger.LogInformation("Processed {Count} pending BTW-buffer transfer(s).", count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "BTW-buffer transfer job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
