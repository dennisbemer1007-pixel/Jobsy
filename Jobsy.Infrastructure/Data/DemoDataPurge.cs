using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// One-shot wipe of operational demo rows on live <c>lobsy.nl</c>: every company,
/// vacancy and non-admin user. Keeps <c>admin@jobsy.local</c> plus platform
/// masterdata (categories, token prices, about-page, integration secrets).
/// </summary>
internal static class DemoDataPurge
{
    public const string Marker = "Operational wipe 2026-09-04 keep admin@jobsy.local";
    public const string KeptAdminEmail = "admin@jobsy.local";

    public static bool IsKeptAdminEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && email.Equals(KeptAdminEmail, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Live production is the apex site. Acceptatie uses acceptatie.lobsy.nl or onrender.
    /// </summary>
    public static bool IsLiveProductionSite(string? publicWebBaseUrl)
    {
        if (!Uri.TryCreate(publicWebBaseUrl, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        return uri.Host.Equals("lobsy.nl", StringComparison.OrdinalIgnoreCase)
               || uri.Host.Equals("www.lobsy.nl", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldRun(IConfiguration configuration, bool alreadyMarked)
    {
        if (alreadyMarked || configuration.GetValue("Seed:Enabled", false))
        {
            return false;
        }

        return configuration.GetValue("Seed:PurgeDemoData", false)
               || IsLiveProductionSite(configuration["PublicWebBaseUrl"]);
    }

    public static async Task<DemoDataPurgeResult> PurgeAsync(JobsyDbContext db, ILogger logger)
    {
        var companyIds = await db.Companies.IgnoreQueryFilters().Select(c => c.Id).ToListAsync();
        var vacancyIds = await db.Vacancies.IgnoreQueryFilters().Select(v => v.Id).ToListAsync();
        var users = await db.Users.ToListAsync();
        var removeUserIds = users.Where(u => !IsKeptAdminEmail(u.Email)).Select(u => u.Id).ToHashSet();

        if (companyIds.Count == 0 && vacancyIds.Count == 0 && removeUserIds.Count == 0)
        {
            await EnsureMarkerAsync(db);
            logger.LogInformation("Operational wipe: nothing to delete (admin-only / empty).");
            return new DemoDataPurgeResult(0, 0, 0);
        }

        logger.LogWarning(
            "Wiping operational data; keeping {Admin}. Removing {Companies} companies, {Vacancies} vacancies, {Users} users.",
            KeptAdminEmail,
            companyIds.Count,
            vacancyIds.Count,
            removeUserIds.Count);

        await RemoveAllAsync(db, db.PendingTokenActions);
        await RemoveAllAsync(db, db.VatBufferTransfers);
        await RemoveAllAsync(db, db.RevenueShareLogs);
        await RemoveAllAsync(db, db.TokenPurchaseInvoices);
        await RemoveAllAsync(db, db.TokenPurchaseCheckouts);
        await RemoveAllAsync(db, db.SupplierOnboardingCheckouts);
        await RemoveAllAsync(db, db.ApiKeys);
        await RemoveAllAsync(db, db.CommissionLedgerEntries);
        await RemoveAllAsync(db, db.SelfBillingInvoiceLines);
        await RemoveAllAsync(db, db.SalesManagerPayoutCheckouts);
        await RemoveAllAsync(db, db.SelfBillingInvoices);
        await RemoveAllAsync(db, db.VatDeclarations);
        await RemoveAllAsync(db, db.SalesManagerApplications);
        await RemoveAllAsync(db, db.SalesManagerProfiles);
        await RemoveAllAsync(db, db.AmbassadeurProfiles);
        await RemoveAllAsync(db, db.PartnerAffiliateProfiles);
        await RemoveAllAsync(db, db.EstablishmentTakeoverRequests);
        await RemoveAllAsync(db, db.CompanyRegistrations);
        await RemoveAllAsync(db, db.RegionCompanies);
        await RemoveAllAsync(db, db.Regions);
        await RemoveAllAsync(db, db.CompanySalaryTableChangeLogs);
        await RemoveAllAsync(db, db.CompanySalaryRates);
        await RemoveAllAsync(db, db.CompanySalaryTableAllowedBranches);
        await RemoveAllAsync(db, db.ApplicationUploadedCvs);
        await RemoveAllAsync(db, db.Applications);
        await RemoveAllAsync(db, db.VacancyClicks);
        await RemoveAllAsync(db, db.VacancyLikes);
        await RemoveAllAsync(db, db.VacancyShares);
        await RemoveAllAsync(db, db.VacancySearchImpressions);
        await RemoveAllAsync(db, db.TokenTransactions.IgnoreQueryFilters());
        await RemoveWhereAsync(db, db.CandidateUploadedCvs, x => removeUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.CandidateReferences, x => removeUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.CandidateActionTokens, x => removeUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.UserNotifications, x => removeUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.UserExternalLogins, x => removeUserIds.Contains(x.UserId));
        await RemoveWhereAsync(db, db.LocalAuthCredentials, x => removeUserIds.Contains(x.UserId));
        await RemoveAllAsync(db, db.SiteVisits);
        await RemoveWhereAsync(db, db.PlatformFeedbacks, x =>
            x.UserId == null || (x.UserId != null && removeUserIds.Contains(x.UserId.Value)));
        await RemoveAllAsync(db, db.UserCompanies);
        await db.SaveChangesAsync();

        foreach (var user in users)
        {
            user.CompanyId = null;
            user.ReferredByAmbassadeurUserId = null;
        }

        foreach (var company in await db.Companies.ToListAsync())
        {
            company.ParentCompanyId = null;
            company.ReferredBySalesManagerUserId = null;
            company.ReferredByAmbassadeurUserId = null;
            company.ReferredByPartnerUserId = null;
            company.CommissionIndirectSalesManagerUserId = null;
        }

        await db.SaveChangesAsync();

        await RemoveAllAsync(db, db.CompanySalaryTables);
        await RemoveAllAsync(db, db.Vacancies.IgnoreQueryFilters());
        await db.SaveChangesAsync();

        await RemoveAllAsync(db, db.Companies);
        await db.SaveChangesAsync();

        await RemoveWhereAsync(db, db.Users, x => removeUserIds.Contains(x.Id));
        await db.SaveChangesAsync();

        await EnsureMarkerAsync(db);

        logger.LogWarning(
            "Operational wipe finished ({Companies} companies, {Vacancies} vacancies, {Users} users). Kept {Admin}.",
            companyIds.Count,
            vacancyIds.Count,
            removeUserIds.Count,
            KeptAdminEmail);

        return new DemoDataPurgeResult(companyIds.Count, vacancyIds.Count, removeUserIds.Count);
    }

    private static async Task EnsureMarkerAsync(JobsyDbContext db)
    {
        if (await db.PlatformLogs.AnyAsync(l => l.Category == "Seed" && l.Message == Marker))
        {
            return;
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = Marker,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static Task RemoveAllAsync<T>(JobsyDbContext db, IQueryable<T> source)
        where T : class
        => RemoveWhereAsync(db, source, _ => true);

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
