using Jobsy.Core.Interfaces;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Drops mail to real recipients while <see cref="TestAccountScope"/> is active.
/// Mail to test-* addresses is allowed (D5).
/// </summary>
public sealed class TestAccountMailGuard : IEmailService
{
    private readonly IEmailService _inner;
    private readonly ILogger<TestAccountMailGuard> _logger;
    private readonly string _emailDomain;

    public TestAccountMailGuard(
        IEmailService inner,
        ILogger<TestAccountMailGuard> logger,
        string emailDomain = "lobsy.nl")
    {
        _inner = inner;
        _logger = logger;
        _emailDomain = emailDomain;
    }

    public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (TestAccountScope.IsActive && !IsTestAddress(message.To))
        {
            _logger.LogInformation(
                "mail.dropped.test_boundary template={Template} to={To}",
                message.Category ?? "(none)",
                EmailServiceStub.RedactEmail(message.To));
            return Task.FromResult(EmailDeliveryResult.Stub);
        }

        return _inner.SendAsync(message, cancellationToken);
    }

    private bool IsTestAddress(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0)
        {
            return false;
        }

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        return local.StartsWith("test-", StringComparison.OrdinalIgnoreCase)
               && domain.Equals(_emailDomain, StringComparison.OrdinalIgnoreCase);
    }
}
