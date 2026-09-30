namespace Jobsy.Core.Interfaces;

public interface IEmailPreferenceService
{
    Task<bool> IsOptedOutAsync(string email, string category, CancellationToken cancellationToken = default);

    Task OptOutAsync(string email, string category, string source, CancellationToken cancellationToken = default);

    Task OptInAsync(string email, string category, CancellationToken cancellationToken = default);

    /// <summary>Opt-out using a precomputed e-mail hash (token payload has no plaintext address).</summary>
    Task OptOutByHashAsync(string emailHash, string category, string source, CancellationToken cancellationToken = default);

    Task OptInByHashAsync(string emailHash, string category, CancellationToken cancellationToken = default);

    /// <summary>Optional categories the user can receive, with enabled state.</summary>
    Task<IReadOnlyList<EmailPreferenceItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record EmailPreferenceItem(
    string Key,
    string Label,
    bool Enabled);
