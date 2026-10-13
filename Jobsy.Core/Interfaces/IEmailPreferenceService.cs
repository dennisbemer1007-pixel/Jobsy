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

    /// <summary>Master switch for reminder e-mail. Missing user counts as on.</summary>
    Task<bool> AreReminderEmailsEnabledAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    /// <summary>True when this address belongs to a user who turned reminder e-mail off.</summary>
    Task<bool> IsReminderEmailDisabledForAddressAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <summary>Turn reminder e-mail off and opt out of every optional category. Writes one audit row.</summary>
    Task DisableReminderEmailsAsync(Guid userId, string source, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Turn reminder e-mail back on and revoke older unsubscribe links.</summary>
    Task EnableReminderEmailsAsync(Guid userId, string source, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Turn one optional category back on for this user, and the reminder switch if it was off.</summary>
    Task RestoreOptionalCategoryAsync(Guid userId, string category, string source, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>False when the user bumped the revocation epoch after this token was issued.</summary>
    Task<bool> UnsubscribeEpochAllowsAsync(Guid userId, int epoch, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed record EmailPreferenceItem(
    string Key,
    string Label,
    bool Enabled);
