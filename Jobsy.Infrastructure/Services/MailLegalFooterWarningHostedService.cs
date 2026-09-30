using Jobsy.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Logs a Production warning when Mail legal footer fields are empty (Dennis must provide them).
/// </summary>
public sealed class MailLegalFooterWarningHostedService : IHostedService
{
    private readonly IOptions<MailOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MailLegalFooterWarningHostedService> _logger;

    public MailLegalFooterWarningHostedService(
        IOptions<MailOptions> options,
        IHostEnvironment environment,
        ILogger<MailLegalFooterWarningHostedService> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        var mail = _options.Value;
        if (mail.MissingLegalFooter)
        {
            _logger.LogWarning(
                "E-mailfooter mist adres en/of KvK-nummer (Mail:LegalAddress, Mail:KvkNumber). Dennis moet deze nog aanleveren.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
