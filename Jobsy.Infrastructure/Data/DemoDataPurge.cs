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
    public const string LiveRenderServiceName = "jobsy-api";

    private static readonly Type[] PostgresTruncateTypes =
    [
        typeof(PendingTokenAction),
        typeof(VatBufferTransfer),
        typeof(RevenueShareLog),
        typeof(TokenPurchaseInvoice),
        typeof(TokenPurchaseCheckout),
        typeof(SupplierOnboardingCheckout),
        typeof(ApiKey),
        typeof(CommissionLedgerEntry),
        typeof(SelfBillingInvoiceLine),
        typeof(SalesManagerPayoutCheckout),
        typeof(SelfBillingInvoice),
        typeof(VatDeclaration),
        typeof(SalesManagerApplication),
        typeof(SalesManagerProfile),
        typeof(AmbassadeurProfile),
        typeof(PartnerAffiliateProfile),
        typeof(EstablishmentTakeoverRequest),
        typeof(CompanyRegistration),
        typeof(RegionCompany),
        typeof(Region),
        typeof(CompanySalaryTableChangeLog),
        typeof(CompanySalaryRate),
        typeof(CompanySalaryTableAllowedBranch),
        typeof(CompanySalaryTable),
        typeof(ApplicationUploadedCv),
        typeof(Application),
        typeof(VacancyClick),
        typeof(VacancyLike),
        typeof(VacancyShare),
        typeof(VacancySearchImpression),
        typeof(TokenTransaction),
        typeof(CandidateUploadedCv),
        typeof(CandidateReference),
        typeof(CandidateActionToken),
        typeof(UserNotification),
        typeof(UserExternalLogin),
        typeof(SiteVisit),
        typeof(PlatformFeedback),
        typeof(UserCompany),
        typeof(Vacancy)
    ];

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

    /// <summary>
    /// Render injects <c>RENDER_SERVICE_NAME</c> without a Blueprint env sync.
    /// Production API is <c>jobsy-api</c>; Acceptatie is <c>lobsy-acc-api</c>.
    /// </summary>
    public static bool IsLiveProductionRuntime(IConfiguration configuration)
    {
        var serviceName = configuration["RENDER_SERVICE_NAME"];
        if (!string.IsNullOrWhiteSpace(serviceName)
            && serviceName.Equals(LiveRenderServiceName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsLiveProductionSite(configuration["PublicWebBaseUrl"]);
    }

    public static bool ShouldRun(IConfiguration configuration, bool alreadyMarked)
    {
        if (alreadyMarked)
        {
            return false;
        }

        if (IsLiveProductionRuntime(configuration))
        {
            return true;
        }

        if (configuration.GetValue("Seed:Enabled", false))
        {
            return false;
        }

        return configuration.GetValue("Seed:PurgeDemoData", false);
    }

    public static async Task<DemoDataPurgeResult> PurgeAsync(JobsyDbContext db, ILogger logger)
    {
        var companyCount = await db.Companies.IgnoreQueryFilters().CountAsync();
        var vacancyCount = await db.Vacancies.IgnoreQueryFilters().CountAsync();
        var removeUserIds = (await db.Users.Select(u => new { u.Id, u.Email }).ToListAsync())
            .Where(u => !IsKeptAdminEmail(u.Email))
            .Select(u => u.Id)
            .ToHashSet();

        if (companyCount == 0 && vacancyCount == 0 && removeUserIds.Count == 0)
        {
            await EnsureMarkerAsync(db);
            logger.LogInformation("Operational wipe: nothing to delete (admin-only / empty).");
            return new DemoDataPurgeResult(0, 0, 0);
        }

        logger.LogWarning(
            "Wiping operational data; keeping {Admin}. Removing {Companies} companies, {Vacancies} vacancies, {Users} users.",
            KeptAdminEmail,
            companyCount,
            vacancyCount,
            removeUserIds.Count);

        if (db.Database.IsNpgsql())
        {
            try
            {
                await PurgePostgresAsync(db, logger);
                await EnsureMarkerAsync(db);
                logger.LogWarning(
                    "Operational wipe finished via Postgres ({Companies} companies, {Vacancies} vacancies, {Users} users). Kept {Admin}.",
                    companyCount,
                    vacancyCount,
                    removeUserIds.Count,
                    KeptAdminEmail);
                return new DemoDataPurgeResult(companyCount, vacancyCount, removeUserIds.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Postgres operational wipe failed; falling back to EF.");
                db.ChangeTracker.Clear();
            }
        }

        await PurgeWithEfAsync(db, removeUserIds);

        await EnsureMarkerAsync(db);

        logger.LogWarning(
            "Operational wipe finished ({Companies} companies, {Vacancies} vacancies, {Users} users). Kept {Admin}.",
            companyCount,
            vacancyCount,
            removeUserIds.Count,
            KeptAdminEmail);

        return new DemoDataPurgeResult(companyCount, vacancyCount, removeUserIds.Count);
    }

    private static async Task PurgePostgresAsync(JobsyDbContext db, ILogger logger)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var tables = new List<string>();
        foreach (var type in PostgresTruncateTypes)
        {
            var entity = db.Model.FindEntityType(type);
            var name = entity?.GetTableName();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var schema = entity!.GetSchema();
            tables.Add(string.IsNullOrWhiteSpace(schema) ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"");
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE "Users" SET "CompanyId" = NULL, "ReferredByAmbassadeurUserId" = NULL;
            UPDATE "Companies"
               SET "ParentCompanyId" = NULL,
                   "ReferredBySalesManagerUserId" = NULL,
                   "ReferredByAmbassadeurUserId" = NULL,
                   "ReferredByPartnerUserId" = NULL,
                   "CommissionIndirectSalesManagerUserId" = NULL;
            """);

        if (tables.Count > 0)
        {
            logger.LogWarning("Truncating {Count} operational tables.", tables.Count);
            var truncateSql = "TRUNCATE TABLE " + string.Join(", ", tables) + " RESTART IDENTITY CASCADE";
            await db.Database.ExecuteSqlRawAsync(truncateSql);
        }

        await db.Database.ExecuteSqlRawAsync("""DELETE FROM "Companies";""");
        await db.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "LocalAuthCredentials"
            WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE lower("Email") <> lower({0}));
            """,
            KeptAdminEmail);
        await db.Database.ExecuteSqlRawAsync(
            """DELETE FROM "Users" WHERE lower("Email") <> lower({0});""",
            KeptAdminEmail);

        db.ChangeTracker.Clear();
        await tx.CommitAsync();
    }

    private static async Task PurgeWithEfAsync(JobsyDbContext db, HashSet<Guid> removeUserIds)
    {
        var users = await db.Users.ToListAsync();

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
