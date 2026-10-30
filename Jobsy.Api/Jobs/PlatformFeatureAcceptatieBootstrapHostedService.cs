using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Features;
using Jobsy.Infrastructure.Hosting;
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
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                await AcceptatiePlatformFeatureEnsurer.EnsureAcceptatieFeatureFlagsAsync(
                    db,
                    configuration,
                    logger,
                    cancellationToken);

                scope.ServiceProvider.GetService<FeatureFlags>()?.Invalidate();

                var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
                var snap = await features.GetAsync(cancellationToken);
                if (!snap.SensitiveExperienceEnabled
                    || !snap.TrainingPartnersEnabled
                    || !snap.AfterMatchEnabled)
                {
                    await features.UpdateAsync(
                        new PlatformFeatureUpdate(
                            SensitiveExperienceEnabled: true,
                            TrainingPartnersEnabled: true,
                            AfterMatchEnabled: true),
                        cancellationToken);
                    scope.ServiceProvider.GetService<FeatureFlags>()?.Invalidate();
                    logger.LogInformation(
                        "Acceptatie platform feature bootstrap: SensitiveExperience, TrainingPartners and AfterMatch set ON (admin override).");
                }

                return;
            }
            catch (Exception ex) when (attempt < 3 && ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Acceptatie platform feature bootstrap attempt {Attempt}/3 failed; retrying.",
                    attempt);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Acceptatie platform feature bootstrap skipped after retries.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
