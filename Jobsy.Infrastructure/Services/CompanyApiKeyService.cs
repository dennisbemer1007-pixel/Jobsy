using Jobsy.Core;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
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
    private readonly IOneTimeLinkService _links;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<CompanyApiKeyService> _logger;

    public CompanyApiKeyService(
        JobsyDbContext db,
        IEmailService email,
        IConfiguration configuration,
        IOneTimeLinkService links,
        IPlatformFeatureService features,
        ILogger<CompanyApiKeyService> logger)
    {
        _db = db;
        _email = email;
        _configuration = configuration;
        _links = links;
        _features = features;
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

        if (company.ParentCompanyId is not null)
        {
            throw new ArgumentException("API-keys horen bij de organisatie, niet bij een vestiging.");
        }

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

        if (company.ParentCompanyId is not null)
        {
            throw new ArgumentException("API-keys horen bij de organisatie, niet bij een vestiging.");
        }

        var normalized = recipientEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Ongeldig e-mailadres.");
        }

        var features = await _features.GetAsync(cancellationToken);
        var created = await _links.CreateAsync(
            OneTimeLinkPurpose.ApiKeyReveal,
            userId: null,
            companyId: company.Id,
            normalized,
            OneTimeLinkRules.ApiKeyRevealLifetime,
            createdByUserId: null,
            cancellationToken);

        var revealUrl = EmailLayout.Absolute(
            features.PublicWebBaseUrl,
            $"/koppeling/sleutel?t={Uri.EscapeDataString(created.Token)}");
        var expiresAt = DateTime.UtcNow.Add(OneTimeLinkRules.ApiKeyRevealLifetime);

        try
        {
            var apiBase = ResolvePublicApiBaseUrl();
            var keyMail = TransactionalEmails.CompanyApiKeyCredentials(
                features.PublicWebBaseUrl,
                company.Name,
                apiBase,
                revealUrl,
                expiresAt);
            await _email.SendAsync(new EmailMessage(
                normalized,
                keyMail.Subject,
                keyMail.Html,
                keyMail.Category), cancellationToken);
        }
        catch (Exception ex)
        {
            var link = await _db.OneTimeLinks.FirstOrDefaultAsync(l => l.Id == created.Id, cancellationToken);
            if (link is not null)
            {
                _db.OneTimeLinks.Remove(link);
                await _db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogError(
                ex,
                "Failed to e-mail API reveal link for company {CompanyId}; existing key stays active.",
                companyId);
            throw new InvalidOperationException(
                "Versturen van de API-credentials is mislukt. De bestaande key blijft actief.", ex);
        }

        return new EmailApiKeyResult(created.Id, normalized, Sent: true);
    }

    public async Task<ApiKeyRevealResult?> RevealFromTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var link = await _links.ConsumeAsync(OneTimeLinkPurpose.ApiKeyReveal, token, cancellationToken);
        if (link is null || link.CompanyId is not Guid companyId)
        {
            return null;
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null || company.ParentCompanyId is not null)
        {
            return null;
        }

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

        await DeactivateActiveKeysAsync(companyId, cancellationToken);
        _db.ApiKeys.Add(entity);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "apikey.revealed",
            Message = $"apikey.revealed company={companyId:N} key={entity.Id:N} link={link.Id:N}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new ApiKeyRevealResult(
            entity.Id,
            company.Id,
            company.Name,
            entity.Name,
            entity.KeyPrefix,
            plaintext,
            ResolvePublicApiBaseUrl());
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
