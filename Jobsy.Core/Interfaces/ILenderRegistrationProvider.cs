namespace Jobsy.Core.Interfaces;

/// <summary>Pluggable uitleenregistratie provider (Waadi via KvK, Wtta/NAU later).</summary>
public interface ILenderRegistrationProvider
{
    string Name { get; }
    bool IsEnabled { get; }
    Task<LenderProviderResult> CheckAsync(string kvkNumber, CancellationToken cancellationToken = default);
}

public sealed record LenderProviderResult(
    string Outcome,
    string? Reference = null,
    string? DeepLink = null,
    string? Note = null);

public static class LenderProviderOutcomes
{
    public const string Unknown = "Unknown";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";
}
