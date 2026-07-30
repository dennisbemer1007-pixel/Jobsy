using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ICompanyApiKeyService
{
    Task<ApiKey?> FindActiveByPlaintextAsync(string plaintextKey, CancellationToken cancellationToken = default);

    Task TouchLastUsedAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyApiKeyView>> ListForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminApiKeyView>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new active key for the company (deactivates any previous active keys).
    /// Returns the plaintext secret exactly once — it is never stored.
    /// </summary>
    Task<GeneratedApiKeyResult> GenerateAsync(
        Guid companyId,
        string? name = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    Task<bool> DeactivateForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates to a new key and e-mails the plaintext secret to the company contact address.
    /// </summary>
    Task<EmailApiKeyResult> EmailCredentialsAsync(
        Guid companyId,
        string recipientEmail,
        CancellationToken cancellationToken = default);
}

public record CompanyApiKeyView(
    Guid Id,
    Guid CompanyId,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTime? LastUsedAt,
    DateTime CreatedAt);

public record AdminApiKeyView(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTime? LastUsedAt,
    DateTime CreatedAt);

public record GeneratedApiKeyResult(
    Guid Id,
    Guid CompanyId,
    string Name,
    string KeyPrefix,
    string PlaintextKey,
    DateTime CreatedAt);

public record EmailApiKeyResult(
    Guid Id,
    string RecipientEmail,
    string KeyPrefix,
    bool Sent);
