using System.Globalization;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class TokenFinanceQueryService : ITokenFinanceQueryService
{
    private readonly JobsyDbContext _db;

    public TokenFinanceQueryService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TokenPurchaseFinanceRow>> GetPurchasesAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TokenPurchaseInvoices.AsNoTracking().AsQueryable();
        if (year is int y)
        {
            query = query.Where(i => i.IssuedAt.Year == y);
            if (quarter is int q && q is >= 1 and <= 4)
            {
                var startMonth = (q - 1) * 3 + 1;
                var endMonth = startMonth + 2;
                query = query.Where(i => i.IssuedAt.Month >= startMonth && i.IssuedAt.Month <= endMonth);
            }
        }

        return await query
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => new TokenPurchaseFinanceRow(
                i.Id,
                i.InvoiceNumber,
                i.TokenPurchaseCheckoutId,
                i.MolliePaymentId,
                i.CompanyId,
                i.CompanyName,
                i.PackSize,
                i.AmountExVatCents,
                i.VatAmountCents,
                i.TotalAmountCents,
                i.IssuedAt,
                $"/api/tokens/invoices/{i.Id}/pdf",
                i.VatDeclarationStatusLabel,
                i.Checkout != null ? i.Checkout.Status.ToString() : "Paid",
                i.Checkout != null ? i.Checkout.PaymentMethod : null))
            .Take(2000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TokenGoodwillFinanceRow>> GetGoodwillAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TokenTransactions.AsNoTracking()
            .Where(t => t.Kind == TokenTransactionKind.Goodwill);

        if (year is int y)
        {
            query = query.Where(t => t.CreatedAt.Year == y);
            if (quarter is int q && q is >= 1 and <= 4)
            {
                var startMonth = (q - 1) * 3 + 1;
                var endMonth = startMonth + 2;
                query = query.Where(t => t.CreatedAt.Month >= startMonth && t.CreatedAt.Month <= endMonth);
            }
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TokenGoodwillFinanceRow(
                t.Id,
                t.CompanyId,
                t.Company.Name,
                t.Amount,
                t.Note ?? "",
                t.ActorUserId,
                t.ActorUser != null ? t.ActorUser.FullName : null,
                t.CreatedAt))
            .Take(2000)
            .ToListAsync(cancellationToken);
    }

    public Task<string> ExportPurchasesCsvAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
        => ExportPurchasesCsvAsync(year, quarter, null, cancellationToken);

    public async Task<string> ExportPurchasesCsvAsync(
        int? year,
        int? quarter,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetPurchasesAsync(year, quarter, cancellationToken);
        if (month is int m and >= 1 and <= 12)
        {
            var y = year ?? DateTime.UtcNow.Year;
            rows = rows.Where(r =>
            {
                var local = AmsterdamTime.ToLocal(r.IssuedAt);
                return local.Year == y && local.Month == m;
            }).ToList();
        }

        var sb = new StringBuilder();
        sb.AppendLine("FactuurId;Factuurnummer;MolliePaymentId;Bedrijf;Tokens;ExBtw;Btw;Totaal;Datum");
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        foreach (var r in rows)
        {
            sb.Append(Escape(r.InvoiceId.ToString())).Append(';')
                .Append(Escape(r.InvoiceNumber)).Append(';')
                .Append(Escape(r.MolliePaymentId)).Append(';')
                .Append(Escape(r.CompanyName)).Append(';')
                .Append(r.PackSize.ToString(culture)).Append(';')
                .Append(TokenVatPricing.FormatEuro(r.AmountExVatCents)).Append(';')
                .Append(TokenVatPricing.FormatEuro(r.VatAmountCents)).Append(';')
                .Append(TokenVatPricing.FormatEuro(r.TotalAmountCents)).Append(';')
                .Append(r.IssuedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", culture))
                .AppendLine();
        }

        return sb.ToString();
    }

    public async Task<string> ExportGoodwillCsvAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetGoodwillAsync(year, quarter, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("TransactieId;Datum;Bedrijf;Tokens;WaardeEuro;Reden;UitgegevenDoor");
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        foreach (var r in rows)
        {
            sb.Append(Escape(r.TransactionId.ToString())).Append(';')
                .Append(r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", culture)).Append(';')
                .Append(Escape(r.CompanyName)).Append(';')
                .Append(r.TokenAmount.ToString("0.##", culture)).Append(';')
                .Append("0,00").Append(';')
                .Append(Escape(r.Reason)).Append(';')
                .Append(Escape(r.IssuedByName ?? ""))
                .AppendLine();
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
