using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Resolves VAPID keys from configuration, or generates an in-memory pair for local/demo.
/// </summary>
public sealed class WebPushVapidKeyProvider
{
    private readonly object _gate = new();
    private readonly IOptionsMonitor<WebPushOptions> _options;
    private readonly ILogger<WebPushVapidKeyProvider> _logger;
    private string? _publicKey;
    private string? _privateKey;
    private string? _subject;

    public WebPushVapidKeyProvider(
        IOptionsMonitor<WebPushOptions> options,
        ILogger<WebPushVapidKeyProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public (string Subject, string PublicKey, string PrivateKey) GetKeys()
    {
        lock (_gate)
        {
            var opts = _options.CurrentValue;
            var subject = string.IsNullOrWhiteSpace(opts.Subject) ? "mailto:support@lobsy.nl" : opts.Subject.Trim();
            if (!string.IsNullOrWhiteSpace(opts.PublicKey) && !string.IsNullOrWhiteSpace(opts.PrivateKey))
            {
                return (subject, opts.PublicKey.Trim(), opts.PrivateKey.Trim());
            }

            if (_publicKey is not null && _privateKey is not null && _subject is not null)
            {
                return (_subject, _publicKey, _privateKey);
            }

            var keys = VapidHelper.GenerateVapidKeys();
            _publicKey = keys.PublicKey;
            _privateKey = keys.PrivateKey;
            _subject = subject;
            _logger.LogWarning(
                "WebPush VAPID keys were not configured; generated ephemeral keys for this process. PublicKey={PublicKey}",
                _publicKey);
            return (_subject, _publicKey, _privateKey);
        }
    }

    public string PublicKey => GetKeys().PublicKey;
}
