using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Jobsy.Core.Entities;
using Jobsy.Core.Sales;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Sales;

/// <summary>
/// Default payout provider: SEPA pain.001.001.03 + CSV hand-transfer list.
/// // Mollie payouts: implement ISalesPayoutProvider when available
/// </summary>
public sealed class BankTransferPayoutProvider : ISalesPayoutProvider
{
    private readonly SalesPayoutProviderOptions _options;

    public BankTransferPayoutProvider(IOptions<SalesPayoutProviderOptions> options)
        => _options = options.Value;

    public string Key => "bank-transfer";
    public bool SupportsAutomaticPayout => false;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.DebtorName)
        && !string.IsNullOrWhiteSpace(_options.DebtorIban)
        && Iban.IsValid(_options.DebtorIban);

    public string? NotConfiguredMessage =>
        IsConfigured
            ? null
            : "SEPA-export is uitgeschakeld: configureer Sales:Payout:DebtorName, DebtorIban en DebtorBic.";

    public Task<SalesPayoutExportFile> ExportAsync(
        SalesPayoutRun run,
        IReadOnlyList<SalesPayoutExportLine> lines,
        string format,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(lines);

        var fmt = (format ?? "sepa").Trim().ToLowerInvariant();
        if (fmt is "csv" or "handmatig")
        {
            return Task.FromResult(BuildCsv(run, lines));
        }

        if (!IsConfigured)
        {
            throw new InvalidOperationException(NotConfiguredMessage!);
        }

        return Task.FromResult(BuildSepa(run, lines));
    }

    private SalesPayoutExportFile BuildSepa(
        SalesPayoutRun run,
        IReadOnlyList<SalesPayoutExportLine> lines)
    {
        var debtorIban = Iban.Normalize(_options.DebtorIban);
        var debtorName = (_options.DebtorName ?? "").Trim();
        var debtorBic = (_options.DebtorBic ?? "").Trim().ToUpperInvariant();
        var msgId = $"LOB-{run.RunDate:yyyyMMdd}-{run.Id.ToString("N")[..8]}";
        var pmtInfId = $"PMT-{run.RunDate:yyyyMMdd}";
        var execDate = SalesClock.NextWorkday(SalesClock.Today().AddDays(1));
        var ctrlSum = decimal.Round(lines.Sum(l => l.AmountInclVat), 2, MidpointRounding.AwayFromZero);
        var nbOfTxs = lines.Count.ToString(CultureInfo.InvariantCulture);
        var now = DateTime.UtcNow;

        XNamespace ns = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03";
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "Document",
                new XElement(ns + "CstmrCdtTrfInitn",
                    new XElement(ns + "GrpHdr",
                        new XElement(ns + "MsgId", msgId),
                        new XElement(ns + "CreDtTm", now.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)),
                        new XElement(ns + "NbOfTxs", nbOfTxs),
                        new XElement(ns + "CtrlSum", FormatAmount(ctrlSum)),
                        new XElement(ns + "InitgPty",
                            new XElement(ns + "Nm", Truncate(debtorName, 70)))),
                    new XElement(ns + "PmtInf",
                        new XElement(ns + "PmtInfId", pmtInfId),
                        new XElement(ns + "PmtMtd", "TRF"),
                        new XElement(ns + "BtchBookg", "true"),
                        new XElement(ns + "NbOfTxs", nbOfTxs),
                        new XElement(ns + "CtrlSum", FormatAmount(ctrlSum)),
                        new XElement(ns + "PmtTpInf",
                            new XElement(ns + "SvcLvl",
                                new XElement(ns + "Cd", "SEPA"))),
                        new XElement(ns + "ReqdExctnDt", execDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                        new XElement(ns + "Dbtr",
                            new XElement(ns + "Nm", Truncate(debtorName, 70))),
                        new XElement(ns + "DbtrAcct",
                            new XElement(ns + "Id",
                                new XElement(ns + "IBAN", debtorIban))),
                        string.IsNullOrWhiteSpace(debtorBic)
                            ? null
                            : new XElement(ns + "DbtrAgt",
                                new XElement(ns + "FinInstnId",
                                    new XElement(ns + "BIC", debtorBic))),
                        new XElement(ns + "ChrgBr", "SLEV"),
                        lines.Select(l => BuildCreditTransfer(ns, l))))));

        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            doc.Save(writer);
        }

        var bytes = ms.ToArray();
        return new SalesPayoutExportFile(
            $"lobsy-payout-{run.RunDate:yyyy-MM}-pain001.xml",
            "application/xml",
            bytes);
    }

    private static XElement BuildCreditTransfer(XNamespace ns, SalesPayoutExportLine line)
    {
        var creditorIban = Iban.Normalize(line.CreditorIban);
        var e2e = Truncate(line.InvoiceNumber, 35);
        var remittance = Truncate($"Lobsy commissie {line.InvoiceNumber}", 140);
        return new XElement(ns + "CdtTrfTxInf",
            new XElement(ns + "PmtId",
                new XElement(ns + "EndToEndId", e2e)),
            new XElement(ns + "Amt",
                new XElement(ns + "InstdAmt",
                    new XAttribute("Ccy", "EUR"),
                    FormatAmount(line.AmountInclVat))),
            new XElement(ns + "Cdtr",
                new XElement(ns + "Nm", Truncate(line.CreditorName, 70))),
            new XElement(ns + "CdtrAcct",
                new XElement(ns + "Id",
                    new XElement(ns + "IBAN", creditorIban))),
            new XElement(ns + "RmtInf",
                new XElement(ns + "Ustrd", remittance)));
    }

    private static SalesPayoutExportFile BuildCsv(
        SalesPayoutRun run,
        IReadOnlyList<SalesPayoutExportLine> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Naam;MaskedIBAN;BedragInclBtw;Factuurnummer");
        foreach (var l in lines)
        {
            sb.Append(EscapeCsv(l.CreditorName)).Append(';')
                .Append(EscapeCsv(l.MaskedIban)).Append(';')
                .Append(FormatAmount(l.AmountInclVat)).Append(';')
                .Append(EscapeCsv(l.InvoiceNumber)).AppendLine();
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        return new SalesPayoutExportFile(
            $"lobsy-payout-{run.RunDate:yyyy-MM}-handmatig.csv",
            "text/csv",
            bytes);
    }

    public static string Sha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string FormatAmount(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int max)
    {
        var v = (value ?? string.Empty).Trim();
        return v.Length <= max ? v : v[..max];
    }

    private static string EscapeCsv(string value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(';') || v.Contains('\n'))
        {
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }

        return v;
    }
}
