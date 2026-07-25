using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IIntegrationHealthService
{
    Task<IReadOnlyList<IntegrationHealthResult>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IntegrationHealthResult> PingAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default);

    /// <summary>Live connection test; persists result on the credential row.</summary>
    Task<IntegrationHealthResult> TestConnectionAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default);
}

public record IntegrationHealthResult(
    IntegrationKey Key,
    string DisplayName,
    bool IsActive,
    string StatusMessage,
    DateTime CheckedAtUtc,
    bool? LastPingOk = null);
