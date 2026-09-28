using Jobsy.Core.Enums;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed class InsufficientTokensException : Exception
{
    public InsufficientTokensException(InsufficientTokensInfo info)
        : base(info.Message)
    {
        Info = info;
    }

    public InsufficientTokensInfo Info { get; }
}

public sealed class TokenPurchaseFinanceItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CheckoutId { get; set; }
    public string MolliePaymentId { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int PackSize { get; set; }
    public int AmountExVatCents { get; set; }
    public int VatAmountCents { get; set; }
    public int TotalAmountCents { get; set; }
    public decimal AmountExVatEuro { get; set; }
    public decimal VatAmountEuro { get; set; }
    public decimal TotalAmountEuro { get; set; }
    public DateTime IssuedAt { get; set; }
    public string InvoicePdfUrl { get; set; } = string.Empty;
    public string? VatDeclarationStatusLabel { get; set; }
}

public sealed class TokenGoodwillFinanceItem
{
    public Guid TransactionId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal TokenAmount { get; set; }
    public int AmountExVatCents { get; set; }
    public int VatAmountCents { get; set; }
    public int TotalAmountCents { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? IssuedByUserId { get; set; }
    public string? IssuedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class VatBufferTransferItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string DestinationIbanMasked { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public decimal AmountEuro { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Note { get; set; }
}

public sealed class VatOpenPeriodItem
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int OpenTokenInvoiceCount { get; set; }
    public int OpenSalesManagerInvoiceCount { get; set; }
    public bool HasOpenItems { get; set; }
}

public sealed class VatDeclarationPreviewItem
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int Rubriek1OmzetExVatCents { get; set; }
    public int Rubriek1VatCents { get; set; }
    public int TokenInvoiceCount { get; set; }
    public int GoodwillCount { get; set; }
    public int Rubriek5VoorbelastingCents { get; set; }
    public int Rubriek5CostExVatCents { get; set; }
    public int SalesManagerInvoiceCount { get; set; }
    public int AmountDueCents { get; set; }
    public bool AlreadyDeclared { get; set; }
}

public sealed class VatDeclarationListItem
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Rubriek1OmzetExVatCents { get; set; }
    public int Rubriek1VatCents { get; set; }
    public int Rubriek5VoorbelastingCents { get; set; }
    public int AmountDueCents { get; set; }
    public int TokenInvoiceCount { get; set; }
    public int GoodwillCount { get; set; }
    public int SalesManagerInvoiceCount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedByName { get; set; }
    public string PlatformCompanyName { get; set; } = string.Empty;
    public bool HasPdf { get; set; }
}
