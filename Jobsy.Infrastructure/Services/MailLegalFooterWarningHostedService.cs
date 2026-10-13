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

        MailOptions mail;
        try
        {
            mail = _options.Value;
        }
        catch (Exception ex)
        {
            // MailOptions post-configure reads the legal identity from the database. This service
            // starts before DatabaseSeedHostedService has migrated a fresh database, so a missing
            // table must not crash the API; the options are not cached on failure and resolve
            // normally once the database is ready.
            _logger.LogWarning(ex, "E-mailfooter kon bij het opstarten niet worden gecontroleerd.");
            return Task.CompletedTask;
        }

        if (mail.MissingLegalFooter)
        {
            _logger.LogWarning(
                "E-mailfooter mist adres en/of KvK-nummer. Vul Bedrijfsgegevens in, of zet Mail:LegalAddress en Mail:KvkNumber.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
