using System.Net;
using Jobsy.Core;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyApiKeyService : ICompanyApiKeyService
{
    public const int MaxNameLength = 128;
    public const string ExternalVacanciesPath = "/api/external/vacancies";

    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompanyApiKeyService> _logger;

    public CompanyApiKeyService(
        JobsyDbContext db,
        IEmailService email,
        IConfiguration configuration,
        ILogger<CompanyApiKeyService> logger)
    {
        _db = db;
        _email = email;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiKey?> FindActiveByPlaintextAsync(
        string plaintextKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey))
        {
            return null;
        }

        var hash = ApiKeyHasher.Hash(plaintextKey);
        return await _db.ApiKeys
            .AsNoTracking()
            .Include(k => k.Company)
            .FirstOrDefaultAsync(k => k.IsActive && k.ApiKeyHash == hash, cancellationToken);
    }

    public async Task TouchLastUsedAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);
        if (key is null)
        {
            return;
        }

        key.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyApiKeyView>> ListForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ApiKeys.AsNoTracking()
            .Where(k => k.CompanyId == companyId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new CompanyApiKeyView(
                k.Id,
                k.CompanyId,
                k.Name,
                k.KeyPrefix,
                k.IsActive,
                k.LastUsedAt,
                k.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminApiKeyView>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ApiKeys.AsNoTracking()
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new AdminApiKeyView(
                k.Id,
                k.CompanyId,
                k.Company.Name,
                k.Name,
                k.KeyPrefix,
                k.IsActive,
                k.LastUsedAt,
                k.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<GeneratedApiKeyResult> GenerateAsync(
        Guid companyId,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var label = NormalizeName(name);
        var plaintext = ApiKeyHasher.GeneratePlaintext();
        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ApiKeyHash = ApiKeyHasher.Hash(plaintext),
            Name = label,
            KeyPrefix = ApiKeyHasher.ToDisplayPrefix(plaintext),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await DeactivateActiveKeysAsync(companyId, cancellationToken);
        _db.ApiKeys.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new GeneratedApiKeyResult(
            entity.Id,
            entity.CompanyId,
            entity.Name,
            entity.KeyPrefix,
            plaintext,
            entity.CreatedAt);
    }

    public async Task<bool> DeactivateAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);
        if (key is null)
        {
            return false;
        }

        if (!key.IsActive)
        {
            return true;
        }

        key.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var keys = await _db.ApiKeys
            .Where(k => k.CompanyId == companyId && k.IsActive)
            .ToListAsync(cancellationToken);
        if (keys.Count == 0)
        {
            return false;
        }

        foreach (var key in keys)
        {
            key.IsActive = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<EmailApiKeyResult> EmailCredentialsAsync(
        Guid companyId,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var normalized = recipientEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Ongeldig e-mailadres.");
        }

        // Build the new key in memory first; only persist after e-mail succeeds so a mail
        // failure cannot leave the company without a recoverable active key.
        var plaintext = ApiKeyHasher.GeneratePlaintext();
        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ApiKeyHash = ApiKeyHasher.Hash(plaintext),
            Name = "API-koppeling (e-mail)",
            KeyPrefix = ApiKeyHasher.ToDisplayPrefix(plaintext),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var apiBase = ResolvePublicApiBaseUrl();
            var endpoint = apiBase + ExternalVacanciesPath;
            var swaggerUrl = apiBase + "/swagger";
            await _email.SendAsync(new EmailMessage(
                normalized,
                $"Lobsy API-credentials voor {company.Name}",
                $"""
                 <p>Hallo,</p>
                 <p>Hierbij de API-credentials voor <strong>{WebUtility.HtmlEncode(company.Name)}</strong>.</p>
                 <p><strong>Endpoint:</strong><br/>
                 <code>{WebUtility.HtmlEncode(endpoint)}</code></p>
                 <p><strong>Header:</strong><br/>
                 <code>{ApiKeyAuthDefaults.HeaderName}: &lt;jouw-api-key&gt;</code></p>
                 <p><strong>API-key:</strong><br/>
                 <code>{WebUtility.HtmlEncode(plaintext)}</code></p>
                 <p>Prefix (ter herkenning): <code>{WebUtility.HtmlEncode(entity.KeyPrefix)}</code></p>
                 <p>Bekijk request/response in Swagger:
                 <a href="{WebUtility.HtmlEncode(swaggerUrl)}">{WebUtility.HtmlEncode(swaggerUrl)}</a></p>
                 <p><strong>Let op:</strong> deze sleutel wordt slechts één keer getoond en
                 vervangt eventuele eerdere actieve keys. Bewaar hem veilig.</p>
                 """,
                "CompanyApiKeyCredentials"), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to e-mail API credentials for company {CompanyId}; key was not activated.",
                companyId);
            throw new InvalidOperationException(
                "Versturen van de API-credentials is mislukt. De bestaande key blijft actief.", ex);
        }

        await DeactivateActiveKeysAsync(companyId, cancellationToken);
        _db.ApiKeys.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new EmailApiKeyResult(entity.Id, normalized, entity.KeyPrefix, Sent: true);
    }

    private async Task DeactivateActiveKeysAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var activeKeys = await _db.ApiKeys
            .Where(k => k.CompanyId == companyId && k.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var existing in activeKeys)
        {
            existing.IsActive = false;
        }
    }

    private string ResolvePublicApiBaseUrl()
    {
        var raw = _configuration["PublicApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = "http://localhost:5200";
        }

        return JobsyPublicUrl.NormalizeOrigin(raw).TrimEnd('/');
    }

    private static string NormalizeName(string? name)
    {
        var label = string.IsNullOrWhiteSpace(name) ? "API-koppeling" : name.Trim();
        if (label.Length > MaxNameLength)
        {
            throw new ArgumentException($"Naam mag maximaal {MaxNameLength} tekens zijn.");
        }

        return label;
    }
}
