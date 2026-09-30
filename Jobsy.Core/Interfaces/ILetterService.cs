namespace Jobsy.Core.Interfaces;

public sealed record LetterAddress(
    string RecipientName,
    IReadOnlyList<string> AddressLines,
    string PostalCode,
    string City,
    string Country = "NL");

public sealed record LetterRequest(
    string RecipientName,
    IReadOnlyList<string> AddressLines,
    string PostalCode,
    string City,
    string Country,
    byte[] Pdf,
    string Reference);

public sealed record LetterSendResult(
    bool Ok,
    string? ProviderLetterId,
    string? ErrorCode,
    string? ErrorMessage);

public enum LetterDeliveryStatus
{
    Unknown = 0,
    Queued = 1,
    Sent = 2,
    Delivered = 3,
    Undeliverable = 4,
    Cancelled = 5
}

public sealed record LetterStatus(
    string ProviderLetterId,
    LetterDeliveryStatus Status,
    DateTime? UpdatedAtUtc,
    string? Detail);

/// <summary>Sends verification letters (Pingen production/staging or in-memory stub).</summary>
public interface ILetterService
{
    string ProviderName { get; }

    Task<LetterSendResult> SendAsync(LetterRequest request, CancellationToken cancellationToken = default);

    Task<LetterStatus> GetStatusAsync(string providerLetterId, CancellationToken cancellationToken = default);
}

/// <summary>Stub-only store so admins can open the generated PDF (code inside) during tests.</summary>
public interface IStubLetterStore
{
    void Save(string providerLetterId, byte[] pdf, string reference);

    bool TryGet(string providerLetterId, out byte[]? pdf, out string? reference);
}
