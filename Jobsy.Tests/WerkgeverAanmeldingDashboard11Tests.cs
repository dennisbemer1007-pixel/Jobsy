using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class EmployerVisibilityPanel11Tests
{
    [Theory]
    [InlineData(CompanyVerificationStatus.Unverified, false)]
    [InlineData(CompanyVerificationStatus.Pending, false)]
    [InlineData(CompanyVerificationStatus.Rejected, false)]
    [InlineData(CompanyVerificationStatus.Verified, true)]
    public void Panel_rows_match_PublicVisibility(CompanyVerificationStatus status, bool expectedVisible)
    {
        var company = new Company { VerificationStatus = status };
        Assert.Equal(expectedVisible, PublicVisibility.IsCompanyPublic(company));
        var rows = EmployerVisibilityPanel.ForCompany(company);
        Assert.Equal(4, rows.Count);
        Assert.All(rows, r => Assert.Equal(expectedVisible, r.Visible));
    }
}

public class EmployerLinks11Tests
{
    [Fact]
    public void Targets_today_routes_absent_case()
    {
        Assert.Equal("/home", EmployerLinks.Home);
        Assert.Equal("/register/verifieren", EmployerLinks.Verify);
        Assert.Equal("/register/verifieren/brief", EmployerLinks.VerifyBrief);
        Assert.Equal("/register/bedrijf", EmployerLinks.AboutCompany);
        Assert.Equal("/employer/vacancies", EmployerLinks.Vacancies);
        Assert.Equal("/branch/vacancies", EmployerLinks.VacanciesForRole(isBranchManagerOnly: true));
        Assert.Equal("/employer/users", EmployerLinks.Users);
        Assert.Equal("/employer/branches", EmployerLinks.Branches);
        Assert.Equal("/employer/culture", EmployerLinks.CultureProfile);
    }
}

public class VestigingSuggestion11Tests
{
    [Fact]
    public async Task New_free_vestiging_becomes_suggestion_NietNu_hides_90_days_never_auto_added()
    {
        await using var db = CreateDb();
        var rootId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = rootId,
            Name = "Org",
            KvkNumber = "99990111",
            Address = "Utrecht",
            Location = new GeoPoint(52.09, 5.12),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Manual,
            VerifiedAtUtc = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = "bm@example.com",
            FullName = "BM",
            Role = UserRole.EnterpriseManager,
            CompanyId = rootId,
            IsActive = true
        });
        db.CompanyRegistrations.Add(new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            KvkNumber = "99990111",
            KvkEstablishmentId = "99990111_0001",
            EstablishmentName = "Org",
            EstablishmentAddress = "Utrecht",
            Scope = RegistrationScope.Organization,
            ContactName = "BM",
            ContactEmail = "bm@example.com",
            ActivationToken = "tok",
            Status = CompanyRegistrationStatus.Activated,
            CreatedOrganizationCompanyId = rootId,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ActivatedAt = DateTime.UtcNow.AddDays(-10)
        });
        // Existing vestiging already on Lobsy.
        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "V1",
            KvkNumber = "99990111",
            KvkEstablishmentId = "99990111_0001",
            Address = "Utrecht",
            Location = new GeoPoint(52.09, 5.12),
            ParentCompanyId = rootId,
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.InheritedFromOrganization
        });
        await db.SaveChangesAsync();

        var kvk = new SuggestionKvkStub(
        [
            new KvkEstablishmentResult(
                "99990111", "0001", "99990111_0001", "V1", "Utrecht", 52.09, 5.12, IsInUse: true),
            new KvkEstablishmentResult(
                "99990111", "0002", "99990111_0002", "V2", "Amersfoort", 52.15, 5.38, IsInUse: false)
        ]);
        var usage = new BudgetOkUsage();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new VestigingSuggestionService(db, kvk, usage, cache, NullLogger<VestigingSuggestionService>.Instance);

        var open = await sut.ListOpenAsync(rootId);
        Assert.Single(open);
        Assert.Equal("99990111_0002", open[0].KvkEstablishmentId);

        await sut.DismissAsync(rootId, "99990111_0002", userId);
        open = await sut.ListOpenAsync(rootId);
        Assert.Empty(open);

        var dismiss = await db.DismissedVestigingSuggestions.SingleAsync();
        Assert.True(dismiss.HiddenUntilUtc > DateTime.UtcNow.AddDays(89));

        // Still only one child company — never auto-added.
        Assert.Equal(1, await db.Companies.CountAsync(c => c.ParentCompanyId == rootId));

        await sut.AcceptAsync(rootId, "99990111_0002", userId);
        Assert.Equal(2, await db.Companies.CountAsync(c => c.ParentCompanyId == rootId));
        var added = await db.Companies.SingleAsync(c => c.KvkEstablishmentId == "99990111_0002");
        Assert.Equal(CompanyVerificationStatus.Verified, added.VerificationStatus);
        Assert.Equal(CompanyVerificationMethod.InheritedFromOrganization, added.VerificationMethod);
        Assert.Empty(await sut.ListOpenAsync(rootId));
    }

    [Fact]
    public async Task Respects_kvk_budget_warning()
    {
        await using var db = CreateDb();
        var kvk = new SuggestionKvkStub([]);
        var usage = new BudgetWarnUsage();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new VestigingSuggestionService(db, kvk, usage, cache, NullLogger<VestigingSuggestionService>.Instance);
        Assert.Equal(0, await sut.RefreshSuggestionsAsync());
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("wa11-suggest-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class SuggestionKvkStub(IReadOnlyList<KvkEstablishmentResult> items) : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(null);

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(items);

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkEstablishmentsLookup.Ok(items));
    }

    private sealed class BudgetOkUsage : IKvkUsageCounter
    {
        public Task IncrementAsync(string callType, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<KvkUsageSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new KvkUsageSummary(0, 0, 0, 10, 50_000, false));
    }

    private sealed class BudgetWarnUsage : IKvkUsageCounter
    {
        public Task IncrementAsync(string callType, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<KvkUsageSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new KvkUsageSummary(0, 0, 0, 49_000, 50_000, true));
    }
}

