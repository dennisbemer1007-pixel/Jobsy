using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesEmployerReadService : ISalesEmployerReadService, ISalesEmployerPortalReadService
{
    private readonly JobsyDbContext _db;
    private readonly ISalesWalletReadService _wallet;
    private readonly ISalesBeneficiaryService _beneficiary;

    public SalesEmployerReadService(
        JobsyDbContext db,
        ISalesWalletReadService wallet,
        ISalesBeneficiaryService beneficiary)
    {
        _db = db;
        _wallet = wallet;
        _beneficiary = beneficiary;
    }

    public async Task<IReadOnlyList<SalesEmployerUnitDto>> ListUnitsAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildRowsAsync(beneficiaryUserId, DateTime.UtcNow, cancellationToken);
        return rows
            .Select(r => new SalesEmployerUnitDto(
                r.Dto.CompanyId,
                r.Dto.DisplayName,
                r.Dto.Place,
                r.AttributedAtUtc,
                r.CommissionStartsAtUtc,
                r.Dto.BranchCount,
                r.CommissionTotal))
            .OrderByDescending(u => u.AttributedAtUtc)
            .ThenBy(u => u.DisplayName)
            .ToList();
    }

    public async Task<SalesEmployerPageDto> ListAsync(
        Guid beneficiaryUserId,
        string? query,
        SalesEmployerStatus? status,
        int? commissionYear,
        int page,
        int pageSize = 25,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var rows = await BuildRowsAsync(beneficiaryUserId, now, cancellationToken);

        IEnumerable<EmployerRow> filtered = rows;
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            filtered = filtered.Where(r =>
                r.Dto.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (r.Dto.Place?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (status is not null)
        {
            filtered = filtered.Where(r => r.Dto.Status == status);
        }

        if (commissionYear is 1 or 2 or 3)
        {
            filtered = filtered.Where(r => r.Dto.CommissionYear == commissionYear);
        }
        else if (commissionYear == 0)
        {
            // "Afgelopen"
            filtered = filtered.Where(r => r.Dto.Status == SalesEmployerStatus.Ended);
        }

        var list = filtered
            .OrderByDescending(r => r.Dto.CommissionThisYear)
            .ThenBy(r => r.Dto.DisplayName)
            .ToList();

        var pageSafe = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = list.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        var items = list
            .Skip((pageSafe - 1) * size)
            .Take(size)
            .Select(r => r.Dto)
            .ToList();

        var active = rows.Count(r => r.Dto.Status == SalesEmployerStatus.Active);
        var firstPurchase = rows.Count(r => r.CommissionStartsAtUtc is not null);
        var quiet = rows.Count(r => r.Dto.Status == SalesEmployerStatus.Quiet);
        var avg = active == 0
            ? 0m
            : decimal.Round(
                rows.Where(r => r.Dto.Status == SalesEmployerStatus.Active)
                    .Sum(r => r.Dto.CommissionThisYear) / active,
                2,
                MidpointRounding.AwayFromZero);

        return new SalesEmployerPageDto
        {
            Items = items,
            Page = pageSafe,
            PageSize = size,
            TotalCount = total,
            TotalPages = totalPages,
            Kpis = new SalesEmployerListKpisDto
            {
                TotalEmployers = rows.Count,
                ActiveEmployers = active,
                FirstPurchaseCount = firstPurchase,
                QuietCount = quiet,
                AverageCommissionPerActive = avg
            }
        };
    }

    public async Task<SalesEmployerDetailDto?> GetDetailAsync(
        Guid beneficiaryUserId,
        Guid companyId,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (!await _beneficiary.CanSeeCompanyAsync(beneficiaryUserId, companyId, cancellationToken))
        {
            return null;
        }

        var rootId = await ResolveRootIdAsync(companyId, cancellationToken);
        var rows = await BuildRowsAsync(beneficiaryUserId, now, cancellationToken);
        var row = rows.FirstOrDefault(r => r.Dto.CompanyId == rootId);
        if (row is null)
        {
            return null;
        }

        var branchIds = row.BranchIds;
        var settings = await _db.SalesCommercialSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var year1Rate = row.DirectRate
            ?? settings?.DirectCommissionRate
            ?? SalesCommissionRules.DefaultDirectCommissionRate;
        var year2Rate = row.Year2Rate
            ?? settings?.Year2DirectCommissionRate
            ?? SalesCommissionRules.DefaultYear2DirectCommissionRate;
        var year3Rate = row.Year3Rate
            ?? settings?.Year3DirectCommissionRate
            ?? SalesCommissionRules.DefaultYear3DirectCommissionRate;
        var duration = row.DurationDays
            ?? settings?.CommissionDurationDays
            ?? SalesCommissionRules.DefaultCommissionDurationDays;

        var years = BuildYearSegments(row.CommissionStartsAtUtc, year1Rate, year2Rate, year3Rate, duration, now);
        var timeline = await BuildTimelineAsync(row, duration, cancellationToken);
        var lines = await BuildLinesAsync(beneficiaryUserId, branchIds, now, cancellationToken);

        return new SalesEmployerDetailDto
        {
            Row = row.Dto,
            CommissionTotal = row.CommissionTotal,
            PurchaseCount = lines.Count,
            Years = years,
            Timeline = timeline,
            Lines = lines
        };
    }

    private async Task<List<EmployerRow>> BuildRowsAsync(
        Guid beneficiaryUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var attributed = await _db.Companies.AsNoTracking()
            .Where(c => c.ReferredBySalesManagerUserId == beneficiaryUserId)
            .Select(c => new
            {
                c.Id,
                c.ParentCompanyId,
                c.Name,
                c.Address,
                c.LegalForm,
                c.SalesAttributedAtUtc,
                c.SalesAttributionSource,
                c.FirstYearStartedAt,
                c.CommissionStartsAtUtc,
                c.CommissionDirectRateSnapshot,
                c.CommissionYear2RateSnapshot,
                c.CommissionYear3RateSnapshot,
                c.CommissionDurationDaysSnapshot,
                c.PendingStartHighlightBonus
            })
            .ToListAsync(cancellationToken);

        if (attributed.Count == 0)
        {
            return [];
        }

        var byRoot = attributed
            .GroupBy(c => SalesCommercialUnit.RootIdOf(c.Id, c.ParentCompanyId))
            .ToList();
        var rootIds = byRoot.Select(g => g.Key).ToList();
        var roots = await _db.Companies.AsNoTracking()
            .Where(c => rootIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Address,
                c.LegalForm,
                c.SalesAttributedAtUtc,
                c.SalesAttributionSource,
                c.CommissionStartsAtUtc,
                c.CommissionDirectRateSnapshot,
                c.CommissionYear2RateSnapshot,
                c.CommissionYear3RateSnapshot,
                c.CommissionDurationDaysSnapshot,
                c.PendingStartHighlightBonus
            })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var allBranchIds = attributed.Select(a => a.Id).Concat(rootIds).Distinct().ToList();
        var ledger = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && e.CompanyId != null
                        && allBranchIds.Contains(e.CompanyId ?? Guid.Empty)
                        && e.Kind != CommissionEntryKind.Payout)
            .Select(e => new
            {
                e.CompanyId,
                e.AmountExVat,
                e.CreatedAt,
                e.Kind
            })
            .ToListAsync(cancellationToken);

        var localYear = SalesClock.Today(utcNow).Year;
        var result = new List<EmployerRow>();
        foreach (var group in byRoot)
        {
            roots.TryGetValue(group.Key, out var root);
            var sample = group.First();
            var displayName = root?.Name ?? sample.Name;
            var legalForm = root?.LegalForm ?? sample.LegalForm;
            var address = root?.Address ?? sample.Address;
            var place = SalesPlace.FromAddress(address, legalForm);
            var branchIds = group.Select(g => g.Id).Append(group.Key).ToHashSet();
            // Count attributed children (not the root itself).
            var branchCount = group.Count(g => g.Id != group.Key);

            var companyLines = ledger.Where(l => l.CompanyId is Guid cid && branchIds.Contains(cid)).ToList();
            var commissionTotal = companyLines.Sum(l => l.AmountExVat);
            var commissionThisYear = companyLines
                .Where(l => SalesClock.Today(l.CreatedAt).Year == localYear && l.AmountExVat > 0)
                .Sum(l => l.AmountExVat);
            var lastPurchase = companyLines
                .Where(l => l.Kind is CommissionEntryKind.TokenCommission
                    or CommissionEntryKind.IndirectTokenCommission
                    or CommissionEntryKind.FounderBonus)
                .Select(l => (DateTime?)l.CreatedAt)
                .DefaultIfEmpty(null)
                .Max();

            var attributedAt = root?.SalesAttributedAtUtc
                ?? sample.SalesAttributedAtUtc
                ?? sample.FirstYearStartedAt
                ?? utcNow;
            var startsAt = root?.CommissionStartsAtUtc
                ?? group.Select(g => g.CommissionStartsAtUtc).FirstOrDefault(d => d is not null);
            var source = root?.SalesAttributionSource ?? sample.SalesAttributionSource;
            var direct = root?.CommissionDirectRateSnapshot ?? sample.CommissionDirectRateSnapshot;
            var y2 = root?.CommissionYear2RateSnapshot ?? sample.CommissionYear2RateSnapshot;
            var y3 = root?.CommissionYear3RateSnapshot ?? sample.CommissionYear3RateSnapshot;
            var duration = root?.CommissionDurationDaysSnapshot
                ?? sample.CommissionDurationDaysSnapshot
                ?? SalesCommissionRules.DefaultCommissionDurationDays;

            var (status, statusLabelKey, quietDays, year, rate, progress) = DeriveStatus(
                startsAt,
                lastPurchase,
                direct,
                y2,
                y3,
                duration,
                utcNow);

            result.Add(new EmployerRow(
                new SalesEmployerDto
                {
                    CompanyId = group.Key,
                    DisplayName = displayName,
                    Place = place,
                    BranchCount = branchCount,
                    AttributedOn = DateOnly.FromDateTime(SalesClock.ToLocal(attributedAt).DateTime),
                    Source = source is null ? "" : SalesLabels.Key(source.Value),
                    Status = status,
                    StatusLabelKey = statusLabelKey,
                    QuietDays = quietDays,
                    CommissionYear = year,
                    CurrentRate = rate,
                    YearProgress = progress,
                    CommissionThisYear = commissionThisYear,
                    LastPurchaseOn = lastPurchase is null
                        ? null
                        : DateOnly.FromDateTime(SalesClock.ToLocal(lastPurchase.Value).DateTime)
                },
                branchIds,
                attributedAt,
                startsAt,
                commissionTotal,
                direct,
                y2,
                y3,
                duration,
                root?.PendingStartHighlightBonus == true
                    || group.Any(g => g.PendingStartHighlightBonus),
                source));
        }

        return result;
    }

    internal static (SalesEmployerStatus Status, string StatusLabelKey, int? QuietDays, int? Year, decimal? Rate, double Progress)
        DeriveStatus(
            DateTime? startsAt,
            DateTime? lastPurchaseUtc,
            decimal? year1,
            decimal? year2,
            decimal? year3,
            int durationDays,
            DateTime utcNow)
    {
        if (startsAt is null)
        {
            return (SalesEmployerStatus.NoPurchase, SalesLabels.Key(SalesEmployerStatus.NoPurchase), null, null, null, 0);
        }

        var terms = new SalesCommissionRules.CommissionTerms(
            year1 ?? SalesCommissionRules.DefaultDirectCommissionRate,
            year2 ?? SalesCommissionRules.DefaultYear2DirectCommissionRate,
            year3 ?? SalesCommissionRules.DefaultYear3DirectCommissionRate,
            0m,
            durationDays,
            startsAt.Value);
        var year = SalesCommissionRules.YearFor(terms, utcNow);
        if (year is null)
        {
            return (SalesEmployerStatus.Ended, SalesLabels.Key(SalesEmployerStatus.Ended), null, null, null, 1);
        }

        var rate = SalesCommissionRules.DirectRate(terms, year);
        var yearStart = startsAt.Value.AddDays(SalesCommissionRules.CommissionYearLengthDays * (year.Value - 1));
        var yearEnd = year.Value == 3
            ? startsAt.Value.AddDays(durationDays)
            : yearStart.AddDays(SalesCommissionRules.CommissionYearLengthDays);
        var progress = Math.Clamp(
            (utcNow - yearStart).TotalDays / Math.Max(1, (yearEnd - yearStart).TotalDays),
            0,
            1);

        if (lastPurchaseUtc is null || lastPurchaseUtc < utcNow.AddDays(-90))
        {
            var quietDays = lastPurchaseUtc is null
                ? (int)(utcNow - startsAt.Value).TotalDays
                : (int)(utcNow - lastPurchaseUtc.Value).TotalDays;
            return (SalesEmployerStatus.Quiet, SalesLabels.Key(SalesEmployerStatus.Quiet), quietDays, year, rate, progress);
        }

        return (SalesEmployerStatus.Active, SalesLabels.Key(SalesEmployerStatus.Active), null, year, rate, progress);
    }

    private static IReadOnlyList<SalesEmployerYearSegmentDto> BuildYearSegments(
        DateTime? startsAt,
        decimal y1,
        decimal y2,
        decimal y3,
        int durationDays,
        DateTime utcNow)
    {
        if (startsAt is null)
        {
            return [];
        }

        var start = startsAt.Value;
        var segments = new List<SalesEmployerYearSegmentDto>();
        for (var year = 1; year <= 3; year++)
        {
            var segStart = start.AddDays(SalesCommissionRules.CommissionYearLengthDays * (year - 1));
            var segEnd = year == 3
                ? start.AddDays(durationDays)
                : segStart.AddDays(SalesCommissionRules.CommissionYearLengthDays);
            var rate = year switch { 1 => y1, 2 => y2, _ => y3 };
            var isCurrent = utcNow >= segStart && utcNow < segEnd;
            var progress = isCurrent
                ? Math.Clamp((utcNow - segStart).TotalDays / Math.Max(1, (segEnd - segStart).TotalDays), 0, 1)
                : utcNow >= segEnd ? 1 : 0;
            segments.Add(new SalesEmployerYearSegmentDto
            {
                Year = year,
                Start = DateOnly.FromDateTime(SalesClock.ToLocal(segStart).DateTime),
                End = DateOnly.FromDateTime(SalesClock.ToLocal(segEnd.AddSeconds(-1)).DateTime),
                Rate = rate,
                IsCurrent = isCurrent,
                Progress = progress
            });
        }

        return segments;
    }

    private async Task<IReadOnlyList<SalesEmployerTimelineItemDto>> BuildTimelineAsync(
        EmployerRow row,
        int durationDays,
        CancellationToken cancellationToken)
    {
        var items = new List<SalesEmployerTimelineItemDto>();
        var sourceKey = row.Source is null ? "Sales.Label.Attribution.Legacy" : SalesLabels.Key(row.Source.Value);
        items.Add(new SalesEmployerTimelineItemDto
        {
            Key = "attributed",
            Label = sourceKey,
            On = DateOnly.FromDateTime(SalesClock.ToLocal(row.AttributedAtUtc).DateTime),
            IsFuture = false
        });

        if (!row.PendingStartHighlight && row.CommissionStartsAtUtc is not null)
        {
            // Start-highlight was consumed if Pending is false and they had attribution —
            // only show when we can confirm consumption via absence of pending after attribution.
            // Spec: show when PendingStartHighlightBonus was consumed.
            // If still pending, skip; if never had bonus, skip. Detect via token transactions note if available.
        }

        // Show start-highlight only when it was pending and is now cleared, approximated by
        // having CommissionStartsAtUtc and PendingStartHighlight == false after attribution.
        // Safer: only include when PendingStartHighlight was true historically — we only know current.
        // Spec says "if PendingStartHighlightBonus was consumed" — we check false after attribution + first purchase.
        if (!row.PendingStartHighlight && row.CommissionStartsAtUtc is not null)
        {
            var highlightHit = await _db.TokenTransactions.AsNoTracking()
                .AnyAsync(
                    t => row.BranchIds.Contains(t.CompanyId)
                         && t.Note != null
                         && (t.Note.Contains("start-highlight") || t.Note.Contains("StartHighlight")),
                    cancellationToken);
            if (highlightHit)
            {
                items.Add(new SalesEmployerTimelineItemDto
                {
                    Key = "start-highlight",
                    Label = "Sales.Timeline.StartHighlight",
                    On = DateOnly.FromDateTime(SalesClock.ToLocal(row.CommissionStartsAtUtc.Value).DateTime),
                    IsFuture = false
                });
            }
        }

        if (row.CommissionStartsAtUtc is not null)
        {
            var start = row.CommissionStartsAtUtc.Value;
            items.Add(new SalesEmployerTimelineItemDto
            {
                Key = "first-purchase",
                Label = "Sales.Timeline.FirstPurchase",
                On = DateOnly.FromDateTime(SalesClock.ToLocal(start).DateTime),
                IsFuture = false
            });

            var y2Start = start.AddDays(SalesCommissionRules.CommissionYearLengthDays);
            var y3Start = start.AddDays(SalesCommissionRules.CommissionYearLengthDays * 2);
            var end = start.AddDays(durationDays);
            var now = DateTime.UtcNow;
            items.Add(new SalesEmployerTimelineItemDto
            {
                Key = "year2",
                Label = "Sales.Timeline.Year2",
                On = DateOnly.FromDateTime(SalesClock.ToLocal(y2Start).DateTime),
                IsFuture = now < y2Start
            });
            items.Add(new SalesEmployerTimelineItemDto
            {
                Key = "year3",
                Label = "Sales.Timeline.Year3",
                On = DateOnly.FromDateTime(SalesClock.ToLocal(y3Start).DateTime),
                IsFuture = now < y3Start
            });
            items.Add(new SalesEmployerTimelineItemDto
            {
                Key = "ends",
                Label = "Sales.Timeline.Ends",
                On = DateOnly.FromDateTime(SalesClock.ToLocal(end).DateTime),
                IsFuture = now < end
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<SalesEmployerCommissionLineDto>> BuildLinesAsync(
        Guid beneficiaryUserId,
        HashSet<Guid> branchIds,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && e.CompanyId != null
                        && branchIds.Contains(e.CompanyId ?? Guid.Empty)
                        && (e.Kind == CommissionEntryKind.TokenCommission
                            || e.Kind == CommissionEntryKind.IndirectTokenCommission
                            || e.Kind == CommissionEntryKind.FounderBonus))
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
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
                e.SourceTokenCheckoutId
            })
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return [];
        }

        var checkoutIds = entries
            .Where(e => e.SourceTokenCheckoutId is not null)
            .Select(e => e.SourceTokenCheckoutId!.Value)
            .Distinct()
            .ToList();
        var checkouts = checkoutIds.Count == 0
            ? new Dictionary<Guid, (int PackSize, decimal AmountExVat)>()
            : await _db.TokenPurchaseCheckouts.AsNoTracking()
                .Where(c => checkoutIds.Contains(c.Id))
                .ToDictionaryAsync(
                    c => c.Id,
                    c => (c.PackSize, AmountExVat: c.AmountExVatCents / 100m),
                    cancellationToken);

        var packSizes = checkouts.Values.Select(v => v.PackSize).Distinct().ToList();
        var packages = packSizes.Count == 0
            ? new Dictionary<int, string>()
            : await _db.SalesPackages.AsNoTracking()
                .Where(p => p.IsActive && packSizes.Contains(p.TokenAmount))
                .GroupBy(p => p.TokenAmount)
                .Select(g => new { Pack = g.Key, Name = g.OrderBy(x => x.SortOrder).First().Name })
                .ToDictionaryAsync(x => x.Pack, x => x.Name, cancellationToken);

        var invoiceIds = entries.Where(e => e.SelfBillingInvoiceId is not null)
            .Select(e => e.SelfBillingInvoiceId!.Value).Distinct().ToList();
        var requestIds = entries.Where(e => e.SalesPayoutRequestId is not null)
            .Select(e => e.SalesPayoutRequestId!.Value).Distinct().ToList();
        var invoiceStatuses = invoiceIds.Count == 0
            ? new Dictionary<Guid, SelfBillingInvoiceStatus>()
            : await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Status, cancellationToken);
        var requestStatuses = requestIds.Count == 0
            ? new Dictionary<Guid, SalesPayoutRequestStatus>()
            : await _db.SalesPayoutRequests.AsNoTracking()
                .Where(r => requestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Status, cancellationToken);

        var lines = new List<SalesEmployerCommissionLineDto>();
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
            var state = _wallet.DeriveState(
                new CommissionLedgerEntry
                {
                    Kind = e.Kind,
                    AmountExVat = e.AmountExVat,
                    AvailableFromUtc = e.AvailableFromUtc,
                    SalesPayoutRequestId = e.SalesPayoutRequestId,
                    SelfBillingInvoiceId = e.SelfBillingInvoiceId,
                    CorrectsEntryId = e.CorrectsEntryId
                },
                utcNow,
                inv,
                req);

            string packageLabel;
            decimal purchaseEx = 0m;
            if (e.SourceTokenCheckoutId is Guid cid && checkouts.TryGetValue(cid, out var co))
            {
                purchaseEx = co.AmountExVat;
                packageLabel = packages.TryGetValue(co.PackSize, out var name)
                    ? name
                    : $"{co.PackSize} tokens";
            }
            else if (e.Kind == CommissionEntryKind.FounderBonus)
            {
                packageLabel = "Sales.Label.Kind.FounderBonus";
                purchaseEx = SalesCommissionRules.FirstYearOnboardingEuro;
            }
            else
            {
                packageLabel = "Sales.Label.Kind.TokenCommission";
            }

            lines.Add(new SalesEmployerCommissionLineDto
            {
                On = DateOnly.FromDateTime(SalesClock.ToLocal(e.CreatedAt).DateTime),
                PackageLabel = packageLabel,
                PurchaseAmountExVat = purchaseEx,
                OwnCommissionExVat = e.AmountExVat,
                State = state.ToString(),
                StateLabel = SalesLabels.Key(state)
            });
        }

        return lines;
    }

    private async Task<Guid> ResolveRootIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var row = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.Id, c.ParentCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? companyId : SalesCommercialUnit.RootIdOf(row.Id, row.ParentCompanyId);
    }

    private sealed record EmployerRow(
        SalesEmployerDto Dto,
        HashSet<Guid> BranchIds,
        DateTime AttributedAtUtc,
        DateTime? CommissionStartsAtUtc,
        decimal CommissionTotal,
        decimal? DirectRate,
        decimal? Year2Rate,
        decimal? Year3Rate,
        int? DurationDays,
        bool PendingStartHighlight,
        SalesAttributionSource? Source);
}
