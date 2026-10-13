using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services.CandidateExternalVacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Sends at most one reminder per external-vacancy outbound after
/// <see cref="CandidateExternalVacancyRules.ReminderAfterDays"/> if the employer has not clicked the invite.
/// </summary>
public sealed class ExternalVacancyReminderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExternalVacancyReminderHostedService> _logger;

    public ExternalVacancyReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExternalVacancyReminderHostedService> logger)
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
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "External vacancy reminder job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var flags = scope.ServiceProvider.GetRequiredService<IFeatureFlags>();
        if (!await flags.IsEnabledAsync(PlatformFeature.CandidateExternalVacancies, cancellationToken))
        {
            return;
        }

        var reminders = scope.ServiceProvider.GetRequiredService<IExternalVacancyReminderService>();
        var sent = await reminders.SendDueRemindersAsync(cancellationToken);
        if (sent > 0)
        {
            _logger.LogInformation("External vacancy reminders sent: {Count}", sent);
        }
    }
}
