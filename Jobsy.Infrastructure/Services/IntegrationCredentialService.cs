using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class IntegrationCredentialService : IIntegrationCredentialService
{
    private static readonly IntegrationKey[] ConfigurableKeys = Enum.GetValues<IntegrationKey>();
    public static readonly TimeSpan SecretsCacheTtl = TimeSpan.FromMinutes(10);

    private readonly JobsyDbContext _db;
    private readonly ISecretProtector _secrets;
    private readonly MailOptions _mailOptions;
    private readonly KvkOptions _kvkOptions;
    private readonly IMemoryCache? _cache;

    public IntegrationCredentialService(JobsyDbContext db, ISecretProtector secrets)
        : this(db, secrets, Options.Create(new MailOptions()), Options.Create(new KvkOptions()), cache: null)
    {
    }

    public IntegrationCredentialService(
        JobsyDbContext db,
        ISecretProtector secrets,
        IOptions<MailOptions> mailOptions)
        : this(db, secrets, mailOptions, Options.Create(new KvkOptions()), cache: null)
    {
    }

    public IntegrationCredentialService(
        JobsyDbContext db,
        ISecretProtector secrets,
        IOptions<MailOptions> mailOptions,
        IOptions<KvkOptions> kvkOptions)
        : this(db, secrets, mailOptions, kvkOptions, cache: null)
    {
    }

    public IntegrationCredentialService(
        JobsyDbContext db,
        ISecretProtector secrets,
        IOptions<MailOptions> mailOptions,
        IOptions<KvkOptions> kvkOptions,
        IMemoryCache? cache)
    {
        _db = db;
        _secrets = secrets;
        _mailOptions = mailOptions.Value ?? new MailOptions();
        _kvkOptions = kvkOptions.Value ?? new KvkOptions();
        _cache = cache;
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
            if (key == IntegrationKey.Mail)
            {
                // Admin "Secrets wissen" must actually disable Resend, including env bootstrap.
                row.FromAddress = null;
                row.IgnoreEnvironmentCredentials = true;
            }
        }
        else if (!string.IsNullOrWhiteSpace(update.ApiKey))
        {
            row.ApiKey = _secrets.Protect(update.ApiKey.Trim());
            if (key == IntegrationKey.Mail)
            {
                row.IgnoreEnvironmentCredentials = false;
            }
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
            var ok = key == IntegrationKey.Mail
                ? IntegrationEndpointUrl.TryNormalizeSmtpHost(update.BaseUrl, out var normalized, out var error)
                : IntegrationEndpointUrl.TryNormalizeBaseUrl(update.BaseUrl, out normalized, out error);
            if (!ok)
            {
                throw new InvalidOperationException(error ?? "Ongeldige Base URL.");
            }

            row.BaseUrl = normalized;
        }

        if (update.FromAddress is not null && !update.ClearApiKey)
        {
            row.FromAddress = string.IsNullOrWhiteSpace(update.FromAddress) ? null : update.FromAddress.Trim();
        }

        if (key == IntegrationKey.Mail && update.UseEnvironmentCredentials)
        {
            row.IgnoreEnvironmentCredentials = false;
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
            || update.BaseUrl is not null
            || update.UseEnvironmentCredentials)
        {
            row.LastPingOk = null;
            row.LastPingMessage = "Opgeslagen — nog niet getest.";
            row.LastPingAtUtc = null;
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _cache?.Remove("integration-secrets:" + key);
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
        var cacheKey = "integration-secrets:" + key;
        if (_cache is not null
            && _cache.TryGetValue(cacheKey, out IntegrationCredentialSecrets? cached))
        {
            return cached;
        }

        var row = await _db.IntegrationCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);

        string? apiKey = null;
        string? clientId = null;
        string? clientSecret = null;
        string? tenantId = null;
        string? model = null;
        string? baseUrl = null;
        string? fromAddress = null;

        if (row is not null)
        {
            apiKey = string.IsNullOrWhiteSpace(row.ApiKey) ? null : _secrets.Unprotect(row.ApiKey);
            clientId = string.IsNullOrWhiteSpace(row.ClientId) ? null : row.ClientId.Trim();
            clientSecret = string.IsNullOrWhiteSpace(row.ClientSecret) ? null : _secrets.Unprotect(row.ClientSecret);
            tenantId = string.IsNullOrWhiteSpace(row.TenantId) ? null : row.TenantId.Trim();
            model = string.IsNullOrWhiteSpace(row.Model) ? null : row.Model.Trim();
            baseUrl = string.IsNullOrWhiteSpace(row.BaseUrl) ? null : row.BaseUrl.Trim();
            fromAddress = string.IsNullOrWhiteSpace(row.FromAddress) ? null : row.FromAddress.Trim();
        }

        if (key == IntegrationKey.Mail && row?.IgnoreEnvironmentCredentials != true)
        {
            // Env/config fills gaps so Render can wire Resend without Admin first.
            apiKey ??= TrimOrNull(_mailOptions.ResendApiKey);
            fromAddress ??= TrimOrNull(_mailOptions.FromAddress);
        }

        if (key == IntegrationKey.Kvk)
        {
            apiKey ??= TrimOrNull(_kvkOptions.ApiKey);
            baseUrl ??= TrimOrNull(_kvkOptions.BaseUrl);
        }

        if (apiKey is null && clientId is null && clientSecret is null && tenantId is null
            && model is null && baseUrl is null && fromAddress is null)
        {
            _cache?.Set(cacheKey, (IntegrationCredentialSecrets?)null, SecretsCacheTtl);
            return null;
        }

        var secrets = new IntegrationCredentialSecrets(
            apiKey,
            clientId,
            clientSecret,
            tenantId,
            model,
            baseUrl,
            fromAddress);
        _cache?.Set(cacheKey, secrets, SecretsCacheTtl);
        return secrets;
    }

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
        IntegrationKey.Mail => "Mail (Resend)",
        _ => key.ToString()
    };

    public static string Description(IntegrationKey key) => key switch
    {
        IntegrationKey.OpenAI => "Vacaturemoderatie via OpenAI.",
        IntegrationKey.Mollie => "Token-betalingen / checkout.",
        IntegrationKey.Kvk => "KvK-handelsregister (live API bij key, anders demo-stub).",
        IntegrationKey.MicrosoftEntra => "Microsoft-login (OIDC).",
        IntegrationKey.GoogleEntra => "Google-login (OAuth).",
        IntegrationKey.Mail => "Uitgaande e-mail via Resend API (SMTP alleen als fallback).",
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
        var fromAddress = string.IsNullOrWhiteSpace(row?.FromAddress) ? null : row!.FromAddress.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(row?.BaseUrl) ? null : row!.BaseUrl.Trim();
        var ignoresEnv = key == IntegrationKey.Mail && row?.IgnoreEnvironmentCredentials == true;
        var usedEnvKey = false;
        var usedEnvFrom = false;

        if (key == IntegrationKey.Mail && !ignoresEnv)
        {
            if (apiKeyPlain is null)
            {
                var envKey = TrimOrNull(_mailOptions.ResendApiKey);
                if (envKey is not null)
                {
                    apiKeyPlain = envKey;
                    usedEnvKey = true;
                }
            }

            if (fromAddress is null)
            {
                var envFrom = TrimOrNull(_mailOptions.FromAddress);
                if (envFrom is not null)
                {
                    fromAddress = envFrom;
                    usedEnvFrom = true;
                }
            }
        }

        if (key == IntegrationKey.Kvk)
        {
            if (apiKeyPlain is null)
            {
                var envKey = TrimOrNull(_kvkOptions.ApiKey);
                if (envKey is not null)
                {
                    apiKeyPlain = envKey;
                    usedEnvKey = true;
                }
            }

            if (baseUrl is null)
            {
                var envBase = TrimOrNull(_kvkOptions.BaseUrl);
                if (envBase is not null)
                {
                    baseUrl = envBase;
                    usedEnvFrom = true;
                }
            }
        }

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
            baseUrl,
            fromAddress,
            SupportsApiKey(key),
            SupportsModel(key),
            SupportsOAuth(key),
            SupportsTenantId(key),
            SupportsBaseUrl(key),
            SupportsFromAddress(key),
            row?.LastPingOk,
            row?.LastPingMessage,
            row?.LastPingAtUtc,
            row?.UpdatedAtUtc,
            ignoresEnv,
            usedEnvKey || usedEnvFrom);
    }
}
