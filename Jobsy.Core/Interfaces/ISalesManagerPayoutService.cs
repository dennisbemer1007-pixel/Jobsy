namespace Jobsy.Core.Interfaces;

public interface ISalesManagerPayoutService
{
    Task<SalesManagerPayoutPreviewDto> GetPreviewAsync(
        Guid salesManagerUserId,
        decimal? requestedAmountExVat = null,
        CancellationToken cancellationToken = default);

    Task<SalesManagerPayoutCheckoutResult> CreateCheckoutAsync(
        Guid salesManagerUserId,
        decimal requestedAmountExVat,
        CancellationToken cancellationToken = default);

    Task<SalesManagerPayoutCompleteResult> CompleteCheckoutAsync(
        string paymentId,
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<byte[]> RenderInvoicePdfAsync(
        Guid invoiceId,
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    static string MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return "—";
        }

        var compact = iban.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        if (compact.Length < 6)
        {
            return "****";
        }

        var prefix = compact.Length >= 2 ? compact[..2] : compact;
        var suffix = compact[^4..];
        return $"{prefix}**{suffix}";
    }
}

public sealed record SalesManagerPayoutPreviewDto(
    decimal AvailableExVat,
    decimal AmountExVat,
    decimal VatAmount,
    decimal AmountInclVat,
    /// <summary>Always null in API responses — use <see cref="MaskedIban"/>.</summary>
    string? Iban,
    string MaskedIban,
    bool CanPayout,
    string? BlockReason);

public sealed record SalesManagerPayoutCheckoutResult(
    string PaymentId,
    string CheckoutUrl,
    decimal AmountEuro,
    string MaskedIban,
    bool IsStub);

public sealed record SalesManagerPayoutCompleteResult(
    Guid InvoiceId,
    string InvoiceNumber,
    decimal TotalInclVat,
    string MaskedIban,
    string Status);
