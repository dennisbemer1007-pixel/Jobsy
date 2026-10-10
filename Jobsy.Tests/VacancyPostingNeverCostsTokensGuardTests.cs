using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Werkgever;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

/// <summary>
/// Owner rule: posting / renew / extend never debits tokens (accept-candidate only).
/// </summary>
public class VacancyPostingNeverCostsTokensGuardTests
{
    [Fact]
    public void VacancyProductService_wires_VacancyPostingTokenRules()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure", "Services", "VacancyProductService.cs"));
        Assert.Contains("VacancyPostingTokenRules", src, StringComparison.Ordinal);
        Assert.DoesNotContain("FreePublishRules.EffectivePublishCost", src, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyPostingTokenRules_publish_and_extend_are_zero()
    {
        Assert.Equal(0m, VacancyPostingTokenRules.PublishCostTokens);
        Assert.Equal(0m, VacancyPostingTokenRules.ExtendCostTokens);
        Assert.Equal(0m, VacancyPostingTokenRules.EffectivePublishCost(99m));
    }

    [Fact]
    public void EstimatePendingTokens_excludes_publish_and_extend()
    {
        var costs = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Publish"] = 5m,
            ["Highlight"] = 2m,
            ["Extend"] = 3m
        };
        var v = new Jobsy.Web.Models.VacancyListItem
        {
            RequestedHighlight = true,
            RequestedExtend = true,
            CategoryPublishCostTokens = 5m
        };
        Assert.Equal(2m, VacancyManageRules.EstimatePendingTokens(v, costs));
    }

    [Fact]
    public async Task Publish_regular_vacancy_at_zero_balance_never_spends_tokens()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0, VacancyKind.Regular);
        SeedSpendCosts(db);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var result = await CreateProducts(db).PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        Assert.Equal(0m, await new TokenLedgerService(db).GetBalanceAsync(companyId));
    }

    [Fact]
    public async Task Extend_at_zero_balance_never_spends_tokens()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0, VacancyKind.Regular);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        var beforeEnd = vacancy.EndDate;
        await db.SaveChangesAsync();

        var result = await CreateProducts(db).ExtendAsync(vacancy, actorUserId: null);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(beforeEnd.AddDays(VacancyProductRules.ExtendDays), vacancy.EndDate);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
    }

    private static IVacancyProductService CreateProducts(JobsyDbContext db)
    {
        var existing = db.PlatformFeatureSettings.Local.FirstOrDefault()
                       ?? db.PlatformFeatureSettings.FirstOrDefault();
        if (existing is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                FreePublishUntil = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.FreePublishUntil = null;
        }

        db.SaveChanges();

        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        return new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            new UserNotificationService(db),
            new CandidateActionTokenService(db),
            NullLogger<VacancyProductService>.Instance);
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = VacancyProductRules.DefaultHighlightCostTokens, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });
        db.SaveChanges();
    }

    private static async Task<(Guid CompanyId, Guid VacancyId)> SeedDraftVacancyAsync(
        JobsyDbContext db,
        decimal tokenBalance,
        VacancyKind kind)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Posting free Co",
            KvkNumber = "12345678",
            Address = "Westland",
            Location = new GeoPoint(51.99, 4.22),
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Kassamedewerker",
            Description = "Demo",
            Status = VacancyStatus.Draft,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Location = new GeoPoint(51.99, 4.22),
            RequiredTransport = TransportMode.Bike,
            Kind = kind
        });

        if (tokenBalance > 0)
        {
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = tokenBalance,
                Kind = TokenTransactionKind.Grant,
                OldBalance = 0,
                NewBalance = tokenBalance,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return (companyId, vacancyId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
