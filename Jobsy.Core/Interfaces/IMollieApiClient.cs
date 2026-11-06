namespace Jobsy.Core.Interfaces;

/// <summary>
/// Shared Mollie HTTP helpers for token packs and deep-test checkouts.
/// </summary>
public interface IMollieApiClient
{
    Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken = default);

    string? ResolveWebhookUrl();

    string? ResolvePublicWebBaseUrl(string? configuredPublicWebBaseUrl);

    Task<MolliePaymentSnapshot> FetchPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<MolliePaymentSnapshot> CreatePaymentAsync(
        MollieCreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Best-effort cancel of an open Mollie payment. Never throws for provider errors.</summary>
    Task TryCancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}

public sealed record MollieCreatePaymentRequest(
    string AmountValue,
    string Description,
    string RedirectUrl,
    string? WebhookUrl,
    IReadOnlyList<string> Methods,
    string? Locale,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record MolliePaymentSnapshot(
    string Id,
    string Status,
    string? Method,
    string? AmountValue,
    string? AmountRefundedValue,
    string? AmountChargedBackValue,
    string? CheckoutUrl,
    IReadOnlyDictionary<string, string> Metadata);
