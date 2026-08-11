using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IIntegrationCredentialService
{
    Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(
        CancellationToken cancellationToken = default);

    Task<IntegrationCredentialView> UpsertAsync(
        IntegrationKey key,
        IntegrationCredentialUpdate update,
        CancellationToken cancellationToken = default);

    Task SavePingResultAsync(
        IntegrationKey key,
        bool ok,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the raw API key for server-side use (never expose to UI).</summary>
    Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default);

    Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default);

    Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default);

    Task<IntegrationCredentialSecrets?> GetSecretsAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default);
}

public sealed record IntegrationCredentialUpdate(
    string? ApiKey = null,
    string? Model = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? TenantId = null,
    string? BaseUrl = null,
    string? FromAddress = null,
    bool ClearApiKey = false,
    bool ClearClientSecret = false,
    /// <summary>Re-enable Mail env/config bootstrap after an Admin clear suppressed it.</summary>
    bool UseEnvironmentCredentials = false);

public sealed record IntegrationCredentialSecrets(
    string? ApiKey,
    string? ClientId,
    string? ClientSecret,
    string? TenantId,
    string? Model,
    string? BaseUrl,
    string? FromAddress);

public sealed record IntegrationCredentialView(
    IntegrationKey Key,
    string DisplayName,
    string Description,
    bool HasApiKey,
    string? ApiKeyMasked,
    bool HasClientSecret,
    string? ClientSecretMasked,
    string? ClientId,
    string? TenantId,
    string? Model,
    string? BaseUrl,
    string? FromAddress,
    bool SupportsApiKey,
    bool SupportsModel,
    bool SupportsOAuth,
    bool SupportsTenantId,
    bool SupportsBaseUrl,
    bool SupportsFromAddress,
    bool? LastPingOk,
    string? LastPingMessage,
    DateTime? LastPingAtUtc,
    DateTime? UpdatedAtUtc,
    bool IgnoresEnvironmentCredentials = false,
    bool UsesEnvironmentCredentials = false);
