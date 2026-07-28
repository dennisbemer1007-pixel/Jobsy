using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class IntegrationCredentialService : IIntegrationCredentialService
{
    private static readonly IntegrationKey[] ConfigurableKeys = Enum.GetValues<IntegrationKey>();

    private readonly JobsyDbContext _db;
    private readonly ISecretProtector _secrets;

    public IntegrationCredentialService(JobsyDbContext db, ISecretProtector secrets)
    {
        _db = db;
        _secrets = secrets;
    }

    public async Task<IntegrationCredentialView?> GetAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigurable(key))
        {
            return null;
        }

        var row = await _db.IntegrationCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        return ToView(key, row);
    }

    public async Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.IntegrationCredentials.AsNoTracking().ToListAsync(cancellationToken);
        return ConfigurableKeys
            .Select(key => ToView(key, rows.FirstOrDefault(r => r.Key == key)))
            .ToList();
    }

    public async Task<IntegrationCredentialView> UpsertAsync(
        IntegrationKey key,
        IntegrationCredentialUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigurable(key))
        {
            throw new InvalidOperationException($"Integratie '{key}' ondersteunt geen settings-tegel.");
        }

        var row = await _db.IntegrationCredentials
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        if (row is null)
        {
            row = new IntegrationCredential
            {
                Id = Guid.NewGuid(),
                Key = key
            };
            _db.IntegrationCredentials.Add(row);
        }

        if (update.ClearApiKey)
        {
            row.ApiKey = null;
        }
        else if (!string.IsNullOrWhiteSpace(update.ApiKey))
        {
            row.ApiKey = _secrets.Protect(update.ApiKey.Trim());
        }

        if (update.ClearClientSecret)
        {
            row.ClientSecret = null;
        }
        else if (!string.IsNullOrWhiteSpace(update.ClientSecret))
        {
            row.ClientSecret = _secrets.Protect(update.ClientSecret.Trim());
        }

        if (update.ClientId is not null)
        {
            row.ClientId = string.IsNullOrWhiteSpace(update.ClientId) ? null : update.ClientId.Trim();
        }

        if (update.TenantId is not null)
        {
            row.TenantId = string.IsNullOrWhiteSpace(update.TenantId) ? null : update.TenantId.Trim();
        }

        if (update.BaseUrl is not null)
        {
            if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(update.BaseUrl, out var normalized, out var error))
            {
                throw new InvalidOperationException(error ?? "Ongeldige Base URL.");
            }

            row.BaseUrl = normalized;
        }

        if (update.FromAddress is not null)
        {
            row.FromAddress = string.IsNullOrWhiteSpace(update.FromAddress) ? null : update.FromAddress.Trim();
        }

        if (SupportsModel(key))
        {
            if (!string.IsNullOrWhiteSpace(update.Model))
            {
                row.Model = update.Model.Trim();
            }
            else if (string.IsNullOrWhiteSpace(row.Model))
            {
                row.Model = "gpt-4o-mini";
            }
        }

        // New credentials invalidate previous ping until retested.
        if (update.ClearApiKey || update.ClearClientSecret
            || !string.IsNullOrWhiteSpace(update.ApiKey)
            || !string.IsNullOrWhiteSpace(update.ClientSecret)
            || update.ClientId is not null
            || update.TenantId is not null
            || update.BaseUrl is not null)
        {
            row.LastPingOk = null;
            row.LastPingMessage = "Opgeslagen — nog niet getest.";
            row.LastPingAtUtc = null;
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToView(key, row);
    }

    public async Task SavePingResultAsync(
        IntegrationKey key,
        bool ok,
        string message,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.IntegrationCredentials
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        if (row is null)
        {
            row = new IntegrationCredential
            {
                Id = Guid.NewGuid(),
                Key = key
            };
            _db.IntegrationCredentials.Add(row);
        }

        row.LastPingOk = ok;
        row.LastPingMessage = message.Length > 500 ? message[..500] : message;
        row.LastPingAtUtc = DateTime.UtcNow;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetRawApiKeyAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        var secrets = await GetSecretsAsync(key, cancellationToken);
        return secrets?.ApiKey;
    }

    public async Task<string?> GetModelAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        var secrets = await GetSecretsAsync(key, cancellationToken);
        return secrets?.Model;
    }

    public async Task<string?> GetBaseUrlAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        var secrets = await GetSecretsAsync(key, cancellationToken);
        return secrets?.BaseUrl;
    }

    public async Task<IntegrationCredentialSecrets?> GetSecretsAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.IntegrationCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return new IntegrationCredentialSecrets(
            string.IsNullOrWhiteSpace(row.ApiKey) ? null : _secrets.Unprotect(row.ApiKey),
            string.IsNullOrWhiteSpace(row.ClientId) ? null : row.ClientId.Trim(),
            string.IsNullOrWhiteSpace(row.ClientSecret) ? null : _secrets.Unprotect(row.ClientSecret),
            string.IsNullOrWhiteSpace(row.TenantId) ? null : row.TenantId.Trim(),
            string.IsNullOrWhiteSpace(row.Model) ? null : row.Model.Trim(),
            string.IsNullOrWhiteSpace(row.BaseUrl) ? null : row.BaseUrl.Trim(),
            string.IsNullOrWhiteSpace(row.FromAddress) ? null : row.FromAddress.Trim());
    }

    public static bool IsConfigurable(IntegrationKey key) => ConfigurableKeys.Contains(key);

    public static bool SupportsApiKey(IntegrationKey key) => key is
        IntegrationKey.Mollie or IntegrationKey.Kvk or IntegrationKey.Mail
        or IntegrationKey.OpenAI;

    public static bool SupportsModel(IntegrationKey key) => key == IntegrationKey.OpenAI;

    public static bool SupportsOAuth(IntegrationKey key) => key is
        IntegrationKey.MicrosoftEntra or IntegrationKey.GoogleEntra or IntegrationKey.Mail;

    public static bool SupportsTenantId(IntegrationKey key) => key == IntegrationKey.MicrosoftEntra;

    public static bool SupportsBaseUrl(IntegrationKey key) => key is
        IntegrationKey.OpenAI or IntegrationKey.Mollie or IntegrationKey.Kvk
        or IntegrationKey.Mail;

    public static bool SupportsFromAddress(IntegrationKey key) => key == IntegrationKey.Mail;

    public static string DisplayName(IntegrationKey key) => key switch
    {
        IntegrationKey.OpenAI => "OpenAI",
        IntegrationKey.Mollie => "Mollie",
        IntegrationKey.Kvk => "KVK",
        IntegrationKey.MicrosoftEntra => "Microsoft Entra",
        IntegrationKey.GoogleEntra => "Google",
        IntegrationKey.Mail => "Mail",
        _ => key.ToString()
    };

    public static string Description(IntegrationKey key) => key switch
    {
        IntegrationKey.OpenAI => "Vacaturemoderatie via OpenAI.",
        IntegrationKey.Mollie => "Token-betalingen / checkout.",
        IntegrationKey.Kvk => "KvK-handelsregister koppeling.",
        IntegrationKey.MicrosoftEntra => "Microsoft-login (OIDC).",
        IntegrationKey.GoogleEntra => "Google-login (OAuth).",
        IntegrationKey.Mail => "Uitgaande e-mail (SMTP/API).",
        _ => string.Empty
    };

    public static string? MaskSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var key = secret.Trim();
        if (key.Length <= 8)
        {
            return "••••••••";
        }

        return $"{key[..3]}••••••••{key[^4..]}";
    }

    private IntegrationCredentialView ToView(IntegrationKey key, IntegrationCredential? row)
    {
        var apiKeyPlain = string.IsNullOrWhiteSpace(row?.ApiKey) ? null : _secrets.Unprotect(row.ApiKey);
        var secretPlain = string.IsNullOrWhiteSpace(row?.ClientSecret) ? null : _secrets.Unprotect(row.ClientSecret);
        var hasKey = !string.IsNullOrWhiteSpace(apiKeyPlain);
        var hasSecret = !string.IsNullOrWhiteSpace(secretPlain);
        return new IntegrationCredentialView(
            key,
            DisplayName(key),
            Description(key),
            hasKey,
            hasKey ? MaskSecret(apiKeyPlain) : null,
            hasSecret,
            hasSecret ? MaskSecret(secretPlain) : null,
            row?.ClientId,
            row?.TenantId,
            SupportsModel(key) ? (row?.Model ?? "gpt-4o-mini") : row?.Model,
            row?.BaseUrl,
            row?.FromAddress,
            SupportsApiKey(key),
            SupportsModel(key),
            SupportsOAuth(key),
            SupportsTenantId(key),
            SupportsBaseUrl(key),
            SupportsFromAddress(key),
            row?.LastPingOk,
            row?.LastPingMessage,
            row?.LastPingAtUtc,
            row?.UpdatedAtUtc);
    }
}
