using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Fills mail footer fields from <see cref="ILegalIdentity"/> when Mail:* is empty (Dependency B).
/// </summary>
public sealed class MailOptionsLegalIdentityPostConfigure : IPostConfigureOptions<MailOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MailOptionsLegalIdentityPostConfigure(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void PostConfigure(string? name, MailOptions options)
    {
        using var scope = _scopeFactory.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<ILegalIdentity>();
        var snap = identity.GetAsync(default).GetAwaiter().GetResult();

        if (string.IsNullOrWhiteSpace(options.LegalName)
            || string.Equals(options.LegalName.Trim(), MailOptions.DefaultLegalName, StringComparison.Ordinal))
        {
            options.LegalName = snap.DisplayName;
        }

        if (string.IsNullOrWhiteSpace(options.LegalAddress) && snap.AddressLine is not null)
        {
            options.LegalAddress = snap.AddressLine;
        }

        if (string.IsNullOrWhiteSpace(options.KvkNumber) && snap.KvkNumber is not null)
        {
            options.KvkNumber = snap.KvkNumber;
        }

        if (string.IsNullOrWhiteSpace(options.SupportAddress))
        {
            options.SupportAddress = snap.SupportEmail;
        }
    }
}
