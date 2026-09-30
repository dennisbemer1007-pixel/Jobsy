using Jobsy.Core.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Shared helper: skip a background-job tick when Employers is OFF.
/// Logs one info line per state change (paused ↔ running). No catch-up when resumed.
/// </summary>
public sealed class EmployersJobGate
{
    private readonly ILogger _logger;
    private readonly string _jobName;
    private bool? _lastEnabled;

    public EmployersJobGate(ILogger logger, string jobName)
    {
        _logger = logger;
        _jobName = jobName;
    }

    public async ValueTask<bool> ShouldRunAsync(
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await ShouldRunAsync(scope.ServiceProvider, cancellationToken);
    }

    public async ValueTask<bool> ShouldRunAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var flags = services.GetRequiredService<IFeatureFlags>();
        var enabled = await flags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
        if (_lastEnabled != enabled)
        {
            if (enabled)
            {
                _logger.LogInformation("{Job} resumed: employers feature is ON.", _jobName);
            }
            else
            {
                _logger.LogInformation(
                    "{Job} paused: employers feature is OFF (tick skipped, no catch-up).",
                    _jobName);
            }

            _lastEnabled = enabled;
        }

        return enabled;
    }
}
