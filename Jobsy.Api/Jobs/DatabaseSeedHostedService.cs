using Jobsy.Infrastructure.Data;

namespace Jobsy.Api.Jobs;

/// <summary>
/// Runs EF migrate + seed after the host starts accepting HTTP.
/// BackgroundService does not block Kestrel listen / Render health checks.
/// </summary>
public sealed class DatabaseSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DatabaseSeedHostedService> _logger;

    public DatabaseSeedHostedService(
        IServiceProvider services,
        ILogger<DatabaseSeedHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await JobsyDbSeeder.SeedAsync(_services);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Database migrate/seed failed during startup.");
            throw;
        }
    }
}
