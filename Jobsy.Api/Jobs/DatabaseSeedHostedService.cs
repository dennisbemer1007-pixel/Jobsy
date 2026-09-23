using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var wipeInsteadOfSeed = JobsyDbSeeder.PreferWipeOverSeed(_configuration);
        var allowSeed = !wipeInsteadOfSeed
                        && (_environment.IsDevelopment()
                            || _configuration.GetValue("Seed:Enabled", false));
        _logger.LogInformation(
            "Startup data path: wipe={Wipe} seed={Seed} service={Service} publicWeb={PublicWeb} seedEnabled={SeedEnabled}",
            wipeInsteadOfSeed,
            allowSeed,
            _configuration["RENDER_SERVICE_NAME"],
            _configuration["PublicWebBaseUrl"],
            _configuration.GetValue("Seed:Enabled", false));
        if (allowSeed)
        {
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
        else
        {
            try
            {
                await JobsyDbSeeder.PurgeDemoDataAsync(_services, _configuration);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Operational wipe failed during startup; API continues.");
            }
        }

        try
        {
            var index = _services.GetRequiredService<IVacancyDiscoveryIndex>();
            index.Invalidate();
            await index.RefreshAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Banenkaart index refresh after seed failed; the 15s job will retry.");
        }
    }
}
