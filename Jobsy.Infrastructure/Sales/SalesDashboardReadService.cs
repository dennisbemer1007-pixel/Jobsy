using System.Globalization;
using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesDashboardReadService : ISalesDashboardReadService
{
    private static readonly CommissionEntryKind[] EarningKinds =
    [
        CommissionEntryKind.TokenCommission,
        CommissionEntryKind.IndirectTokenCommission,
        CommissionEntryKind.FounderBonus,
        CommissionEntryKind.RefundCorrection,
        CommissionEntryKind.ChargebackCorrection,
        CommissionEntryKind.Adjustment
    ];

    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    private readonly JobsyDbContext _db;
    private readonly ISalesWalletReadService _wallet;
    private readonly ISalesFunnelReadService _funnel;
    private readonly ISalesEmployerPortalReadService _employers;

    public SalesDashboardReadService(
        JobsyDbContext db,
        ISalesWalletReadService wallet,
        ISalesFunnelReadService funnel,
        ISalesEmployerPortalReadService employers)
    {
        _db = db;
        _wallet = wallet;
        _funnel = funnel;
        _employers = employers;
    }

    public async Task<SalesDashboardDto> GetAsync(
        Guid beneficiaryUserId,
        string period,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var today = SalesClock.Today(now);
        var periodKey = NormalizePeriod(period);

        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == beneficiaryUserId, cancellationToken);
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == beneficiaryUserId)
            .Select(u => new { u.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        var settings = await _db.SalesCommercialSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var holdDays = settings?.CommissionHoldDays ?? 14;
        var minimum = settings?.PayoutMinimumEuro ?? 50m;

        var balances = await _wallet.GetBalancesAsync(beneficiaryUserId, now, cancellationToken);
        var (earned, prev) = await GetEarnedAsync(beneficiaryUserId, periodKey, today, cancellationToken);
        var monthly = await GetMonthlyBarsAsync(beneficiaryUserId, today, cancellationToken);
        var employersPage = await _employers.ListAsync(
            beneficiaryUserId, null, null, null, 1, 500, now, cancellationToken);
        var kpis = employersPage.Kpis;

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthStartUtc = SalesClock.EndOfLocalDayUtc(monthStart.AddDays(-1)).AddSeconds(1);
        var newThisMonth = await _db.Companies.AsNoTracking()
            .CountAsync(
                c => c.ParentCompanyId == null
                     && c.ReferredBySalesManagerUserId == beneficiaryUserId
                     && c.SalesAttributedAtUtc != null
                     && c.SalesAttributedAtUtc >= monthStartUtc,
                cancellationToken);

        var nextRun = ResolveNextRunDate(today);
        var funnel = await BuildFunnelAsync(beneficiaryUserId, periodKey, today, cancellationToken);
        var todos = await BuildTodosAsync(beneficiaryUserId, employersPage.Items, now, cancellationToken);
        var top = employersPage.Items
            .OrderByDescending(e => e.CommissionThisYear)
            .Take(5)
            .Select(e => new SalesTopEmployerDto
            {
                CompanyId = e.CompanyId,
                DisplayName = e.DisplayName,
                Place = e.Place,
                StatusLabelKey = e.StatusLabelKey,
                QuietDays = e.QuietDays,
                CommissionThisYear = e.CommissionThisYear,
                IsQuiet = e.Status == SalesEmployerStatus.Quiet
            })
            .ToList();

        var (canPayout, blocked) = await ResolvePayoutGateAsync(
            beneficiaryUserId, balances.Available, minimum, profile, cancellationToken);

        return new SalesDashboardDto
        {
            GreetingName = FirstNameOf(user?.FullName),
            TrackingCode = profile?.TrackingCode,
            Period = periodKey,
            Available = balances.Available,
            Pending = balances.Pending,
            EarnedInPeriod = earned,
            EarnedPrevComparable = prev,
            ActiveEmployers = kpis.ActiveEmployers,
            TotalEmployers = kpis.TotalEmployers,
            NewEmployersThisMonth = newThisMonth,
            NextRunDate = nextRun,
            CommissionHoldDays = holdDays,
            PayoutMinimumEuro = minimum,
            CanRequestPayout = canPayout,
            PayoutBlockedReason = blocked,
            Monthly = monthly,
            Funnel = funnel,
            Todos = todos,
            TopEmployers = top
        };
    }

    internal static DateOnly ResolveNextRunDate(DateOnly today)
    {
        var thisMonth = SalesClock.FirstWorkdayOfMonth(today.Year, today.Month);
        if (today <= thisMonth)
        {
            return thisMonth;
        }

        var next = today.AddMonths(1);
        return SalesClock.FirstWorkdayOfMonth(next.Year, next.Month);
    }

    private async Task<(decimal Earned, decimal? Prev)> GetEarnedAsync(
        Guid beneficiaryUserId,
        string period,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId && EarningKinds.Contains(e.Kind))
            .Select(e => new { e.AmountExVat, e.CreatedAt, e.Kind })
            .ToListAsync(cancellationToken);

        DateOnly from;
        DateOnly? prevFrom = null;
        DateOnly? prevTo = null;
        switch (period)
        {
            case "month":
                from = new DateOnly(today.Year, today.Month, 1);
                var prevMonth = from.AddMonths(-1);
                prevFrom = new DateOnly(prevMonth.Year, prevMonth.Month, 1);
                prevTo = from.AddDays(-1);
                break;
            case "all":
                from = DateOnly.MinValue;
                break;
            default:
                from = new DateOnly(today.Year, 1, 1);
                prevFrom = new DateOnly(today.Year - 1, 1, 1);
                prevTo = new DateOnly(today.Year - 1, 12, 31);
                break;
        }

        decimal SumIn(DateOnly start, DateOnly end) => entries
            .Where(e =>
            {
                var d = SalesClock.Today(e.CreatedAt);
                return d >= start && d <= end && e.AmountExVat > 0;
            })
            .Sum(e => e.AmountExVat);

        var earned = SumIn(from, today);
        if (prevFrom is null || prevTo is null)
        {
            return (earned, null);
        }

        var prev = SumIn(prevFrom.Value, prevTo.Value);
        return (earned, prev > 0 ? prev : null);
    }

    private async Task<IReadOnlyList<SalesMonthlyBarDto>> GetMonthlyBarsAsync(
        Guid beneficiaryUserId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var fromUtc = SalesClock.EndOfLocalDayUtc(from.AddDays(-1)).AddSeconds(1);
        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && EarningKinds.Contains(e.Kind)
                        && e.AmountExVat > 0
                        && e.CreatedAt >= fromUtc)
            .Select(e => new { e.AmountExVat, e.CreatedAt })
            .ToListAsync(cancellationToken);

        var bars = new List<SalesMonthlyBarDto>(12);
        for (var i = 0; i < 12; i++)
        {
            var m = from.AddMonths(i);
            var amount = entries
                .Where(e =>
                {
                    var d = SalesClock.Today(e.CreatedAt);
                    return d.Year == m.Year && d.Month == m.Month;
                })
                .Sum(e => e.AmountExVat);
            bars.Add(new SalesMonthlyBarDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.ToString("MMM", Nl).TrimEnd('.'),
                AmountExVat = amount,
                IsCurrent = m.Year == today.Year && m.Month == today.Month
            });
        }

        return bars;
    }

    private async Task<SalesFunnelDto?> BuildFunnelAsync(
        Guid beneficiaryUserId,
        string period,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        DateOnly from;
        string label;
        switch (period)
        {
            case "month":
                from = new DateOnly(today.Year, today.Month, 1);
                label = "Sales.Funnel.Period.Month";
                break;
            case "all":
                from = new DateOnly(2000, 1, 1);
                label = "Sales.Funnel.Period.All";
                break;
            default:
                from = new DateOnly(today.Year, 1, 1);
                label = "Sales.Funnel.Period.Year";
                break;
        }

        var snap = await _funnel.GetAsync(beneficiaryUserId, from, today, cancellationToken);
        if (snap.Visits == 0 && snap.Registered == 0)
        {
            return null;
        }

        return new SalesFunnelDto
        {
            Visits = snap.Visits,
            Registered = snap.Registered,
            FirstPurchase = snap.FirstPurchase,
            ActiveNow = snap.ActiveNow,
            PeriodLabel = label
        };
    }

    private async Task<IReadOnlyList<SalesTodoDto>> BuildTodosAsync(
        Guid beneficiaryUserId,
        IReadOnlyList<SalesEmployerDto> employers,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var todos = new List<SalesTodoDto>();
        var today = SalesClock.Today(utcNow);

        foreach (var e in employers.Where(x => x.Status == SalesEmployerStatus.NoPurchase))
        {
            var days = today.DayNumber - e.AttributedOn.DayNumber;
            if (days < 7)
            {
                continue;
            }

            todos.Add(new SalesTodoDto
            {
                Kind = "no-purchase",
                TitleKey = "Sales.Todo.NoPurchase",
                TitleArgs = [e.DisplayName],
                SubLineKey = "Sales.Todo.NoPurchaseSub",
                SubLineArgs = [e.AttributedOn.ToString("dd-MM-yyyy")],
                ActionLabelKey = "Sales.Todo.View",
                ActionHref = $"/sales/werkgevers?open={e.CompanyId}",
                CompanyId = e.CompanyId,
                Urgency = 100 + days
            });
        }

        foreach (var e in employers.Where(x => x.CommissionYear is 1 or 2))
        {
            var daysLeftInYear = (int)Math.Round((1 - e.YearProgress) * SalesCommissionRules.CommissionYearLengthDays);
            if (daysLeftInYear is < 0 or > 60)
            {
                continue;
            }

            var nextYear = e.CommissionYear!.Value + 1;
            var nextRate = nextYear switch
            {
                2 => SalesCommissionRules.DefaultYear2DirectCommissionRate,
                3 => SalesCommissionRules.DefaultYear3DirectCommissionRate,
                _ => 0m
            };
            var currentPct = (int)Math.Round((e.CurrentRate ?? 0m) * 100m);
            var nextPct = (int)Math.Round(nextRate * 100m);
            var switchOn = today.AddDays(daysLeftInYear);
            todos.Add(new SalesTodoDto
            {
                Kind = "year-change",
                TitleKey = "Sales.Todo.YearChange",
                TitleArgs = [e.DisplayName, nextYear.ToString()],
                SubLineKey = "Sales.Todo.YearChangeSub",
                SubLineArgs = [switchOn.ToString("dd-MM-yyyy"), nextPct.ToString(), currentPct.ToString()],
                ActionLabelKey = "Sales.Todo.View",
                ActionHref = $"/sales/werkgevers?open={e.CompanyId}",
                CompanyId = e.CompanyId,
                Urgency = 80 + (60 - daysLeftInYear)
            });
        }

        var paidSince = utcNow.AddDays(-14);
        var paidInvoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == beneficiaryUserId
                        && i.Status == SelfBillingInvoiceStatus.Paid
                        && i.PaidAt != null
                        && i.PaidAt >= paidSince)
            .OrderByDescending(i => i.PaidAt)
            .Take(3)
            .Select(i => new { i.Id, i.InvoiceNumber, i.TotalInclVat, i.PaidAt })
            .ToListAsync(cancellationToken);
        foreach (var inv in paidInvoices)
        {
            todos.Add(new SalesTodoDto
            {
                Kind = "invoice-paid",
                TitleKey = "Sales.Todo.InvoicePaid",
                TitleArgs = [inv.InvoiceNumber],
                SubLineKey = "Sales.Todo.InvoicePaidSub",
                SubLineArgs = [SalesMoney.Format(inv.TotalInclVat, SalesMoneyKind.InclVat)],
                ActionLabelKey = "Sales.Todo.Download",
                ActionHref = $"/sales/wallet?tab=facturen",
                InvoiceId = inv.Id,
                Urgency = 50
            });
        }

        var hasConsent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .AnyAsync(
                c => c.UserId == beneficiaryUserId
                     && c.Version == SalesSelfBilling.CurrentVersion
                     && c.RevokedAtUtc == null,
                cancellationToken);
        if (!hasConsent)
        {
            todos.Add(new SalesTodoDto
            {
                Kind = "self-billing",
                TitleKey = "Sales.Todo.SelfBilling",
                SubLineKey = "Sales.Todo.SelfBillingSub",
                ActionLabelKey = "Sales.Todo.Profile",
                ActionHref = "/sales/profiel",
                Urgency = 200
            });
        }

        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == beneficiaryUserId, cancellationToken);
        if (profile is not null && string.IsNullOrWhiteSpace(profile.Iban))
        {
            todos.Add(new SalesTodoDto
            {
                Kind = "iban",
                TitleKey = "Sales.Todo.Iban",
                SubLineKey = "Sales.Todo.IbanSub",
                ActionLabelKey = "Sales.Todo.Profile",
                ActionHref = "/sales/profiel",
                Urgency = 190
            });
        }

        foreach (var e in employers.Where(x => x.Status == SalesEmployerStatus.Quiet))
        {
            var quietDays = e.QuietDays ?? 91;
            if (quietDays <= 90)
            {
                quietDays = 91;
            }

            todos.Add(new SalesTodoDto
            {
                Kind = "quiet",
                TitleKey = "Sales.Todo.Quiet",
                TitleArgs = [e.DisplayName, quietDays.ToString()],
                SubLineKey = "Sales.Todo.QuietSub",
                ActionLabelKey = "Sales.Todo.View",
                ActionHref = $"/sales/werkgevers?open={e.CompanyId}",
                CompanyId = e.CompanyId,
                Urgency = 40 + Math.Min(quietDays, 100)
            });
        }

        return todos
            .OrderByDescending(t => t.Urgency)
            .Take(5)
            .ToList();
    }

    private async Task<(bool Can, string? Reason)> ResolvePayoutGateAsync(
        Guid beneficiaryUserId,
        decimal available,
        decimal minimum,
        SalesManagerProfile? profile,
        CancellationToken cancellationToken)
    {
        if (available < minimum)
        {
            return (false, "Sales.Payout.BelowMinimum");
        }

        if (profile is null || string.IsNullOrWhiteSpace(profile.Iban))
        {
            return (false, "Sales.Payout.NeedsIban");
        }

        var hasConsent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .AnyAsync(
                c => c.UserId == beneficiaryUserId
                     && c.Version == SalesSelfBilling.CurrentVersion
                     && c.RevokedAtUtc == null,
                cancellationToken);
        if (!hasConsent)
        {
            return (false, "Sales.Payout.NeedsConsent");
        }

        return (true, null);
    }

    private static string NormalizePeriod(string? period) =>
        period?.Trim().ToLowerInvariant() switch
        {
            "month" or "maand" => "month",
            "all" or "alles" => "all",
            _ => "year"
        };

    private static string FirstNameOf(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "";
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "" : parts[0];
    }
}
