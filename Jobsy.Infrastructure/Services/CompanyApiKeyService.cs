using System.Net;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyApiKeyService : ICompanyApiKeyService
{
    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;

    public CompanyApiKeyService(JobsyDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
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

        var activeKeys = await _db.ApiKeys
            .Where(k => k.CompanyId == companyId && k.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var existing in activeKeys)
        {
            existing.IsActive = false;
        }

        var plaintext = ApiKeyHasher.GeneratePlaintext();
        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ApiKeyHash = ApiKeyHasher.Hash(plaintext),
            Name = string.IsNullOrWhiteSpace(name) ? "API-koppeling" : name.Trim(),
            KeyPrefix = ApiKeyHasher.ToDisplayPrefix(plaintext),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

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
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var normalized = recipientEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@'))
        {
            throw new ArgumentException("Ongeldig e-mailadres.");
        }

        // Rotate: plaintext of an existing key is never recoverable from the hash.
        var generated = await GenerateAsync(companyId, "API-koppeling (e-mail)", cancellationToken);

        await _email.SendAsync(new EmailMessage(
            normalized,
            $"Lobsy API-credentials voor {company.Name}",
            $"""
             <p>Hallo,</p>
             <p>Hierbij de API-credentials voor <strong>{WebUtility.HtmlEncode(company.Name)}</strong>.</p>
             <p>Gebruik header <code>X-API-Key</code> op de externe vacature-API.</p>
             <p><strong>Let op:</strong> deze sleutel wordt slechts één keer getoond. Bewaar hem veilig.</p>
             <p>API-key:</p>
             <p><code>{WebUtility.HtmlEncode(generated.PlaintextKey)}</code></p>
             <p>Prefix (ter herkenning): <code>{WebUtility.HtmlEncode(generated.KeyPrefix)}</code></p>
             <p>Eerdere actieve keys voor dit bedrijf zijn gedeactiveerd.</p>
             """,
            "CompanyApiKeyCredentials"), cancellationToken);

        return new EmailApiKeyResult(generated.Id, normalized, generated.KeyPrefix, Sent: true);
    }
}
