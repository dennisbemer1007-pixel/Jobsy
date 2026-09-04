using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Identifies and deletes deterministic seeder rows (Westland/Haaglanden banenkaart,
/// @jobsy.local demo accounts, sprint-8 metrics). Leaves real registrations and
/// platform masterdata (categories, token prices, about-page) intact.
/// </summary>
internal static class DemoDataPurge
{
    public const string Marker = "Demo data purged";

    public static bool IsDemoCompanyId(Guid id)
    {
        if (id == Guid.Parse("11111111-1111-1111-1111-111111111111")
            || id == Guid.Parse("22222222-2222-2222-2222-222222222222")
            || id == Guid.Parse("33333333-3333-3333-3333-333333333333")
            || id == Guid.Parse("44444444-4444-4444-4444-444444444444"))
        {
            return true;
        }

        var key = id.ToString("D");
        return key.StartsWith("c1000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("c2000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("c3000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("c4000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDemoVacancyId(Guid id)
    {
        if (id == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            || id == Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            || id == Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
            || id == Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")
            || id == Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")
            || id == Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")
            || id == Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
        {
            return true;
        }

        var key = id.ToString("D");
        return key.StartsWith("a1000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("a2000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("a3000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("a4000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("a5000000-0000-4000-8000-", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDemoUserEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && email.EndsWith("@jobsy.local", StringComparison.OrdinalIgnoreCase);

    public static async Task<DemoDataPurgeResult> PurgeAsync(JobsyDbContext db, ILogger logger)
    {
        var companies = await db.Companies.ToListAsync();
        var demoCompanyIds = companies.Where(c => IsDemoCompanyId(c.Id)).Select(c => c.Id).ToHashSet();

        var users = await db.Users.ToListAsync();
        var demoUserIds = users.Where(u => IsDemoUserEmail(u.Email)).Select(u => u.Id).ToHashSet();

        var vacancies = await db.Vacancies.ToListAsync();
        var demoVacancyIds = vacancies
            .Where(v => demoCompanyIds.Contains(v.CompanyId)
                        || (v.IntermediaryCompanyId is { } inter && demoCompanyIds.Contains(inter))
                        || IsDemoVacancyId(v.Id))
            .Select(v => v.Id)
            .ToHashSet();

        if (demoCompanyIds.Count == 0 && demoUserIds.Count == 0 && demoVacancyIds.Count == 0)
        {
            logger.LogInformation("No seeder mock data found to purge.");
            return new DemoDataPurgeResult(0, 0, 0);
        }

        logger.LogWarning(
            "Purging seeder mock data: {Companies} companies, {Vacancies} vacancies, {Users} @jobsy.local users.",
            demoCompanyIds.Count,
            demoVacancyIds.Count,
            demoUserIds.Count);

        await RemoveWhereAsync(db, db.PendingTokenActions, x =>
            demoCompanyIds.Contains(x.CompanyId) || demoVacancyIds.Contains(x.VacancyId));

        var demoInvoiceIds = await db.TokenPurchaseInvoices
            .Where(i => demoCompanyIds.Contains(i.CompanyId))
            .Select(i => i.Id)
            .ToListAsync();
        if (demoInvoiceIds.Count > 0)
        {
            await RemoveWhereAsync(db, db.VatBufferTransfers, x => demoInvoiceIds.Contains(x.TokenPurchaseInvoiceId));
        }

        await RemoveWhereAsync(db, db.RevenueShareLogs, x => demoCompanyIds.Contains(x.CompanyId));
        await RemoveWhereAsync(db, db.TokenPurchaseInvoices, x => demoCompanyIds.Contains(x.CompanyId));
        await RemoveWhereAsync(db, db.TokenPurchaseCheckouts, x => demoCompanyIds.Contains(x.CompanyId));
        await RemoveWhereAsync(db, db.SupplierOnboardingCheckouts, x => demoCompanyIds.Contains(x.CompanyId));
        await RemoveWhereAsync(db, db.ApiKeys, x => demoCompanyIds.Contains(x.CompanyId));
        await RemoveWhereAsync(db, db.CommissionLedgerEntries, x =>
            (x.CompanyId != null && demoCompanyIds.Contains(x.CompanyId.Value))
            || demoUserIds.Contains(x.SalesManagerUserId));

        var demoSelfBillingIds = await db.SelfBillingInvoices
            .Where(i => demoUserIds.Contains(i.SalesManagerUserId))
            .Select(i => i.Id)
            .ToListAsync();
        if (demoSelfBillingIds.Count > 0)
        {
            await RemoveWhereAsync(db, db.SelfBillingInvoiceLines, x => demoSelfBillingIds.Contains(x.SelfBillingInvoiceId));
        }

        await RemoveWhereAsync(db, db.SalesManagerPayoutCheckouts, x => demoUserIds.Contains(x.SalesManagerUserId));
        await RemoveWhereAsync(db, db.SelfBillingInvoices, x => demoUserIds.Contains(x.SalesManagerUserId));
        await RemoveWhereAsync(db, db.SalesManagerApplications, x =>
            demoUserIds.Contains(x.ReferrerSalesManagerUserId)
            || (x.ProvisionedUserId != null && demoUserIds.Contains(x.ProvisionedUserId.Value)));
        await RemoveWhereAsync(db, db.SalesManagerProfiles, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.AmbassadeurProfiles, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.PartnerAffiliateProfiles, x => demoUserIds.Contains(x.UserId));

        await RemoveWhereAsync(db, db.EstablishmentTakeoverRequests, x => demoCompanyIds.Contains(x.TargetCompanyId));
        await RemoveWhereAsync(db, db.CompanyRegistrations, x =>
            (x.CreatedOrganizationCompanyId != null && demoCompanyIds.Contains(x.CreatedOrganizationCompanyId.Value))
            || (x.CreatedBranchCompanyId != null && demoCompanyIds.Contains(x.CreatedBranchCompanyId.Value)));

        var demoRegionIds = await db.Regions
            .Where(r => demoCompanyIds.Contains(r.OrganizationCompanyId))
            .Select(r => r.Id)
            .ToListAsync();
        if (demoRegionIds.Count > 0)
        {
            await RemoveWhereAsync(db, db.RegionCompanies, x => demoRegionIds.Contains(x.RegionId));
            await RemoveWhereAsync(db, db.Regions, x => demoRegionIds.Contains(x.Id));
        }

        await RemoveWhereAsync(db, db.RegionCompanies, x => demoCompanyIds.Contains(x.CompanyId));

        var demoTableIds = await db.CompanySalaryTables
            .Where(t => demoCompanyIds.Contains(t.CompanyId))
            .Select(t => t.Id)
            .ToListAsync();
        if (demoTableIds.Count > 0)
        {
            await RemoveWhereAsync(db, db.CompanySalaryTableChangeLogs, x => demoTableIds.Contains(x.SalaryTableId));
            await RemoveWhereAsync(db, db.CompanySalaryRates, x => demoTableIds.Contains(x.SalaryTableId));
            await RemoveWhereAsync(db, db.CompanySalaryTableAllowedBranches, x =>
                demoTableIds.Contains(x.SalaryTableId) || demoCompanyIds.Contains(x.CompanyId));
        }

        var demoApplicationIds = (await db.Applications.ToListAsync())
            .Where(a => demoVacancyIds.Contains(a.VacancyId)
                        || (a.CandidateUserId is { } cand && demoUserIds.Contains(cand)))
            .Select(a => a.Id)
            .ToHashSet();
        if (demoApplicationIds.Count > 0)
        {
            await RemoveWhereAsync(db, db.ApplicationUploadedCvs, x => demoApplicationIds.Contains(x.ApplicationId));
            await RemoveWhereAsync(db, db.Applications, x => demoApplicationIds.Contains(x.Id));
        }
        await RemoveWhereAsync(db, db.VacancyClicks, x => demoVacancyIds.Contains(x.VacancyId));
        await RemoveWhereAsync(db, db.VacancyLikes, x => demoVacancyIds.Contains(x.VacancyId));
        await RemoveWhereAsync(db, db.VacancyShares, x => demoVacancyIds.Contains(x.VacancyId));
        await RemoveWhereAsync(db, db.VacancySearchImpressions, x => demoVacancyIds.Contains(x.VacancyId));
        await RemoveWhereAsync(db, db.TokenTransactions, x => demoCompanyIds.Contains(x.CompanyId));

        await RemoveWhereAsync(db, db.CandidateUploadedCvs, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.CandidateReferences, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.CandidateActionTokens, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.UserNotifications, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.UserExternalLogins, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.LocalAuthCredentials, x => demoUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.SiteVisits, x => x.UserId != null && demoUserIds.Contains(x.UserId.Value));
        await RemoveWhereAsync(db, db.PlatformFeedbacks, x => x.UserId != null && demoUserIds.Contains(x.UserId.Value));
        await RemoveWhereAsync(db, db.UserCompanies, x =>
            demoUserIds.Contains(x.UserId) || demoCompanyIds.Contains(x.CompanyId));

        foreach (var user in users.Where(u => demoCompanyIds.Contains(u.CompanyId ?? Guid.Empty)))
        {
            user.CompanyId = null;
        }

        foreach (var company in companies.Where(c => demoCompanyIds.Contains(c.Id)))
        {
            company.ParentCompanyId = null;
            company.ReferredBySalesManagerUserId = null;
            company.ReferredByAmbassadeurUserId = null;
            company.ReferredByPartnerUserId = null;
            company.CommissionIndirectSalesManagerUserId = null;
        }

        await db.SaveChangesAsync();

        if (demoTableIds.Count > 0)
        {
            await RemoveWhereAsync(db, db.CompanySalaryTables, x => demoTableIds.Contains(x.Id));
        }

        await RemoveWhereAsync(db, db.Vacancies, x => demoVacancyIds.Contains(x.Id));
        await db.SaveChangesAsync();

        await RemoveWhereAsync(db, db.Companies, x => demoCompanyIds.Contains(x.Id));
        await db.SaveChangesAsync();

        await RemoveWhereAsync(db, db.Users, x => demoUserIds.Contains(x.Id));
        await RemoveWhereAsync(db, db.PlatformLogs, x => x.Category == "Seed");

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = Marker,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        logger.LogWarning(
            "Purged seeder mock data ({Companies} companies, {Vacancies} vacancies, {Users} users).",
            demoCompanyIds.Count,
            demoVacancyIds.Count,
            demoUserIds.Count);

        return new DemoDataPurgeResult(demoCompanyIds.Count, demoVacancyIds.Count, demoUserIds.Count);
    }

    private static async Task RemoveWhereAsync<T>(
        JobsyDbContext db,
        IQueryable<T> source,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        where T : class
    {
        var rows = await source.Where(predicate).ToListAsync();
        if (rows.Count > 0)
        {
            db.Set<T>().RemoveRange(rows);
        }
    }
}

internal readonly record struct DemoDataPurgeResult(int Companies, int Vacancies, int Users);
