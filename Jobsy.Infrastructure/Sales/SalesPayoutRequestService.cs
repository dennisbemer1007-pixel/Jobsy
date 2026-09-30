using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesPayoutRequestService : ISalesPayoutRequestService
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    private readonly JobsyDbContext _db;
    private readonly ISalesWalletReadService _wallet;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;

    public SalesPayoutRequestService(
        JobsyDbContext db,
        ISalesWalletReadService wallet,
        ITransactionalMailer mailer,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features)
    {
        _db = db;
        _wallet = wallet;
        _mailer = mailer;
        _companySettings = companySettings;
        _features = features;
    }

    public async Task<SalesPayoutPreviewDto> PreviewAsync(
        Guid beneficiaryUserId,
        bool mfaSatisfied,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var ctx = await LoadContextAsync(beneficiaryUserId, now, cancellationToken);
        var blockers = BuildBlockers(ctx, mfaSatisfied);
        var amount = decimal.Round(ctx.Available, 2, MidpointRounding.AwayFromZero);
        var isKor = ctx.VatTreatment == SalesManagerVatTreatment.SmallBusinessScheme;
        var vat = isKor ? 0m : SalesCommissionRules.VatOn(amount);
        var total = amount + vat;
        var runDate = ResolveExpectedRunDate(ctx.Today, ctx.IbanHoldUntilLocal);

        SalesInvoicePreviewDto? invoice = null;
        if (amount > 0 && ctx.Profile is not null)
        {
            var platform = await _companySettings.GetAsync(cancellationToken);
            invoice = new SalesInvoicePreviewDto
            {
                InvoiceNumberPlaceholder = "wordt toegekend bij goedkeuring",
                SupplierCompanyName = ctx.Profile.CompanyName ?? "",
                SupplierKvk = ctx.Profile.KvkNumber ?? "",
                SupplierVat = string.IsNullOrWhiteSpace(ctx.Profile.VatNumber) ? null : ctx.Profile.VatNumber,
                SupplierAddress = FormatAddress(ctx.Profile),
                CustomerName = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy B.V." : platform.CompanyName,
                ConsentDate = ctx.ConsentAt is DateTime c
                    ? DateOnly.FromDateTime(SalesClock.ToLocal(c).DateTime)
                    : null,
                ConsentVersion = ctx.ConsentVersion,
                LineCount = ctx.AvailableLines.Count,
                PeriodLabel = ctx.Today.ToString("MMMM yyyy", Nl),
                SubtotalExVat = amount,
                VatAmount = vat,
                TotalInclVat = total,
                VatTreatment = ctx.VatTreatment.ToString(),
                IsKor = isKor
            };
        }

        return new SalesPayoutPreviewDto
        {
            AvailableExVat = amount,
            AmountExVat = amount,
            VatAmount = vat,
            TotalInclVat = total,
            VatTreatment = ctx.VatTreatment.ToString(),
            IsKor = isKor,
            MaskedIban = ctx.MaskedIban,
            HolderName = ctx.Profile?.PayoutAccountHolderName,
            ExpectedRunDate = runDate,
            IbanHoldUntil = ctx.IbanHoldUntilLocal,
            PayoutMinimumEuro = ctx.Minimum,
            CommissionHoldDays = ctx.HoldDays,
            CanRequest = blockers.Count == 0 && amount > 0,
            Blockers = blockers,
            InvoicePreview = invoice,
            OpenRequestId = ctx.OpenRequest?.Id
        };
    }

    public async Task<SalesPayoutRequestDto> RequestAsync(
        Guid beneficiaryUserId,
        bool mfaSatisfied,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;

        // Idempotent: return existing open request on double-click.
        var existing = await FindOpenRequestAsync(beneficiaryUserId, cancellationToken);
        if (existing is not null)
        {
            return await MapRequestAsync(existing, cancellationToken);
        }

        var preview = await PreviewAsync(beneficiaryUserId, mfaSatisfied, now, cancellationToken);
        if (!preview.CanRequest || preview.Blockers.Count > 0)
        {
            var first = preview.Blockers.FirstOrDefault();
            throw new InvalidOperationException(first?.MessageKey ?? "Sales.Payout.Blocked");
        }

        var ctx = await LoadContextAsync(beneficiaryUserId, now, cancellationToken);
        if (ctx.AvailableLines.Count == 0 || ctx.Available <= 0)
        {
            throw new InvalidOperationException("Sales.Payout.NothingAvailable");
        }

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Re-check open request inside the transaction.
        existing = await FindOpenRequestAsync(beneficiaryUserId, cancellationToken);
        if (existing is not null)
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            return await MapRequestAsync(existing, cancellationToken);
        }

        var amount = decimal.Round(ctx.Available, 2, MidpointRounding.AwayFromZero);
        var isKor = ctx.VatTreatment == SalesManagerVatTreatment.SmallBusinessScheme;
        var vat = isKor ? 0m : SalesCommissionRules.VatOn(amount);
        var requestId = Guid.NewGuid();
        var request = new SalesPayoutRequest
        {
            Id = requestId,
            BeneficiaryUserId = beneficiaryUserId,
            AmountExVat = amount,
            VatTreatment = ctx.VatTreatment,
            VatAmount = vat,
            TotalInclVat = amount + vat,
            MaskedIban = ctx.MaskedIban.Length > 34 ? ctx.MaskedIban[..34] : ctx.MaskedIban,
            Status = SalesPayoutRequestStatus.Requested,
            RequestedAtUtc = now
        };
        _db.SalesPayoutRequests.Add(request);

        var lineIds = ctx.AvailableLines.Select(l => l.Id).ToList();
        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            int linked;
            try
            {
                linked = await _db.CommissionLedgerEntries
                    .Where(e => lineIds.Contains(e.Id) && e.SalesPayoutRequestId == null)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(e => e.SalesPayoutRequestId, requestId),
                        cancellationToken);
            }
            catch (InvalidOperationException)
            {
                linked = 0;
                foreach (var id in lineIds)
                {
                    var tracked = await _db.CommissionLedgerEntries
                        .FirstAsync(e => e.Id == id, cancellationToken);
                    if (tracked.SalesPayoutRequestId is null)
                    {
                        tracked.SalesPayoutRequestId = requestId;
                        linked++;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            if (linked != lineIds.Count)
            {
                if (tx is not null)
                {
                    await tx.RollbackAsync(cancellationToken);
                }
                else
                {
                    _db.SalesPayoutRequests.Remove(request);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                throw new InvalidOperationException("Sales.Payout.Race");
            }

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            // Unique open-request index: another request won the race.
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            _db.ChangeTracker.Clear();
            existing = await FindOpenRequestAsync(beneficiaryUserId, cancellationToken);
            if (existing is not null)
            {
                return await MapRequestAsync(existing, cancellationToken);
            }

            throw;
        }

        await SendRequestedMailAsync(beneficiaryUserId, preview.ExpectedRunDate, cancellationToken);
        return await MapRequestAsync(request, cancellationToken);
    }

    public async Task CancelAsync(
        Guid beneficiaryUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.SalesPayoutRequests
            .FirstOrDefaultAsync(
                r => r.Id == requestId && r.BeneficiaryUserId == beneficiaryUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Aanvraag niet gevonden.");

        if (request.Status != SalesPayoutRequestStatus.Requested)
        {
            throw new InvalidOperationException(
                "Alleen een openstaande aanvraag (nog niet in een ronde) kun je annuleren.");
        }

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            await _db.CommissionLedgerEntries
                .Where(e => e.SalesPayoutRequestId == requestId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.SalesPayoutRequestId, (Guid?)null),
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            var lines = await _db.CommissionLedgerEntries
                .Where(e => e.SalesPayoutRequestId == requestId)
                .ToListAsync(cancellationToken);
            foreach (var line in lines)
            {
                line.SalesPayoutRequestId = null;
            }
        }

        request.Status = SalesPayoutRequestStatus.Cancelled;
        request.DecidedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }
    }

    private async Task<PayoutContext> LoadContextAsync(
        Guid beneficiaryUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var today = SalesClock.Today(utcNow);
        var settings = await _db.SalesCommercialSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var minimum = settings?.PayoutMinimumEuro ?? 50m;
        var holdDays = settings?.CommissionHoldDays ?? 14;

        var profile = await _db.SalesManagerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == beneficiaryUserId, cancellationToken);

        var consent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .Where(c => c.UserId == beneficiaryUserId
                        && c.Version == SalesSelfBilling.CurrentVersion
                        && c.RevokedAtUtc == null)
            .OrderByDescending(c => c.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var open = await FindOpenRequestAsync(beneficiaryUserId, cancellationToken);
        var balances = await _wallet.GetBalancesAsync(beneficiaryUserId, utcNow, cancellationToken);
        var availableLines = await LoadAvailableLinesAsync(beneficiaryUserId, utcNow, cancellationToken);

        // Available amount from linked lines (includes negative corrections).
        var availableFromLines = decimal.Round(
            availableLines.Sum(l => l.AmountExVat),
            2,
            MidpointRounding.AwayFromZero);

        DateOnly? ibanHold = null;
        if (profile?.IbanPayoutHoldUntilUtc is DateTime holdUtc && holdUtc > utcNow)
        {
            ibanHold = DateOnly.FromDateTime(SalesClock.ToLocal(holdUtc).DateTime);
        }

        var treatment = profile?.VatTreatment ?? SalesManagerVatTreatment.Standard21;
        var masked = string.IsNullOrWhiteSpace(profile?.Iban) ? "—" : Iban.Mask(profile.Iban);

        return new PayoutContext(
            today,
            minimum,
            holdDays,
            profile,
            consent?.AcceptedAtUtc,
            consent?.Version,
            open,
            availableFromLines,
            balances.Pending,
            availableLines,
            treatment,
            masked,
            ibanHold);
    }

    private async Task<List<CommissionLedgerEntry>> LoadAvailableLinesAsync(
        Guid beneficiaryUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var entries = await _db.CommissionLedgerEntries
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && e.SalesPayoutRequestId == null
                        && e.SelfBillingInvoiceId == null
                        && e.Kind != CommissionEntryKind.Payout)
            .ToListAsync(cancellationToken);

        var available = new List<CommissionLedgerEntry>();
        foreach (var e in entries)
        {
            var state = _wallet.DeriveState(e, utcNow, null, null);
            if (state == CommissionEntryState.Available)
            {
                available.Add(e);
            }
        }

        return available;
    }

    private List<SalesPayoutBlockerDto> BuildBlockers(PayoutContext ctx, bool mfaSatisfied)
    {
        var blockers = new List<SalesPayoutBlockerDto>();
        var available = decimal.Round(ctx.Available, 2, MidpointRounding.AwayFromZero);

        if (available < ctx.Minimum)
        {
            blockers.Add(new SalesPayoutBlockerDto(
                "below_minimum",
                "Sales.Payout.Block.BelowMinimum",
                null,
                SalesMoney.FormatPlain(available)));
        }

        if (ctx.ConsentAt is null)
        {
            blockers.Add(new SalesPayoutBlockerDto(
                "no_consent",
                "Sales.Payout.Block.NoConsent",
                "/sales/profiel"));
        }

        if (!IsProfileComplete(ctx.Profile))
        {
            blockers.Add(new SalesPayoutBlockerDto(
                "incomplete_profile",
                "Sales.Payout.Block.IncompleteProfile",
                "/sales/profiel"));
        }

        if (ctx.OpenRequest is not null)
        {
            blockers.Add(new SalesPayoutBlockerDto(
                "open_request",
                "Sales.Payout.Block.OpenRequest",
                "/sales/wallet?tab=uitbetalingen"));
        }

        if (!mfaSatisfied)
        {
            blockers.Add(new SalesPayoutBlockerDto(
                "mfa",
                "Sales.Payout.Block.Mfa",
                "/account/beveiliging"));
        }

        return blockers;
    }

    private static bool IsProfileComplete(SalesManagerProfile? profile)
    {
        if (profile is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(profile.Iban)
            || string.IsNullOrWhiteSpace(profile.PayoutAccountHolderName)
            || string.IsNullOrWhiteSpace(profile.CompanyName)
            || string.IsNullOrWhiteSpace(profile.KvkNumber)
            || string.IsNullOrWhiteSpace(profile.Address)
            || string.IsNullOrWhiteSpace(profile.PostalCode)
            || string.IsNullOrWhiteSpace(profile.City))
        {
            return false;
        }

        if (profile.VatTreatment == SalesManagerVatTreatment.Standard21
            && string.IsNullOrWhiteSpace(profile.VatNumber))
        {
            return false;
        }

        return true;
    }

    internal static DateOnly ResolveExpectedRunDate(DateOnly today, DateOnly? ibanHoldUntil)
    {
        var thisMonth = SalesClock.FirstWorkdayOfMonth(today.Year, today.Month);
        DateOnly run;
        if (today <= thisMonth)
        {
            run = thisMonth;
        }
        else
        {
            var next = today.AddMonths(1);
            run = SalesClock.FirstWorkdayOfMonth(next.Year, next.Month);
        }

        if (ibanHoldUntil is DateOnly hold && hold > run)
        {
            return SalesClock.NextWorkday(hold);
        }

        return run;
    }

    private async Task<SalesPayoutRequest?> FindOpenRequestAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken)
    {
        return await _db.SalesPayoutRequests
            .AsNoTracking()
            .Where(r => r.BeneficiaryUserId == beneficiaryUserId
                        && (r.Status == SalesPayoutRequestStatus.Requested
                            || r.Status == SalesPayoutRequestStatus.InRun
                            || r.Status == SalesPayoutRequestStatus.Approved))
            .OrderByDescending(r => r.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SalesPayoutRequestDto> MapRequestAsync(
        SalesPayoutRequest request,
        CancellationToken cancellationToken)
    {
        string? invoiceNumber = null;
        if (request.SelfBillingInvoiceId is Guid invId)
        {
            invoiceNumber = await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => i.Id == invId)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new SalesPayoutRequestDto
        {
            Id = request.Id,
            AmountExVat = request.AmountExVat,
            VatAmount = request.VatAmount,
            TotalInclVat = request.TotalInclVat,
            VatTreatment = request.VatTreatment.ToString(),
            MaskedIban = request.MaskedIban,
            Status = request.Status.ToString(),
            RequestedAtUtc = request.RequestedAtUtc,
            SelfBillingInvoiceId = request.SelfBillingInvoiceId,
            InvoiceNumber = invoiceNumber,
            RejectionReason = request.RejectionReason,
            DecidedAtUtc = request.DecidedAtUtc,
            PaidAtUtc = request.PaidAtUtc
        };
    }

    private async Task SendRequestedMailAsync(
        Guid beneficiaryUserId,
        DateOnly runDate,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == beneficiaryUserId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var prefs = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == beneficiaryUserId)
            .Select(p => p.EmailPrefsJson)
            .FirstOrDefaultAsync(cancellationToken);
        var parsed = SalesEmailPrefs.Parse(prefs);
        if (!parsed.PayoutStatus)
        {
            return;
        }

        var dateLabel = runDate.ToString("d MMMM yyyy", Nl);
        var subject = "We hebben je uitbetalingsaanvraag";
        var html =
            $"<p>Hallo {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>"
            + $"<p>We hebben je aanvraag. Lobsy keurt uitbetalingen goed op {System.Net.WebUtility.HtmlEncode(dateLabel)}.</p>"
            + "<p>Je volgt de status in Wallet &amp; uitbetalingen.</p>";
        {
            var __features = await _features.GetAsync(cancellationToken);
            var __plain = System.Text.RegularExpressions.Regex.Replace(html ?? string.Empty, "<[^>]+>", " ");
            __plain = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(__plain, @"\s+", " ")).Trim();
            var __pre = __plain.Length > 90 ? __plain[..90] : __plain;
            if (string.Equals(__pre, subject, StringComparison.Ordinal)) __pre = "Bericht van Lobsy";
            var __mail = TransactionalEmails.AdHoc("SalesMail.PayoutRequested", "SalesMail.PayoutRequested", EmailKind.Essential, __features.PublicWebBaseUrl, subject, __pre, subject,
                [new ParagraphBlock(EmailText.Plain(__plain))]);
            await _mailer.SendAsync(__mail, user.Email, cancellationToken: cancellationToken);
        }
    }

    private static string FormatAddress(SalesManagerProfile profile) =>
        string.Join(", ", new[]
        {
            profile.Address,
            $"{profile.PostalCode} {profile.City}".Trim(),
            profile.Country
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private sealed record PayoutContext(
        DateOnly Today,
        decimal Minimum,
        int HoldDays,
        SalesManagerProfile? Profile,
        DateTime? ConsentAt,
        string? ConsentVersion,
        SalesPayoutRequest? OpenRequest,
        decimal Available,
        decimal Pending,
        List<CommissionLedgerEntry> AvailableLines,
        SalesManagerVatTreatment VatTreatment,
        string MaskedIban,
        DateOnly? IbanHoldUntilLocal);
}
