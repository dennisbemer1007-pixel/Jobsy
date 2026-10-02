using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesPayoutRunService : ISalesPayoutRunService
{
    private readonly JobsyDbContext _db;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly ISalesPayoutProvider _provider;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly IPersonalDataAccessLogger _accessLog;
    private readonly SalesPayoutProviderOptions _options;

    public SalesPayoutRunService(
        JobsyDbContext db,
        ISelfBillingInvoiceService invoices,
        ISalesPayoutProvider provider,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        IPersonalDataAccessLogger accessLog,
        IOptions<SalesPayoutProviderOptions> options)
    {
        _db = db;
        _invoices = invoices;
        _provider = provider;
        _mailer = mailer;
        _features = features;
        _accessLog = accessLog;
        _options = options.Value;
    }

    public async Task<SalesPayoutRun?> TryCreateScheduledRunAsync(
        DateOnly runDate,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.SalesPayoutRuns.AsNoTracking()
            .AnyAsync(r => r.RunDate == runDate && !r.IsExtra, cancellationToken);
        if (exists)
        {
            return null;
        }

        try
        {
            return await CreateRunInternalAsync(
                runDate,
                isExtra: false,
                createdByUserId: null,
                utcNow,
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique index conflict — another instance won the race.
            return null;
        }
    }

    public async Task<SalesPayoutRunDto> CreateExtraRunAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var run = await CreateRunInternalAsync(
            SalesClock.Today(utcNow),
            isExtra: true,
            createdByUserId: adminUserId,
            utcNow,
            cancellationToken);
        await AuditAsync("sales.payout.run.create-extra", adminUserId, new { run.Id, run.RunDate }, cancellationToken);
        return ToRunDto(run);
    }

    public async Task RejectLineAsync(
        Guid adminUserId,
        Guid runId,
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (reason ?? "").Trim();
        if (trimmed.Length is < 5 or > 500)
        {
            throw new ArgumentException("Reden moet tussen 5 en 500 tekens zijn.");
        }

        var run = await _db.SalesPayoutRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetaalronde niet gevonden.");

        if (run.Status is not SalesPayoutRunStatus.Draft)
        {
            throw new InvalidOperationException("Alleen regels in een concept-ronde kunnen worden afgewezen.");
        }

        var request = await _db.SalesPayoutRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.SalesPayoutRunId == runId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetalingsaanvraag niet gevonden in deze ronde.");

        if (request.Status != SalesPayoutRequestStatus.InRun)
        {
            throw new InvalidOperationException("Alleen regels met status In ronde kunnen worden afgewezen.");
        }

        await UnlinkLedgerLinesAsync(request.Id, cancellationToken);

        request.Status = SalesPayoutRequestStatus.Rejected;
        request.RejectionReason = trimmed;
        request.DecidedAtUtc = DateTime.UtcNow;
        request.DecidedByUserId = adminUserId;
        request.SalesPayoutRunId = runId; // keep history link
        await _db.SaveChangesAsync(cancellationToken);

        await AuditAsync("sales.payout.run.reject-line", adminUserId, new
        {
            runId,
            requestId,
            request.BeneficiaryUserId
        }, cancellationToken);

        await SendPayoutMailAsync(
            request.BeneficiaryUserId,
            "SalesMail.PayoutRejected",
            "Je uitbetalingsaanvraag is afgewezen",
            $"<p>Je uitbetalingsaanvraag is afgewezen.</p><p>Reden: {System.Net.WebUtility.HtmlEncode(trimmed)}</p>"
            + "<p>Je commissie is weer beschikbaar in je wallet.</p>",
            respectPreference: false,
            cancellationToken);
    }

    public async Task<SalesPayoutRunDto> ApproveRunAsync(
        Guid adminUserId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.SalesPayoutRuns
            .Include(r => r.Requests)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetaalronde niet gevonden.");

        if (run.Status != SalesPayoutRunStatus.Draft)
        {
            throw new InvalidOperationException("Alleen een concept-ronde kan worden goedgekeurd.");
        }

        var utcNow = DateTime.UtcNow;
        var inRun = run.Requests.Where(r => r.Status == SalesPayoutRequestStatus.InRun).ToList();
        var approvedCount = 0;

        foreach (var request in inRun)
        {
            var flags = await ComputeFlagsAsync(request, utcNow, cancellationToken);
            if (flags.BalanceTooLow)
            {
                await DeferRequestAsync(
                    request,
                    "Saldo te laag",
                    "SalesMail.PayoutDeferredMissingConsent",
                    "Je uitbetalingsaanvraag wacht op de volgende ronde",
                    "<p>Je uitbetalingsaanvraag kon nog niet worden goedgekeurd omdat het saldo te laag is.</p>",
                    cancellationToken);
                continue;
            }

            if (flags.NewIban)
            {
                var holdLabel = flags.IbanHoldUntilLocal?.ToString("dd-MM-yyyy") ?? "—";
                await DeferRequestAsync(
                    request,
                    $"Nieuwe rekening: wacht tot {holdLabel}",
                    "SalesMail.PayoutDeferredIbanHold",
                    "Je uitbetaling wacht op je nieuwe rekening",
                    $"<p>Je uitbetaling wacht tot {System.Net.WebUtility.HtmlEncode(holdLabel)} vanwege een recente wijziging van je uitbetaalrekening.</p>",
                    cancellationToken);
                continue;
            }

            if (flags.NoConsent || flags.IncompleteProfile)
            {
                var why = flags.NoConsent ? "geen self-billing toestemming" : "onvolledig profiel";
                await DeferRequestAsync(
                    request,
                    why,
                    "SalesMail.PayoutDeferredMissingConsent",
                    "Je uitbetalingsaanvraag is uitgesteld",
                    $"<p>Je uitbetalingsaanvraag kon nog niet worden goedgekeurd ({System.Net.WebUtility.HtmlEncode(why)}). Rond je gegevens af in Profiel &amp; gegevens.</p>",
                    cancellationToken);
                continue;
            }

            var consent = await _db.SalesSelfBillingConsents
                .Where(c => c.UserId == request.BeneficiaryUserId
                            && c.Version == SalesSelfBilling.CurrentVersion
                            && c.RevokedAtUtc == null)
                .OrderByDescending(c => c.AcceptedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Toestemming ontbreekt onverwacht.");

            var invoice = await _invoices.IssueForRequestAsync(request, consent, cancellationToken);
            request.Status = SalesPayoutRequestStatus.Approved;
            request.SelfBillingInvoiceId = invoice.Id;
            request.DecidedAtUtc = utcNow;
            request.DecidedByUserId = adminUserId;
            request.RejectionReason = null;
            approvedCount++;

            await SendPayoutMailAsync(
                request.BeneficiaryUserId,
                "SalesMail.PayoutApproved",
                "Je uitbetaling is goedgekeurd",
                $"<p>Je uitbetaling is goedgekeurd. Je krijgt {System.Net.WebUtility.HtmlEncode(SalesMoney.FormatPlain(request.TotalInclVat))} binnen 3 werkdagen.</p>",
                respectPreference: true,
                cancellationToken);
        }

        if (approvedCount == 0 && inRun.Count > 0)
        {
            // All deferred — keep Draft so admin can retry after fixes, or close empty.
            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync("sales.payout.run.approve", adminUserId, new
            {
                runId,
                approved = 0,
                deferred = inRun.Count
            }, cancellationToken);
            return ToRunDto(run);
        }

        run.Status = SalesPayoutRunStatus.Approved;
        run.ApprovedAtUtc = utcNow;
        run.ApprovedByUserId = adminUserId;
        await _db.SaveChangesAsync(cancellationToken);

        await AuditAsync("sales.payout.run.approve", adminUserId, new
        {
            runId,
            approved = approvedCount
        }, cancellationToken);

        return ToRunDto(run);
    }

    public async Task<SalesPayoutExportResult> ExportAsync(
        Guid adminUserId,
        Guid runId,
        string format,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.SalesPayoutRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetaalronde niet gevonden.");

        if (run.Status is not (SalesPayoutRunStatus.Approved or SalesPayoutRunStatus.Exported))
        {
            throw new InvalidOperationException("Export is alleen mogelijk na goedkeuring.");
        }

        var fmt = (format ?? "sepa").Trim().ToLowerInvariant();
        if (fmt is "sepa" or "xml" or "pain")
        {
            if (!_provider.IsConfigured)
            {
                throw new InvalidOperationException(_provider.NotConfiguredMessage ?? "Export niet geconfigureerd.");
            }
        }

        var lines = await BuildExportLinesAsync(runId, cancellationToken);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Geen goedgekeurde regels om te exporteren.");
        }

        var file = await _provider.ExportAsync(run, lines, fmt, cancellationToken);
        var sha = BankTransferPayoutProvider.Sha256Hex(file.Bytes);

        run.Status = SalesPayoutRunStatus.Exported;
        run.ExportedAtUtc = DateTime.UtcNow;
        run.ExportFileSha256 = sha;
        run.ProviderKey = _provider.Key;
        await _db.SaveChangesAsync(cancellationToken);

        await _accessLog.LogAsync(new PersonalDataAccessEntry(
            ActorUserId: adminUserId,
            ActorRole: "Admin",
            Resource: "sales.payout-account",
            Action: "export",
            Reason: $"payout-run:{runId:D}"), cancellationToken);

        await AuditAsync("sales.payout.run.export", adminUserId, new
        {
            runId,
            format = fmt,
            sha,
            lineCount = lines.Count
        }, cancellationToken);

        return new SalesPayoutExportResult(file.FileName, file.ContentType, file.Bytes, sha, AccessLogged: true);
    }

    public async Task<SalesPayoutRunDto> MarkPaidAsync(
        Guid adminUserId,
        Guid runId,
        IReadOnlyList<Guid>? invoiceIds,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.SalesPayoutRuns
            .Include(r => r.Requests)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetaalronde niet gevonden.");

        if (run.Status is not (SalesPayoutRunStatus.Approved or SalesPayoutRunStatus.Exported or SalesPayoutRunStatus.Paid))
        {
            throw new InvalidOperationException("Markeer als betaald is alleen mogelijk na goedkeuring of export.");
        }

        var approved = run.Requests
            .Where(r => r.Status == SalesPayoutRequestStatus.Approved && r.SelfBillingInvoiceId is not null)
            .ToList();

        IEnumerable<SalesPayoutRequest> toPay = approved;
        if (invoiceIds is { Count: > 0 })
        {
            var set = invoiceIds.ToHashSet();
            toPay = approved.Where(r => r.SelfBillingInvoiceId is Guid id && set.Contains(id));
        }

        var paidNow = 0;
        foreach (var request in toPay)
        {
            var invoiceId = request.SelfBillingInvoiceId!.Value;
            await _invoices.MarkPaidAsync(invoiceId, cancellationToken);
            paidNow++;

            await SendPayoutMailAsync(
                request.BeneficiaryUserId,
                "SalesMail.PayoutPaid",
                "Je uitbetaling is betaald",
                "<p>Je uitbetaling is betaald. Je ziet de factuur in Wallet &amp; uitbetalingen.</p>",
                respectPreference: true,
                cancellationToken);
        }

        // CloseRequestForPaidInvoiceAsync is invoked from MarkPaidAsync hook.
        await _db.Entry(run).Collection(r => r.Requests).LoadAsync(cancellationToken);
        var stillOpen = run.Requests.Any(r =>
            r.Status is SalesPayoutRequestStatus.Approved or SalesPayoutRequestStatus.InRun);
        if (!stillOpen && run.Requests.Any(r => r.Status == SalesPayoutRequestStatus.Paid))
        {
            run.Status = SalesPayoutRunStatus.Closed;
        }
        else if (run.Requests.Any(r => r.Status == SalesPayoutRequestStatus.Paid))
        {
            run.Status = SalesPayoutRunStatus.Paid;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await AuditAsync("sales.payout.mark-paid", adminUserId, new
        {
            runId,
            paidNow
        }, cancellationToken);

        return ToRunDto(run);
    }

    public async Task CloseRequestForPaidInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _db.SelfBillingInvoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        SalesPayoutRequest? request = null;
        if (invoice.SalesPayoutRequestId is Guid rid)
        {
            request = await _db.SalesPayoutRequests
                .FirstOrDefaultAsync(r => r.Id == rid, cancellationToken);
        }

        request ??= await _db.SalesPayoutRequests
            .FirstOrDefaultAsync(r => r.SelfBillingInvoiceId == invoiceId, cancellationToken);

        if (request is null)
        {
            return;
        }

        if (request.Status == SalesPayoutRequestStatus.Paid)
        {
            return;
        }

        request.Status = SalesPayoutRequestStatus.Paid;
        request.PaidAtUtc ??= invoice.PaidAt ?? DateTime.UtcNow;
        request.SelfBillingInvoiceId ??= invoiceId;
        await _db.SaveChangesAsync(cancellationToken);

        if (request.SalesPayoutRunId is Guid runId)
        {
            var run = await _db.SalesPayoutRuns
                .Include(r => r.Requests)
                .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
            if (run is null)
            {
                return;
            }

            var anyApprovedOrInRun = run.Requests.Any(r =>
                r.Status is SalesPayoutRequestStatus.Approved or SalesPayoutRequestStatus.InRun);
            if (!anyApprovedOrInRun && run.Requests.Any(r => r.Status == SalesPayoutRequestStatus.Paid))
            {
                run.Status = SalesPayoutRunStatus.Closed;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (run.Status is SalesPayoutRunStatus.Approved or SalesPayoutRunStatus.Exported)
            {
                run.Status = SalesPayoutRunStatus.Paid;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<SalesPayoutRunListItemDto>> ListRunsAsync(
        CancellationToken cancellationToken = default)
    {
        var runs = await _db.SalesPayoutRuns.AsNoTracking()
            .OrderByDescending(r => r.RunDate)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var adminIds = runs.Where(r => r.ApprovedByUserId is not null)
            .Select(r => r.ApprovedByUserId!.Value).Distinct().ToList();
        var names = adminIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => adminIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? "", cancellationToken);

        var result = new List<SalesPayoutRunListItemDto>();
        foreach (var run in runs)
        {
            var lines = await _db.SalesPayoutRequests.AsNoTracking()
                .Where(r => r.SalesPayoutRunId == run.Id
                            && r.Status != SalesPayoutRequestStatus.Rejected
                            && r.Status != SalesPayoutRequestStatus.Cancelled)
                .Select(r => new { r.TotalInclVat })
                .ToListAsync(cancellationToken);

            string? approvedBy = null;
            if (run.ApprovedByUserId is Guid aid && names.TryGetValue(aid, out var n))
            {
                approvedBy = PersonalDataMasker.MaskName(n);
            }

            result.Add(new SalesPayoutRunListItemDto(
                run.Id,
                run.RunDate,
                run.IsExtra,
                run.Status.ToString(),
                lines.Count,
                lines.Sum(l => l.TotalInclVat),
                approvedBy,
                run.CreatedAtUtc,
                run.ApprovedAtUtc,
                run.ExportedAtUtc,
                run.Status is SalesPayoutRunStatus.Paid or SalesPayoutRunStatus.Closed
                    ? run.ExportedAtUtc ?? run.ApprovedAtUtc
                    : null));
        }

        return result;
    }

    public async Task<SalesPayoutRunDetailDto> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.SalesPayoutRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetaalronde niet gevonden.");

        var requests = await _db.SalesPayoutRequests.AsNoTracking()
            .Where(r => r.SalesPayoutRunId == runId)
            .OrderBy(r => r.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var userIds = requests.Select(r => r.BeneficiaryUserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Role })
            .ToListAsync(cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var invoiceIds = requests.Where(r => r.SelfBillingInvoiceId is not null)
            .Select(r => r.SelfBillingInvoiceId!.Value).ToList();
        var invoices = invoiceIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, cancellationToken);

        string? approvedBy = null;
        if (run.ApprovedByUserId is Guid aid)
        {
            var name = await _db.Users.AsNoTracking()
                .Where(u => u.Id == aid)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            approvedBy = PersonalDataMasker.MaskName(name);
        }

        var utcNow = DateTime.UtcNow;
        var lineDtos = new List<SalesPayoutRunLineDto>();
        foreach (var req in requests)
        {
            userMap.TryGetValue(req.BeneficiaryUserId, out var user);
            var flags = await ComputeFlagsAsync(req, utcNow, cancellationToken);
            string? invoiceNumber = null;
            if (req.SelfBillingInvoiceId is Guid iid)
            {
                invoices.TryGetValue(iid, out invoiceNumber);
            }

            var flagNote = flags.BalanceTooLow ? "Saldo te laag"
                : flags.NewIban ? $"Nieuwe rekening: wacht tot {flags.IbanHoldUntilLocal:dd-MM-yyyy}"
                : flags.NoConsent ? "Geen toestemming"
                : flags.IncompleteProfile ? "Profiel onvolledig"
                : null;

            lineDtos.Add(new SalesPayoutRunLineDto(
                req.Id,
                req.BeneficiaryUserId,
                PersonalDataMasker.MaskName(user?.FullName),
                user?.Role.ToString() ?? "SalesManager",
                req.AmountExVat,
                req.VatAmount,
                req.TotalInclVat,
                req.VatTreatment.ToString(),
                req.MaskedIban,
                req.Status.ToString(),
                req.SelfBillingInvoiceId,
                invoiceNumber,
                flags.NewIban,
                flags.NoConsent,
                flags.BalanceTooLow,
                flags.IncompleteProfile,
                flagNote,
                req.RejectionReason));
        }

        var nextStep = run.Status switch
        {
            SalesPayoutRunStatus.Draft => "SalesAdmin.Runs.Next.Approve",
            SalesPayoutRunStatus.Approved => "SalesAdmin.Runs.Next.Export",
            SalesPayoutRunStatus.Exported => "SalesAdmin.Runs.Next.MarkPaid",
            SalesPayoutRunStatus.Paid => "SalesAdmin.Runs.Next.Closing",
            _ => "SalesAdmin.Runs.Next.Done"
        };

        return new SalesPayoutRunDetailDto(
            run.Id,
            run.RunDate,
            run.IsExtra,
            run.Status.ToString(),
            run.ProviderKey,
            run.ExportFileSha256,
            _provider.IsConfigured,
            _provider.NotConfiguredMessage,
            approvedBy,
            run.CreatedAtUtc,
            run.ApprovedAtUtc,
            run.ExportedAtUtc,
            lineDtos,
            nextStep);
    }

    private async Task<SalesPayoutRun> CreateRunInternalAsync(
        DateOnly runDate,
        bool isExtra,
        Guid? createdByUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var ambassadorsEnabled = features.AmbassadorsEnabled;

        var candidateRequests = await _db.SalesPayoutRequests
            .Where(r => r.Status == SalesPayoutRequestStatus.Requested
                        && r.RequestedAtUtc < utcNow)
            .ToListAsync(cancellationToken);

        if (!ambassadorsEnabled && candidateRequests.Count > 0)
        {
            var beneficiaryIds = candidateRequests.Select(r => r.BeneficiaryUserId).Distinct().ToList();
            var ambassadorIds = await _db.Users.AsNoTracking()
                .Where(u => beneficiaryIds.Contains(u.Id) && u.Role == UserRole.Ambassadeur)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            if (ambassadorIds.Count > 0)
            {
                var skip = ambassadorIds.ToHashSet();
                candidateRequests = candidateRequests.Where(r => !skip.Contains(r.BeneficiaryUserId)).ToList();
            }
        }

        var run = new SalesPayoutRun
        {
            Id = Guid.NewGuid(),
            RunDate = runDate,
            IsExtra = isExtra,
            Status = SalesPayoutRunStatus.Draft,
            CreatedAtUtc = utcNow,
            CreatedByUserId = createdByUserId,
            ProviderKey = string.IsNullOrWhiteSpace(_options.Provider) ? "bank-transfer" : _options.Provider
        };
        _db.SalesPayoutRuns.Add(run);

        foreach (var req in candidateRequests)
        {
            req.Status = SalesPayoutRequestStatus.InRun;
            req.SalesPayoutRunId = run.Id;
            req.RejectionReason = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return run;
    }

    private async Task DeferRequestAsync(
        SalesPayoutRequest request,
        string note,
        string mailCategory,
        string subject,
        string html,
        CancellationToken cancellationToken)
    {
        request.Status = SalesPayoutRequestStatus.Requested;
        request.SalesPayoutRunId = null;
        request.RejectionReason = note;
        await SendPayoutMailAsync(
            request.BeneficiaryUserId,
            mailCategory,
            subject,
            html,
            respectPreference: true,
            cancellationToken);
    }

    private async Task UnlinkLedgerLinesAsync(Guid requestId, CancellationToken cancellationToken)
    {
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

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<LineFlags> ComputeFlagsAsync(
        SalesPayoutRequest request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.BeneficiaryUserId, cancellationToken);

        DateOnly? holdLocal = null;
        var newIban = false;
        if (profile?.IbanPayoutHoldUntilUtc is DateTime holdUtc && holdUtc > utcNow)
        {
            newIban = true;
            holdLocal = DateOnly.FromDateTime(SalesClock.ToLocal(holdUtc).DateTime);
        }

        var consent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .AnyAsync(c => c.UserId == request.BeneficiaryUserId
                           && c.Version == SalesSelfBilling.CurrentVersion
                           && c.RevokedAtUtc == null, cancellationToken);

        var incomplete = profile is null
            || string.IsNullOrWhiteSpace(profile.Iban)
            || string.IsNullOrWhiteSpace(profile.PayoutAccountHolderName)
            || string.IsNullOrWhiteSpace(profile.CompanyName)
            || string.IsNullOrWhiteSpace(profile.KvkNumber)
            || (profile.VatTreatment != SalesManagerVatTreatment.SmallBusinessScheme
                && string.IsNullOrWhiteSpace(profile.VatNumber));

        var balanceTooLow = request.AmountExVat <= 0m;

        return new LineFlags(newIban, !consent, incomplete, balanceTooLow, holdLocal);
    }

    private async Task<List<SalesPayoutExportLine>> BuildExportLinesAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var requests = await _db.SalesPayoutRequests.AsNoTracking()
            .Where(r => r.SalesPayoutRunId == runId
                        && r.Status == SalesPayoutRequestStatus.Approved
                        && r.SelfBillingInvoiceId != null)
            .ToListAsync(cancellationToken);

        var result = new List<SalesPayoutExportLine>();
        foreach (var req in requests)
        {
            var invoice = await _db.SelfBillingInvoices.AsNoTracking()
                .FirstAsync(i => i.Id == req.SelfBillingInvoiceId!.Value, cancellationToken);
            var profile = await _db.SalesManagerProfiles
                .FirstOrDefaultAsync(p => p.UserId == req.BeneficiaryUserId, cancellationToken)
                ?? throw new InvalidOperationException("Profiel ontbreekt voor SEPA-export.");

            // IBAN is decrypted in memory via the EF value converter — never returned by APIs.
            var fullIban = profile.Iban
                ?? throw new InvalidOperationException("IBAN ontbreekt voor SEPA-export.");
            var holder = !string.IsNullOrWhiteSpace(profile.PayoutAccountHolderName)
                ? profile.PayoutAccountHolderName.Trim()
                : profile.CompanyName?.Trim();
            if (string.IsNullOrWhiteSpace(holder))
            {
                throw new InvalidOperationException("Rekeninghouder ontbreekt voor SEPA-export.");
            }

            result.Add(new SalesPayoutExportLine(
                req.Id,
                invoice.Id,
                invoice.InvoiceNumber,
                holder,
                fullIban,
                req.MaskedIban,
                req.TotalInclVat,
                req.AmountExVat));
        }

        return result;
    }

    private async Task SendPayoutMailAsync(
        Guid beneficiaryUserId,
        string category,
        string subject,
        string html,
        bool respectPreference,
        CancellationToken cancellationToken)
    {
        if (respectPreference)
        {
            var prefsJson = await _db.SalesManagerProfiles.AsNoTracking()
                .Where(p => p.UserId == beneficiaryUserId)
                .Select(p => p.EmailPrefsJson)
                .FirstOrDefaultAsync(cancellationToken);
            if (!SalesEmailPrefs.Parse(prefsJson).PayoutStatus)
            {
                return;
            }
        }

        var email = await _db.Users.AsNoTracking()
            .Where(u => u.Id == beneficiaryUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var features = await _features.GetAsync(cancellationToken);
        var plain = System.Text.RegularExpressions.Regex.Replace(html ?? string.Empty, "<[^>]+>", " ");
        plain = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(plain, @"\s+", " ")).Trim();
        var mail = TransactionalEmails.AdHoc(
            category,
            category,
            EmailKind.Essential,
            features.PublicWebBaseUrl,
            subject,
            plain.Length > 80 ? plain[..80] : plain,
            subject,
            [new ParagraphBlock(EmailText.Plain(plain))],
            greeting: null);
        await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);
    }

    private async Task AuditAsync(
        string action,
        Guid adminUserId,
        object details,
        CancellationToken cancellationToken)
    {
        // Dependencies C absent: interim structured PlatformLog (no IAdminAuditLog yet).
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = action,
            Message = action,
            DetailsJson = JsonSerializer.Serialize(new
            {
                adminUserId,
                details
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SalesPayoutRunDto ToRunDto(SalesPayoutRun run)
        => new(
            run.Id,
            run.RunDate,
            run.IsExtra,
            run.Status.ToString(),
            run.Requests?.Count ?? 0,
            run.Requests?.Where(r => r.Status != SalesPayoutRequestStatus.Rejected
                                     && r.Status != SalesPayoutRequestStatus.Cancelled)
                .Sum(r => r.TotalInclVat) ?? 0m);

    private sealed record LineFlags(
        bool NewIban,
        bool NoConsent,
        bool IncompleteProfile,
        bool BalanceTooLow,
        DateOnly? IbanHoldUntilLocal);
}
