using Jobsy.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Resolves VAPID keys from configuration. In Development, generates an in-memory pair when unset.
/// In Production, never generates temporary keys (would break all subscriptions on restart).
/// </summary>
public sealed class WebPushVapidKeyProvider : IHostedService
{
    private readonly object _gate = new();
    private readonly IOptionsMonitor<WebPushOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<WebPushVapidKeyProvider> _logger;
    private string? _publicKey;
    private string? _privateKey;
    private string? _subject;
    private bool _disabled;

    public WebPushVapidKeyProvider(
        IOptionsMonitor<WebPushOptions> options,
        IHostEnvironment environment,
        ILogger<WebPushVapidKeyProvider> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public bool IsEnabled
    {
        get
        {
            _ = TryGetKeys(out _);
            return !_disabled && _publicKey is not null;
        }
    }

    public (string Subject, string PublicKey, string PrivateKey) GetKeys()
    {
        if (!TryGetKeys(out var keys) || keys is null)
        {
            throw new InvalidOperationException(
                "Web Push is disabled: configure WebPush__Subject, WebPush__PublicKey and WebPush__PrivateKey.");
        }

        return keys.Value;
    }

    public string PublicKey => GetKeys().PublicKey;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = TryGetKeys(out _);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool TryGetKeys(out (string Subject, string PublicKey, string PrivateKey)? keys)
    {
        lock (_gate)
        {
            var opts = _options.CurrentValue;
            var subject = string.IsNullOrWhiteSpace(opts.Subject) ? "" : opts.Subject.Trim();
            if (!string.IsNullOrWhiteSpace(opts.PublicKey)
                && !string.IsNullOrWhiteSpace(opts.PrivateKey)
                && !string.IsNullOrWhiteSpace(subject))
            {
                _publicKey = opts.PublicKey.Trim();
                _privateKey = opts.PrivateKey.Trim();
                _subject = subject;
                _disabled = false;
                keys = (_subject, _publicKey, _privateKey);
                return true;
            }

            if (_environment.IsDevelopment())
            {
                if (_publicKey is not null && _privateKey is not null && _subject is not null)
                {
                    keys = (_subject, _publicKey, _privateKey);
                    return true;
                }

                var generated = VapidHelper.GenerateVapidKeys();
                _publicKey = generated.PublicKey;
                _privateKey = generated.PrivateKey;
                _subject = string.IsNullOrWhiteSpace(subject) ? "mailto:support@lobsy.nl" : subject;
                _disabled = false;
                _logger.LogWarning(
                    "WebPush VAPID keys were not configured; generated ephemeral keys for Development. PublicKey={PublicKey}",
                    _publicKey);
                keys = (_subject, _publicKey, _privateKey);
                return true;
            }

            if (!_disabled)
            {
                _logger.LogError(
                    "Web Push disabled: set WebPush__Subject, WebPush__PublicKey and WebPush__PrivateKey in the environment. " +
                    "Temporary VAPID keys are not generated outside Development (redeploys would invalidate all subscriptions).");
            }

            _disabled = true;
            _publicKey = null;
            _privateKey = null;
            _subject = null;
            keys = null;
            return false;
        }
    }
}
