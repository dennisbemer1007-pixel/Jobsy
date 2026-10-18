using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Jobs;

/// <summary>
/// Ensures Acceptatie has critical feature flags ON via DB admin overrides (not Render env sync).
/// Production is never touched.
/// </summary>
public sealed class PlatformFeatureAcceptatieBootstrapHostedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<PlatformFeatureAcceptatieBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var deployment = configuration["Lobsy:DeploymentEnvironment"];
        if (!string.Equals(deployment, "Acceptatie", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
            var snap = await features.GetAsync(cancellationToken);
            if (snap.SensitiveExperienceEnabled && snap.TrainingPartnersEnabled)
            {
                return;
            }

            await features.UpdateAsync(
                new PlatformFeatureUpdate(
                    SensitiveExperienceEnabled: true,
                    TrainingPartnersEnabled: true),
                cancellationToken);
            logger.LogInformation(
                "Acceptatie platform feature bootstrap: SensitiveExperienceEnabled and TrainingPartnersEnabled set ON (admin override).");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Acceptatie platform feature bootstrap skipped (database may not be ready yet).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
