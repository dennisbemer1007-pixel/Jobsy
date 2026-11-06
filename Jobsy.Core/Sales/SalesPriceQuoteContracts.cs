namespace Jobsy.Core.Sales;

/// <summary>
/// Real token pack prices for sales surfaces (D14). Never mix <c>SalesPackage</c> into per-token.
/// </summary>
public sealed record SalesPriceQuote(
    decimal MinPricePerToken,
    decimal MaxPricePerToken,
    IReadOnlyList<SalesTokenPackQuote> Packs,
    IReadOnlyList<SalesActionCostQuote> Actions);

public sealed record SalesTokenPackQuote(
    int PackSize,
    decimal PriceExVat,
    decimal PricePerToken);

public sealed record SalesActionCostQuote(
    string Key,
    string Label,
    decimal CostTokens,
    decimal FromEuro);

public interface ISalesPriceQuote
{
    Task<SalesPriceQuote> GetAsync(CancellationToken cancellationToken = default);
}