public class EmployerOnboardingStatus11Tests
{
    [Fact]
    public async Task Banner_shown_for_unverified_employer_not_for_verified_or_candidate()
    {
        await using var db = CreateDb();
        var (unverifiedUser, _) = await SeedAsync(db, CompanyVerificationStatus.Unverified);
        var (verifiedUser, _) = await SeedAsync(db, CompanyVerificationStatus.Verified, kvk: "99990121");
        var candidateId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = "cand@example.com",
            FullName = "Cand",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var flow = new FakeVerificationFlow();
        var suggestions = new FakeSuggestions();
        var lender = new FakeLender();
        var sut = new EmployerOnboardingStatusService(db, flow, suggestions, lender);

        var unverified = await sut.GetAsync(unverifiedUser);
        Assert.NotNull(unverified);
        Assert.True(unverified!.ShowUnverifiedBanner);
        Assert.Contains(unverified.Checklist, c => c.Key == "verify" && !c.Done);
        Assert.All(unverified.Visibility, v => Assert.False(v.Visible));

        var verified = await sut.GetAsync(verifiedUser);
        Assert.NotNull(verified);
        Assert.False(verified!.ShowUnverifiedBanner);
        Assert.True(verified.ShowSuccessBanner);

        Assert.Null(await sut.GetAsync(candidateId));
    }

