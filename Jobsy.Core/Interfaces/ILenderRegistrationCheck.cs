namespace Jobsy.Core.Interfaces;

/// <summary>
/// Isolated uitleenregistratie (Waadi / Wtta) check for intermediary bureaus (intermediair D23 / 04.5).
/// Registration is never blocked by this; only publishing is gated via <see cref="CanPublish"/>.
/// </summary>
public interface ILenderRegistrationCheck
{
    Task<LenderRegistrationState> GetStateAsync(Guid bureauOrgId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a <c>Pending</c> history row and asks every enabled provider.
    /// Call exactly once after an SBI-78 bureau activation.
    /// </summary>
    Task<LenderRegistrationState> StartForNewBureauAsync(
        Guid bureauOrgId,
        string kvkNumber,
        CancellationToken cancellationToken = default);

    Task RecordDecisionAsync(
        Guid bureauOrgId,
        LenderRegistrationDecision decision,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    bool CanPublish(LenderRegistrationState state);
}

public sealed record LenderRegistrationState(
    Guid BureauOrgId,
    string Status,
    string? Source,
    string? Reference,
    DateTime? CheckedAtUtc,
    DateTime? ValidUntil,
    string? Note,
    string? WaadiCheckUrl);

public sealed record LenderRegistrationDecision(
    bool Approve,
    string Source,
    string? Reference,
    DateTime? ValidUntil,
    string? Note);

public static class LenderRegistrationStatuses
{
    public const string NotChecked = "NotChecked";
    public const string Pending = "Pending";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";
}

public static class LenderRegistrationSources
{
    public const string WaadiKvk = "WaadiKvk";
    public const string WttaNau = "WttaNau";
    public const string AdminManual = "AdminManual";
}

public static class LenderRegistrationRules
{
    public const string PendingErrorCode = "lender_registration_pending";

    public const string PendingMessageNl =
        "We controleren je uitleenregistratie nog. Je kunt vacatures als concept opslaan; publiceren kan zodra Lobsy dit heeft bevestigd.";
}
