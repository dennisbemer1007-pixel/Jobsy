using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Api.Jobs;

/// <summary>
/// Runs EF migrate + seed after the host starts accepting HTTP.
/// BackgroundService does not block Kestrel listen / Render health checks.
/// Migration failures stop the host; non-fatal seed errors are logged so the API stays up.
/// </summary>
public sealed class DatabaseSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DatabaseSeedHostedService> _logger;

    public DatabaseSeedHostedService(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DatabaseSeedHostedService> logger)
    {
        _services = services;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await JobsyDbSeeder.MigrateAsync(_services);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Database migrate failed during startup.");
            throw;
        }

        var allowSeed = _environment.IsDevelopment()
                        || _configuration.GetValue("Seed:Enabled", false)
                        || _configuration.GetValue("JobsyAuth:AllowDevelopmentAuth", false);
        if (!allowSeed)
        {
            _logger.LogInformation(
                "Skipping database seed (requires Development, Seed:Enabled, or JobsyAuth:AllowDevelopmentAuth).");
            return;
        }

        try
        {
            await JobsyDbSeeder.SeedDataAsync(_services);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Keep API available (salesmanager endpoints, auth, etc.) even if demo seed flakes.
            _logger.LogError(ex, "Database seed failed during startup; API continues without full seed.");
        }
    }
}