    [Fact]
    public async Task Checklist_complete_when_verified_and_steps_done()
    {
        await using var db = CreateDb();
        var (userId, rootId) = await SeedAsync(db, CompanyVerificationStatus.Verified, kvk: "99990122");
        var root = await db.Companies.FirstAsync(c => c.Id == rootId);
        root.WorkTypeLabels = "Zorg|Schoonmaak";
        root.LastAutoPublishedVacancyCount = 2;
        db.CompanyCultureProfiles.Add(new CompanyCultureProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = rootId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            Source = CompanyCultureSources.Quick
        });
        db.CompanyValuesProfiles.Add(new CompanyValuesProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = rootId
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = rootId,
            Title = "Zorg",
            Description = "x",
            Status = VacancyStatus.Active,
            PublishOnVerification = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        var colleague = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = colleague,
            Email = "col@example.com",
            FullName = "Col",
            Role = UserRole.BranchManager,
            CompanyId = rootId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = colleague, CompanyId = rootId });
        await db.SaveChangesAsync();

        var sut = new EmployerOnboardingStatusService(db, new FakeVerificationFlow(), new FakeSuggestions(), new FakeLender());
        var status = await sut.GetAsync(userId);
        Assert.NotNull(status);
        Assert.True(status!.Checklist.Single(c => c.Key == "account").Done);
        Assert.True(status.Checklist.Single(c => c.Key == "verify").Done);
        Assert.True(status.Checklist.Single(c => c.Key == "about").Done);
        Assert.True(status.Checklist.Single(c => c.Key == "vacancy").Done);
        Assert.True(status.Checklist.Single(c => c.Key == "invite").Done);
        Assert.True(status.ChecklistComplete);
        Assert.Equal(2, status.LastAutoPublishedVacancyCount);
    }

    private static async Task<(Guid UserId, Guid RootId)> SeedAsync(
        JobsyDbContext db,
        CompanyVerificationStatus status,
        string kvk = "99990120")
    {
        var rootId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = rootId,
            Name = "Test Co",
            KvkNumber = kvk,
            Address = "Utrecht",
            Location = new GeoPoint(52.09, 5.12),
            Type = CompanyType.Employer,
            VerificationStatus = status,
            VerificationMethod = status == CompanyVerificationStatus.Verified
                ? CompanyVerificationMethod.BusinessEmail
                : CompanyVerificationMethod.None,
            VerifiedAtUtc = status == CompanyVerificationStatus.Verified ? DateTime.UtcNow : null
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = $"{kvk}@example.com",
            FullName = "Mgr",
            Role = UserRole.EnterpriseManager,
            CompanyId = rootId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = rootId });
        await db.SaveChangesAsync();
        return (userId, rootId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("wa11-onboard-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeVerificationFlow : ICompanyVerificationFlowService
    {
        public Task<CompanyVerificationOptionsView> GetOptionsAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("not needed");

        public Task<EmailVerificationStartResult> StartEmailAsync(Guid userId, string email, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VerificationConfirmResult> ConfirmEmailAsync(Guid userId, string code, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<LetterVerificationStartResult> RequestLetterAsync(Guid userId, bool isResend, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VerificationConfirmResult> ConfirmLetterAsync(Guid userId, string code, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ManualVerificationStartResult> RequestManualAsync(
            Guid userId, string reason, string? message, IReadOnlyList<string>? attachmentIds, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<LetterVerificationStartResult> AdminSendLetterAsync(Guid companyId, Guid adminUserId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeSuggestions : IVestigingSuggestionService
    {
        public Task<int> RefreshSuggestionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<EmployerVestigingSuggestionDto>> ListOpenAsync(Guid rootCompanyId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmployerVestigingSuggestionDto>>([]);
        public Task DismissAsync(Guid rootCompanyId, string kvkEstablishmentId, Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task AcceptAsync(Guid rootCompanyId, string kvkEstablishmentId, Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeLender : ILenderRegistrationCheck
    {
        public Task<LenderRegistrationState> GetStateAsync(Guid bureauOrgId, CancellationToken cancellationToken = default)
            => Task.FromResult(new LenderRegistrationState(bureauOrgId, LenderRegistrationStatuses.Verified, null, null, null, null, null, null));

        public Task<LenderRegistrationState> StartForNewBureauAsync(Guid bureauOrgId, string kvkNumber, CancellationToken cancellationToken = default)
            => GetStateAsync(bureauOrgId, cancellationToken);

        public Task RecordDecisionAsync(Guid bureauOrgId, LenderRegistrationDecision decision, Guid adminUserId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public bool CanPublish(LenderRegistrationState state) => true;
    }
}
