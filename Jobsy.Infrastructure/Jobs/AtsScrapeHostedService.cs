using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Daily poll of enabled ATS whitelist sources.</summary>
public sealed class AtsScrapeHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AtsScrapeHostedService> _logger;

    public AtsScrapeHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<AtsScrapeHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(8), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var scrape = scope.ServiceProvider.GetRequiredService<IAtsScrapeService>();
                var report = await scrape.ScrapeAllEnabledAsync(stoppingToken);
                _logger.LogInformation(
                    "ATS scrape cycle finished; upserted={Count} inserted={Inserted} httpErrors={HttpErrors}.",
                    report.Upserted, report.Inserted, report.HttpErrors);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ATS scrape cycle failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
