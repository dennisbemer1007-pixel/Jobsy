using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesWalletPortalService : ISalesWalletPortalService
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");
    private const int PageSize = 25;

    private readonly JobsyDbContext _db;
    private readonly ISalesWalletReadService _wallet;
    private readonly ISalesPayoutRequestService _payouts;
    private readonly IPlatformCompanySettingsService _companySettings;

    static SalesWalletPortalService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SalesWalletPortalService(
        JobsyDbContext db,
        ISalesWalletReadService wallet,
        ISalesPayoutRequestService payouts,
        IPlatformCompanySettingsService companySettings)
    {
        _db = db;
        _wallet = wallet;
        _payouts = payouts;
        _companySettings = companySettings;
    }

    public async Task<SalesWalletOverviewDto> GetOverviewAsync(
        Guid beneficiaryUserId,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var today = SalesClock.Today(now);
        var balances = await _wallet.GetBalancesAsync(beneficiaryUserId, now, cancellationToken);
        var settings = await _db.SalesCommercialSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == beneficiaryUserId, cancellationToken);
        var treatment = profile?.VatTreatment ?? SalesManagerVatTreatment.Standard21;
        var isKor = treatment == SalesManagerVatTreatment.SmallBusinessScheme;
        var vatOnAvailable = isKor ? 0m : SalesCommissionRules.VatOn(balances.Available);

        // Preview blockers without MFA (overview uses CanRequest from server with mfa=true placeholder;
        // the request endpoint re-checks MFA). Overview CTA still lists blockers excluding MFA when
        // the caller hasn't passed MFA — API sets mfaSatisfied.
        var preview = await _payouts.PreviewAsync(beneficiaryUserId, mfaSatisfied: true, now, cancellationToken);

        var invoices = await ListInvoicesAsync(beneficiaryUserId, cancellationToken);
        var yearInvoices = invoices
            .Where(i => SalesClock.Today(i.CreatedAtUtc).Year == today.Year)
            .ToList();

        return new SalesWalletOverviewDto
        {
            Available = balances.Available,
            Pending = balances.Pending,
            Requested = balances.Requested,
            PaidThisYear = balances.PaidThisYear,
            InvoiceCountThisYear = yearInvoices.Count,
            LocalYear = today.Year,
            NextRunDate = preview.ExpectedRunDate,
            PayoutMinimumEuro = settings?.PayoutMinimumEuro ?? 50m,
            CommissionHoldDays = settings?.CommissionHoldDays ?? 14,
            VatTreatment = treatment.ToString(),
            IsKor = isKor,
            VatOnAvailable = vatOnAvailable,
            MaskedIban = string.IsNullOrWhiteSpace(profile?.Iban) ? null : Iban.Mask(profile.Iban),
            HolderName = profile?.PayoutAccountHolderName,
            CanRequestPayout = preview.CanRequest,
            Blockers = preview.Blockers,
            LatestInvoices = invoices.Take(3).ToList(),
            OpenRequestId = preview.OpenRequestId
        };
    }

    public async Task<SalesWalletEntriesPageDto> ListEntriesAsync(
        Guid beneficiaryUserId,
        int? periodYear,
        string? kind,
        string? state,
        int page = 1,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        page = Math.Max(1, page);

        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.Kind,
                e.AmountExVat,
                e.AvailableFromUtc,
                e.SalesPayoutRequestId,
                e.SelfBillingInvoiceId,
                e.CorrectsEntryId,
                e.CreatedAt,
                e.CompanyId,
                e.Reason,
                e.Note,
                e.SourceTokenCheckoutId
            })
            .ToListAsync(cancellationToken);

        var companyIds = entries.Where(e => e.CompanyId is not null)
            .Select(e => e.CompanyId!.Value).Distinct().ToList();
        var companies = companyIds.Count == 0
            ? new Dictionary<Guid, (string Name, string Address, CompanyLegalForm? LegalForm, Guid? ParentCompanyId)>()
            : await _db.Companies.AsNoTracking()
                .Where(c => companyIds.Contains(c.Id))
                .ToDictionaryAsync(
                    c => c.Id,
                    c => (c.Name, c.Address, c.LegalForm, c.ParentCompanyId),
                    cancellationToken);

        // Resolve root display names for vestigingen.
        var parentIds = companies.Values
            .Where(c => c.ParentCompanyId is not null)
            .Select(c => c.ParentCompanyId!.Value)
            .Distinct()
            .Where(id => !companies.ContainsKey(id))
            .ToList();
        if (parentIds.Count > 0)
        {
            var parents = await _db.Companies.AsNoTracking()
                .Where(c => parentIds.Contains(c.Id))
                .ToDictionaryAsync(
                    c => c.Id,
                    c => (c.Name, c.Address, c.LegalForm, c.ParentCompanyId),
                    cancellationToken);
            foreach (var p in parents)
            {
                companies[p.Key] = p.Value;
            }
        }

        var invoiceIds = entries.Where(e => e.SelfBillingInvoiceId is not null)
            .Select(e => e.SelfBillingInvoiceId!.Value).Distinct().ToList();
        var requestIds = entries.Where(e => e.SalesPayoutRequestId is not null)
            .Select(e => e.SalesPayoutRequestId!.Value).Distinct().ToList();
        var invoiceStatuses = invoiceIds.Count == 0
            ? new Dictionary<Guid, SelfBillingInvoiceStatus>()
            : await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Status, cancellationToken);
        var invoiceNumbers = invoiceIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, cancellationToken);
        var requestStatuses = requestIds.Count == 0
            ? new Dictionary<Guid, SalesPayoutRequestStatus>()
            : await _db.SalesPayoutRequests.AsNoTracking()
                .Where(r => requestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Status, cancellationToken);

        var checkoutIds = entries.Where(e => e.SourceTokenCheckoutId is not null)
            .Select(e => e.SourceTokenCheckoutId!.Value).Distinct().ToList();
        var checkouts = checkoutIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _db.TokenPurchaseCheckouts.AsNoTracking()
                .Where(c => checkoutIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.AmountExVatCents / 100m, cancellationToken);

        var mapped = new List<SalesWalletEntryDto>();
        foreach (var e in entries)
        {
            SelfBillingInvoiceStatus? inv = e.SelfBillingInvoiceId is Guid iid
                && invoiceStatuses.TryGetValue(iid, out var is_)
                ? is_
                : null;
            SalesPayoutRequestStatus? req = e.SalesPayoutRequestId is Guid rid
                && requestStatuses.TryGetValue(rid, out var rs)
                ? rs
                : null;

            var stub = new CommissionLedgerEntry
            {
                Kind = e.Kind,
                AmountExVat = e.AmountExVat,
                AvailableFromUtc = e.AvailableFromUtc,
                SalesPayoutRequestId = e.SalesPayoutRequestId,
                SelfBillingInvoiceId = e.SelfBillingInvoiceId,
                CorrectsEntryId = e.CorrectsEntryId
            };
            var entryState = _wallet.DeriveState(stub, now, inv, req);
            var localDate = DateOnly.FromDateTime(SalesClock.ToLocal(e.CreatedAt).DateTime);

            if (periodYear is int y && localDate.Year != y)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(kind)
                && !string.Equals(e.Kind.ToString(), kind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(state)
                && !string.Equals(entryState.ToString(), state, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var displayName = ResolveDisplayName(e.CompanyId, companies);
            var kindKey = SalesLabels.Key(e.Kind);
            var title = BuildTitle(e.Kind, displayName, e.SelfBillingInvoiceId, invoiceNumbers, e.Note);
            var sub = BuildSubLine(e.Kind, e.Reason, e.Note, e.SourceTokenCheckoutId, checkouts, e.AmountExVat);

            mapped.Add(new SalesWalletEntryDto
            {
                Id = e.Id,
                Date = localDate,
                Kind = e.Kind.ToString(),
                KindLabelKey = kindKey,
                Title = title,
                SubLine = sub,
                State = entryState.ToString(),
                StateLabelKey = SalesLabels.Key(entryState),
                AvailableOn = entryState == CommissionEntryState.Pending
                    ? DateOnly.FromDateTime(SalesClock.ToLocal(e.AvailableFromUtc).DateTime)
                    : null,
                AmountExVat = e.AmountExVat
            });
        }

        var total = mapped.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        if (page > totalPages)
        {
            page = totalPages;
        }

        var pageItems = mapped.Skip((page - 1) * PageSize).Take(PageSize).ToList();
        return new SalesWalletEntriesPageDto
        {
            Items = pageItems,
            Page = page,
            PageSize = PageSize,
            TotalCount = total,
            TotalPages = totalPages
        };
    }

    public async Task<IReadOnlyList<SalesPayoutListItemDto>> ListPayoutsAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default)
    {
        var requests = await _db.SalesPayoutRequests.AsNoTracking()
            .Where(r => r.BeneficiaryUserId == beneficiaryUserId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var invoiceIds = requests.Where(r => r.SelfBillingInvoiceId is not null)
            .Select(r => r.SelfBillingInvoiceId!.Value).Distinct().ToList();
        var invoiceNumbers = invoiceIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, cancellationToken);

        var items = new List<SalesPayoutListItemDto>();
        foreach (var r in requests)
        {
            var (steps, active) = StepperFor(r.Status);
            items.Add(new SalesPayoutListItemDto
            {
                Id = r.Id,
                IsLegacyCheckout = false,
                AmountExVat = r.AmountExVat,
                TotalInclVat = r.TotalInclVat,
                MaskedIban = r.MaskedIban,
                Status = r.Status.ToString(),
                StatusLabelKey = SalesLabels.Key(r.Status),
                RequestedAtUtc = r.RequestedAtUtc,
                InvoiceId = r.SelfBillingInvoiceId,
                InvoiceNumber = r.SelfBillingInvoiceId is Guid iid
                    && invoiceNumbers.TryGetValue(iid, out var num)
                    ? num
                    : null,
                RejectionReason = r.RejectionReason,
                StepperSteps = steps,
                ActiveStepIndex = active
            });
        }

        // Legacy stub checkouts as "Eerdere uitbetaling".
        var legacy = await _db.SalesManagerPayoutCheckouts.AsNoTracking()
            .Where(c => c.SalesManagerUserId == beneficiaryUserId
                        && c.Status == SalesManagerPayoutCheckoutStatus.Completed)
            .OrderByDescending(c => c.CompletedAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var c in legacy)
        {
            string? invNum = null;
            if (c.SelfBillingInvoiceId is Guid iid)
            {
                invNum = await _db.SelfBillingInvoices.AsNoTracking()
                    .Where(i => i.Id == iid)
                    .Select(i => i.InvoiceNumber)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            items.Add(new SalesPayoutListItemDto
            {
                Id = c.Id,
                IsLegacyCheckout = true,
                AmountExVat = c.AmountExVat,
                TotalInclVat = c.AmountEuro,
                MaskedIban = c.MaskedIban,
                Status = "Paid",
                StatusLabelKey = "Sales.Label.PayoutRequest.Paid",
                RequestedAtUtc = c.CreatedAt,
                InvoiceId = c.SelfBillingInvoiceId,
                InvoiceNumber = invNum,
                StepperSteps = ["Sales.Wallet.Step.Requested", "Sales.Wallet.Step.InRun", "Sales.Wallet.Step.Approved", "Sales.Wallet.Step.Paid"],
                ActiveStepIndex = 3
            });
        }

        return items
            .OrderByDescending(i => i.RequestedAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<SalesInvoiceListItemDto>> ListInvoicesAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == beneficiaryUserId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.Id, i.InvoiceNumber, i.CreatedAt, i.TotalInclVat, i.SubtotalExVat, i.Status })
            .ToListAsync(cancellationToken);

        return list.Select(i => new SalesInvoiceListItemDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            CreatedAtUtc = i.CreatedAt,
            TotalInclVat = i.TotalInclVat,
            SubtotalExVat = i.SubtotalExVat,
            Status = i.Status.ToString(),
            StatusLabelKey = SalesLabels.Key(i.Status)
        }).ToList();
    }

    public async Task<byte[]> RenderJaaroverzichtPdfAsync(
        Guid beneficiaryUserId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == beneficiaryUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Profiel niet gevonden.");

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);
        var startUtc = SalesClock.EndOfLocalDayUtc(yearStart.AddDays(-1)).AddSeconds(1);
        var endUtc = SalesClock.EndOfLocalDayUtc(yearEnd);

        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && e.CreatedAt >= startUtc
                        && e.CreatedAt <= endUtc)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var invoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == beneficiaryUserId
                        && i.CreatedAt >= startUtc
                        && i.CreatedAt <= endUtc)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var earned = entries
            .Where(e => e.Kind is CommissionEntryKind.TokenCommission
                or CommissionEntryKind.IndirectTokenCommission
                or CommissionEntryKind.FounderBonus)
            .Sum(e => e.AmountExVat);
        var paid = invoices
            .Where(i => i.Status == SelfBillingInvoiceStatus.Paid)
            .Sum(i => i.SubtotalExVat);

        var platform = await _companySettings.GetAsync(cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(SalesPdfInvoiceCopy.JaaroverzichtTitle).FontSize(18).SemiBold();
                    col.Item().Text($"{profile.CompanyName} · {year}").FontSize(11);
                    col.Item().PaddingTop(4).Text(platform.CompanyName).FontSize(9).FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Text($"{SalesPdfInvoiceCopy.JaaroverzichtEarned}: {SalesMoney.Format(earned, SalesMoneyKind.ExVat)}");
                    col.Item().Text($"{SalesPdfInvoiceCopy.JaaroverzichtPaid}: {SalesMoney.Format(paid, SalesMoneyKind.ExVat)}");
                    col.Item().PaddingTop(12).Text(SalesPdfInvoiceCopy.JaaroverzichtInvoices).SemiBold();
                    foreach (var inv in invoices)
                    {
                        col.Item().Text(
                            $"{inv.InvoiceNumber} · {SalesClock.ToLocal(inv.CreatedAt).ToString("dd-MM-yyyy", Nl)} · "
                            + $"{SalesMoney.Format(inv.TotalInclVat, SalesMoneyKind.InclVat)} · {inv.Status}");
                    }

                    if (invoices.Count == 0)
                    {
                        col.Item().Text("—");
                    }
                });
            });
        }).GeneratePdf();
    }

    private static (IReadOnlyList<string> Steps, int Active) StepperFor(SalesPayoutRequestStatus status)
    {
        string[] steps =
        [
            "Sales.Wallet.Step.Requested",
            "Sales.Wallet.Step.InRun",
            "Sales.Wallet.Step.Approved",
            "Sales.Wallet.Step.Paid"
        ];
        var active = status switch
        {
            SalesPayoutRequestStatus.Requested => 0,
            SalesPayoutRequestStatus.InRun => 1,
            SalesPayoutRequestStatus.Approved => 2,
            SalesPayoutRequestStatus.Paid => 3,
            SalesPayoutRequestStatus.Rejected => 0,
            SalesPayoutRequestStatus.Cancelled => 0,
            _ => 0
        };
        return (steps, active);
    }

    private static string ResolveDisplayName(
        Guid? companyId,
        Dictionary<Guid, (string Name, string Address, CompanyLegalForm? LegalForm, Guid? ParentCompanyId)> companies)
    {
        if (companyId is not Guid id || !companies.TryGetValue(id, out var c))
        {
            return "";
        }

        if (c.ParentCompanyId is Guid parentId && companies.TryGetValue(parentId, out var parent))
        {
            return parent.Name;
        }

        return c.Name;
    }

    private static string BuildTitle(
        CommissionEntryKind kind,
        string displayName,
        Guid? invoiceId,
        Dictionary<Guid, string> invoiceNumbers,
        string? note)
    {
        var kindLabel = kind switch
        {
            CommissionEntryKind.TokenCommission or CommissionEntryKind.IndirectTokenCommission => "Commissie",
            CommissionEntryKind.FounderBonus => "Founder-bonus",
            CommissionEntryKind.Payout => "Uitbetaling",
            CommissionEntryKind.Adjustment or CommissionEntryKind.RefundCorrection
                or CommissionEntryKind.ChargebackCorrection => "Correctie",
            _ => "Mutatie"
        };

        if (kind == CommissionEntryKind.Payout
            && invoiceId is Guid iid
            && invoiceNumbers.TryGetValue(iid, out var num))
        {
            return $"{kindLabel} · factuur {num}";
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return $"{kindLabel} · {displayName}";
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            return $"{kindLabel} · {note}";
        }

        return kindLabel;
    }

    private static string? BuildSubLine(
        CommissionEntryKind kind,
        string? reason,
        string? note,
        Guid? checkoutId,
        Dictionary<Guid, decimal> checkouts,
        decimal amount)
    {
        if (kind is CommissionEntryKind.RefundCorrection
                or CommissionEntryKind.ChargebackCorrection
                or CommissionEntryKind.Adjustment)
        {
            return reason ?? note;
        }

        if (kind is CommissionEntryKind.TokenCommission or CommissionEntryKind.IndirectTokenCommission
            && checkoutId is Guid cid
            && checkouts.TryGetValue(cid, out var purchase)
            && purchase > 0
            && amount > 0)
        {
            var rate = decimal.Round(amount / purchase * 100m, 0, MidpointRounding.AwayFromZero);
            // Year is not stored on the line; show rate over purchase only.
            return $"{rate} % over {SalesMoney.FormatPlain(purchase)}";
        }

        if (kind == CommissionEntryKind.FounderBonus)
        {
            return note ?? "20 % van startpakket";
        }

        return null;
    }
}

/// <summary>Dutch PDF copy for invoices / jaaroverzicht (D5). Mirrored in UiStringsSales as SalesPdf.*.</summary>
public static class SalesPdfInvoiceCopy
{
    public const string InvoiceTitle = "Factuur";
    public const string IssuedByCustomer = "Factuur uitgereikt door afnemer";
    public const string SelfBillingAccordingTo =
        "Self-billing volgens de afspraak van {0} (versie {1})";
    public const string Supplier = "Leverancier";
    public const string Customer = "Afnemer";
    public const string Vat21 = "Btw 21 %";
    public const string VatKor =
        "Btw-vrijgesteld op grond van de kleineondernemersregeling (KOR)";
    public const string PaidToAccount = "Betaald aan rekening {0}";
    public const string Subtotal = "Subtotaal excl. btw";
    public const string Total = "Totaal incl. btw";
    public const string JaaroverzichtTitle = "Jaaroverzicht commissie";
    public const string JaaroverzichtEarned = "Totaal verdiend";
    public const string JaaroverzichtPaid = "Totaal uitbetaald";
    public const string JaaroverzichtInvoices = "Facturen";
}
