using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Ops;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Ops;

public enum TestAccountSeedAction
{
    Created,
    Updated,
    Unchanged,
    Skipped
}

public sealed record TestAccountSeedRow(
    string AccountKey,
    string Email,
    string Role,
    TestAccountSeedAction Action,
    string Reason);

public sealed class TestAccountSeedProgress
{
    public string Step { get; set; } = "seed";
    public string? AccountKey { get; set; }
}

public sealed class TestAccountsSeedResult
{
    public List<TestAccountSeedRow> Rows { get; } = [];
    public bool AnySkipped => Rows.Any(r => r.Action == TestAccountSeedAction.Skipped);
    public int Created => Rows.Count(r => r.Action == TestAccountSeedAction.Created);
    public int Updated => Rows.Count(r => r.Action == TestAccountSeedAction.Updated);
    public int Unchanged => Rows.Count(r => r.Action == TestAccountSeedAction.Unchanged);
    public int Skipped => Rows.Count(r => r.Action == TestAccountSeedAction.Skipped);
    public bool AdminRealAccountConflict { get; set; }
}

public sealed class TestAccountsSeedService
{
    private readonly JobsyDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IPupilCodeService? _pupilCodes;

    public TestAccountsSeedService(
        JobsyDbContext db,
        IConfiguration configuration,
        IPupilCodeService? pupilCodes = null)
    {
        _db = db;
        _configuration = configuration;
        _pupilCodes = pupilCodes;
    }

    public async Task<TestAccountsSeedResult> SeedAsync(
        bool dryRun,
        IReadOnlySet<string>? onlyKeys,
        TestAccountSeedProgress? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress ??= new TestAccountSeedProgress();
        var result = new TestAccountsSeedResult();
        var domain = _configuration["TestAccounts:EmailDomain"] ?? "lobsy.nl";
        var usersByKey = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in TestAccountCatalog.All)
        {
            if (onlyKeys is { Count: > 0 }
                && !onlyKeys.Contains(entry.AccountKey))
            {
                continue;
            }

            progress.Step = "user";
            progress.AccountKey = entry.AccountKey;

            if (!TestAccountCatalog.RoleExistsInBuild(entry.Role))
            {
                result.Rows.Add(new(
                    entry.AccountKey,
                    TestAccountCatalog.BuildEmail(entry.EmailSlug, domain),
                    entry.Role.ToString(),
                    TestAccountSeedAction.Skipped,
                    $"role {entry.Role} does not exist in this build; skipped"));
                continue;
            }

            var email = TestAccountCatalog.BuildEmail(entry.EmailSlug, domain);
            var password = _configuration[$"TestAccounts:Password:{entry.AccountKey}"];
            if (string.IsNullOrWhiteSpace(password))
            {
                result.Rows.Add(new(
                    entry.AccountKey,
                    email,
                    entry.Role.ToString(),
                    TestAccountSeedAction.Skipped,
                    "password missing from TestAccounts:Password:<Key>"));
                continue;
            }

            try
            {
                RegistrationPasswordRules.Validate(password, required: true);
            }
            catch (ArgumentException)
            {
                result.Rows.Add(new(
                    entry.AccountKey,
                    email,
                    entry.Role.ToString(),
                    TestAccountSeedAction.Skipped,
                    $"password for {entry.AccountKey} doesn't meet the password rules"));
                continue;
            }

            var normalized = email.ToLowerInvariant();
            var existing = await _db.Users.FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalized,
                cancellationToken);

            if (existing is not null && !existing.IsTestAccount)
            {
                result.Rows.Add(new(
                    entry.AccountKey,
                    email,
                    entry.Role.ToString(),
                    TestAccountSeedAction.Skipped,
                    "a real account uses this e-mail"));
                if (entry.AccountKey.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    result.AdminRealAccountConflict = true;
                }

                continue;
            }

            if (existing is null)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = normalized,
                    FullName = entry.DisplayName,
                    Role = entry.Role,
                    IsActive = true,
                    IsTestAccount = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    ConsentVersion = PrivacyConstants.CurrentConsentVersion
                };
                ClearMfa(user);
                _db.Users.Add(user);
                await EnsurePasswordAsync(user, password, bumpSession: false, cancellationToken);
                usersByKey[entry.AccountKey] = user;
                result.Rows.Add(new(
                    entry.AccountKey, email, entry.Role.ToString(),
                    TestAccountSeedAction.Created, "created"));
                continue;
            }

            var changed = false;
            if (existing.Role != entry.Role)
            {
                existing.Role = entry.Role;
                changed = true;
            }

            if (!string.Equals(existing.FullName, entry.DisplayName, StringComparison.Ordinal))
            {
                existing.FullName = entry.DisplayName;
                changed = true;
            }

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                changed = true;
            }

            existing.IsTestAccount = true;
            ClearMfa(existing);
            var passwordRotated = await EnsurePasswordAsync(
                existing, password, bumpSession: true, cancellationToken);
            if (passwordRotated)
            {
                changed = true;
            }

            usersByKey[entry.AccountKey] = existing;
            result.Rows.Add(new(
                entry.AccountKey,
                email,
                entry.Role.ToString(),
                changed ? TestAccountSeedAction.Updated : TestAccountSeedAction.Unchanged,
                changed ? (passwordRotated ? "updated (password rotated)" : "updated") : "unchanged"));
        }

        if (result.AdminRealAccountConflict)
        {
            return result;
        }

        await EnsureSampleDataAsync(usersByKey, domain, dryRun, progress, cancellationToken);
        progress.Step = "save";
        progress.AccountKey = null;
        return result;
    }

    private static void ClearMfa(User user)
    {
        user.AuthenticatorEnabled = false;
        user.AuthenticatorSecret = null;
        user.RecoveryCodesHash = null;
        user.AuthenticatorEnrolledAtUtc = null;
        user.MfaFailedCount = 0;
        user.MfaLockoutUntilUtc = null;
        user.LastTotpTimeStep = null;
        user.LastMfaLockoutMailAtUtc = null;
    }

    private async Task<bool> EnsurePasswordAsync(
        User user,
        string password,
        bool bumpSession,
        CancellationToken cancellationToken)
    {
        var email = user.Email.Trim().ToLowerInvariant();
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken)
            ?? await _db.LocalAuthCredentials
                .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        if (credential is null)
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(password),
                FailedLoginCount = 0,
                LockoutUntil = null,
                LockoutCount = 0,
                LastLockoutAtUtc = null,
                LastLockoutMailAtUtc = null
            });
            return true;
        }

        credential.UserId = user.Id;
        credential.Email = email;
        credential.FailedLoginCount = 0;
        credential.LockoutUntil = null;
        credential.LockoutCount = 0;
        credential.LastLockoutAtUtc = null;
        credential.LastLockoutMailAtUtc = null;

        if (!JobsyPasswordHasher.Verify(password, credential.PasswordHash)
            || JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(password);
            if (bumpSession)
            {
                user.SessionVersion++;
            }

            return true;
        }

        return false;
    }

    private async Task EnsureSampleDataAsync(
        IReadOnlyDictionary<string, User> usersByKey,
        string domain,
        bool dryRun,
        TestAccountSeedProgress progress,
        CancellationToken cancellationToken)
    {
        progress.Step = "companies";
        progress.AccountKey = null;
        var root = await EnsureCompanyAsync(
            TestAccountsIds.RootCompany,
            "Testbedrijf Lobsy (test)",
            TestAccountsIds.RootKvk,
            "0001",
            CompanyType.Employer,
            parentId: null,
            new GeoPoint(52.0705, 4.3007),
            "Den Haag",
            cancellationToken);

        var haag = await EnsureCompanyAsync(
            TestAccountsIds.VestigingDenHaag,
            "Testvestiging Den Haag (test)",
            TestAccountsIds.HaagKvk,
            "0001",
            CompanyType.Employer,
            root.Id,
            new GeoPoint(52.0780, 4.3100),
            "Den Haag",
            cancellationToken);

        var delft = await EnsureCompanyAsync(
            TestAccountsIds.VestigingDelft,
            "Testvestiging Delft (test)",
            TestAccountsIds.DelftKvk,
            "0001",
            CompanyType.Employer,
            root.Id,
            new GeoPoint(52.0116, 4.3571),
            "Delft",
            cancellationToken);

        var bureau = await EnsureCompanyAsync(
            TestAccountsIds.IntermediaryCompany,
            "Testbureau Lobsy (test)",
            TestAccountsIds.IntermediaryKvk,
            "0001",
            CompanyType.Intermediary,
            parentId: null,
            new GeoPoint(52.0705, 4.3007),
            "Den Haag",
            cancellationToken);

        progress.Step = "memberships";
        if (usersByKey.TryGetValue("BranchManager", out var bm))
        {
            progress.AccountKey = "BranchManager";
            bm.CompanyId = haag.Id;
            await EnsureMembershipAsync(bm.Id, haag.Id, cancellationToken);
        }

        if (usersByKey.TryGetValue("EnterpriseManager", out var em))
        {
            progress.AccountKey = "EnterpriseManager";
            em.CompanyId = root.Id;
            await EnsureMembershipAsync(em.Id, root.Id, cancellationToken);
            await EnsureMembershipAsync(em.Id, haag.Id, cancellationToken);
            await EnsureMembershipAsync(em.Id, delft.Id, cancellationToken);
        }

        if (usersByKey.TryGetValue("RegionalManager", out var rm))
        {
            progress.AccountKey = "RegionalManager";
            rm.CompanyId = root.Id;
            await EnsureMembershipAsync(rm.Id, haag.Id, cancellationToken);
            await EnsureMembershipAsync(rm.Id, delft.Id, cancellationToken);
            await EnsureRegionAsync(root.Id, haag.Id, delft.Id, cancellationToken);
        }

        if (usersByKey.TryGetValue("Intermediary", out var im))
        {
            progress.AccountKey = "Intermediary";
            im.CompanyId = bureau.Id;
            await EnsureMembershipAsync(im.Id, bureau.Id, cancellationToken);
        }

        if (usersByKey.TryGetValue("SalesManager", out var sm))
        {
            progress.Step = "sales-profile";
            progress.AccountKey = "SalesManager";
            await EnsureSalesProfileAsync(sm.Id, cancellationToken);
            root.ReferredBySalesManagerUserId = sm.Id;
            root.SalesAttributedAtUtc ??= DateTime.UtcNow;
        }

        if (usersByKey.TryGetValue("Ambassadeur", out var am))
        {
            progress.Step = "ambassadeur-profile";
            progress.AccountKey = "Ambassadeur";
            await EnsureAmbassadeurProfileAsync(am.Id, cancellationToken);
        }

        progress.Step = "token-grant";
        progress.AccountKey = null;
        await EnsureTokenGrantAsync(root.Id, cancellationToken);
        progress.Step = "vacancies";
        await EnsureVacanciesAsync(haag, delft, bureau, dryRun, cancellationToken);

        if (usersByKey.TryGetValue("Candidate", out var candidate))
        {
            progress.Step = "candidate";
            progress.AccountKey = "Candidate";
            await EnsureCompleteCandidateAsync(candidate, cancellationToken);
        }

        if (usersByKey.TryGetValue("CandidateNew", out var candidateNew))
        {
            progress.Step = "candidate-new";
            progress.AccountKey = "CandidateNew";
            candidateNew.OpenForWork = false;
            candidateNew.PreferencesJson = null;
            candidateNew.HomeLocation = null;
            var existingOnboarding = await _db.CandidateOnboardings
                .FirstOrDefaultAsync(o => o.UserId == candidateNew.Id, cancellationToken);
            if (existingOnboarding is not null)
            {
                _db.CandidateOnboardings.Remove(existingOnboarding);
            }
        }

        if (usersByKey.TryGetValue("Teacher", out var teacher)
            && usersByKey.TryGetValue("SchoolAdmin", out var schoolAdmin))
        {
            progress.Step = "school";
            progress.AccountKey = "Teacher";
            await EnsureSchoolAsync(teacher, schoolAdmin, dryRun, cancellationToken);
        }

        _ = domain;
    }

    private async Task<Company> EnsureCompanyAsync(
        Guid id,
        string name,
        string kvk,
        string vestiging,
        CompanyType type,
        Guid? parentId,
        GeoPoint location,
        string city,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existing is not null)
        {
            existing.Name = name;
            existing.IsTestData = true;
            existing.KvkNumber = kvk;
            existing.KvkEstablishmentId = $"{kvk}_{vestiging}";
            existing.Type = type;
            existing.ParentCompanyId = parentId;
            existing.Location = location;
            existing.Address = $"Teststraat 1, {city}";
            existing.VerificationStatus = CompanyVerificationStatus.Verified;
            existing.VerificationMethod = CompanyVerificationMethod.AdminCreated;
            existing.VerifiedAtUtc ??= DateTime.UtcNow;
            existing.KvkVerificationStatus = KvkVerificationStatus.Verified;
            existing.HasReceivedWelcomeToken = true;
            return existing;
        }

        var company = new Company
        {
            Id = id,
            Name = name,
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_{vestiging}",
            Type = type,
            ParentCompanyId = parentId,
            Location = location,
            Address = $"Teststraat 1, {city}",
            IsTestData = true,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow,
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            KvkVerifiedAtUtc = DateTime.UtcNow,
            HasReceivedWelcomeToken = true,
            WelcomeTokenLedgerCredited = false
        };
        _db.Companies.Add(company);
        return company;
    }

    private async Task EnsureMembershipAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        if (!await _db.UserCompanies.AnyAsync(m => m.UserId == userId && m.CompanyId == companyId, cancellationToken))
        {
            _db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = companyId });
        }
    }

    private async Task EnsureRegionAsync(
        Guid orgId,
        Guid companyA,
        Guid companyB,
        CancellationToken cancellationToken)
    {
        var region = await _db.Regions.FirstOrDefaultAsync(r => r.Id == TestAccountsIds.Region, cancellationToken);
        if (region is null)
        {
            region = new Region
            {
                Id = TestAccountsIds.Region,
                OrganizationCompanyId = orgId,
                Name = "Testregio Haaglanden (test)"
            };
            _db.Regions.Add(region);
        }
        else
        {
            region.OrganizationCompanyId = orgId;
            region.Name = "Testregio Haaglanden (test)";
        }

        foreach (var companyId in new[] { companyA, companyB })
        {
            if (!await _db.RegionCompanies.AnyAsync(
                    rc => rc.RegionId == region.Id && rc.CompanyId == companyId,
                    cancellationToken))
            {
                _db.RegionCompanies.Add(new RegionCompany
                {
                    RegionId = region.Id,
                    CompanyId = companyId
                });
            }
        }
    }

    private async Task EnsureTokenGrantAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var existing = await _db.TokenTransactions
            .FirstOrDefaultAsync(t => t.Id == TestAccountsIds.TokenGrant, cancellationToken);
        if (existing is not null)
        {
            existing.Note = "test-seed";
            existing.Kind = TokenTransactionKind.Grant;
            existing.Amount = 25m;
            return;
        }

        _db.TokenTransactions.Add(new TokenTransaction
        {
            Id = TestAccountsIds.TokenGrant,
            CompanyId = companyId,
            Amount = 25m,
            Kind = TokenTransactionKind.Grant,
            Reason = TokenSpendReason.None,
            OldBalance = 0m,
            NewBalance = 25m,
            Note = "test-seed",
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task EnsureVacanciesAsync(
        Company haag,
        Company delft,
        Company bureau,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        // The CLI does not run the API hosted seeder. Categories are reference data the
        // vacancy FK requires; EnsureDefaults is idempotent when the API already seeded them.
        // Skip on dry-run so nothing is written (EnsureDefaults calls SaveChanges).
        if (!dryRun)
        {
            await new VacancyCategoryService(_db).EnsureDefaultsAsync(cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = today.AddMonths(3);

        await UpsertVacancyAsync(
            TestAccountsIds.VacancyHaag1,
            "Horeca medewerker (test)",
            haag,
            VacancyStatus.Active,
            new GeoPoint(52.0780, 4.3100),
            WorkType.Horeca,
            today,
            end,
            intermediaryId: null,
            cancellationToken);

        await UpsertVacancyAsync(
            TestAccountsIds.VacancyHaag2,
            "Kassamedewerker (test)",
            haag,
            VacancyStatus.Active,
            new GeoPoint(52.0790, 4.3110),
            WorkType.Winkel,
            today,
            end,
            intermediaryId: null,
            cancellationToken);

        await UpsertVacancyAsync(
            TestAccountsIds.VacancyDelftDraft,
            "Magazijnmedewerker (test)",
            delft,
            VacancyStatus.Draft,
            new GeoPoint(52.0116, 4.3571),
            WorkType.Logistiek,
            today,
            end,
            intermediaryId: null,
            cancellationToken);

        await UpsertVacancyAsync(
            TestAccountsIds.ClientVacancy,
            "Uitzendkracht horeca (test)",
            haag,
            VacancyStatus.Active,
            new GeoPoint(52.0785, 4.3105),
            WorkType.Horeca,
            today,
            end,
            bureau.Id,
            cancellationToken);
    }

    private async Task UpsertVacancyAsync(
        Guid id,
        string title,
        Company company,
        VacancyStatus status,
        GeoPoint location,
        WorkType workTypes,
        DateOnly start,
        DateOnly end,
        Guid? intermediaryId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (existing is not null)
        {
            existing.Title = title;
            existing.CompanyId = company.Id;
            existing.Status = status;
            existing.IsTestData = true;
            existing.Location = location;
            existing.WorkTypes = workTypes;
            existing.IntermediaryCompanyId = intermediaryId;
            if (status == VacancyStatus.Active)
            {
                existing.PublishedAtUtc ??= DateTime.UtcNow;
            }

            return;
        }

        _db.Vacancies.Add(new Vacancy
        {
            Id = id,
            Title = title,
            Description = "Testdata voor acceptatie-rollen. Niet zichtbaar voor echte gebruikers.",
            HourlyWage = 14.50m,
            StartDate = start,
            EndDate = end,
            Status = status,
            CompanyId = company.Id,
            IntermediaryCompanyId = intermediaryId,
            Location = location,
            RequiredTransport = TransportMode.Bike | TransportMode.PublicTransport,
            WorkTypes = workTypes,
            Kind = VacancyKind.Regular,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            IsTestData = true,
            ContentModerationPassed = true,
            CreatedAtUtc = DateTime.UtcNow,
            PublishedAtUtc = status == VacancyStatus.Active ? DateTime.UtcNow : null
        });
    }

    private async Task EnsureCompleteCandidateAsync(User candidate, CancellationToken cancellationToken)
    {
        candidate.OpenForWork = true;
        candidate.HomeLocation = new GeoPoint(52.0780, 4.3100);
        candidate.DateOfBirth = new DateOnly(1998, 4, 12);
        candidate.PreferencesJson =
            """{"roles":["horeca","retail"],"maxTravelMinutes":30,"educations":["MBO"]}""";
        candidate.CandidateHowToCompletedAt ??= DateTime.UtcNow;
        candidate.TalentPoolConsentAt ??= DateTime.UtcNow;
        candidate.TalentPoolConsentVersion = PrivacyConstants.CurrentConsentVersion;
        candidate.TestAiConsentAt ??= DateTime.UtcNow;
        candidate.TestAiConsentVersion = PrivacyConstants.CurrentConsentVersion;

        var onboarding = await _db.CandidateOnboardings
            .FirstOrDefaultAsync(o => o.UserId == candidate.Id, cancellationToken);
        if (onboarding is null)
        {
            onboarding = new CandidateOnboarding
            {
                Id = TestAccountsIds.OnboardingComplete,
                UserId = candidate.Id,
                CurrentStep = OnboardingWizardCatalog.V3StepCount,
                WizardVersion = OnboardingWizardCatalog.WizardVersionV3,
                FinishReached = true,
                StartedAtUtc = DateTime.UtcNow.AddDays(-2),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow,
                Source = "test-seed"
            };
            _db.CandidateOnboardings.Add(onboarding);
        }
        else
        {
            onboarding.FinishReached = true;
            onboarding.CompletedAtUtc ??= DateTime.UtcNow;
            onboarding.WizardVersion = OnboardingWizardCatalog.WizardVersionV3;
            onboarding.CurrentStep = OnboardingWizardCatalog.V3StepCount;
            onboarding.UpdatedAtUtc = DateTime.UtcNow;
        }

        MarkJourneyStepsDone(onboarding);

        await EnsureCompletedProfileAsync(candidate.Id, cancellationToken);

        if (!await _db.Applications.AnyAsync(a => a.Id == TestAccountsIds.Application, cancellationToken))
        {
            _db.Applications.Add(new Application
            {
                Id = TestAccountsIds.Application,
                VacancyId = TestAccountsIds.VacancyHaag1,
                CandidateUserId = candidate.Id,
                CandidateName = candidate.FullName,
                CandidateEmail = candidate.Email,
                CandidateCity = "Den Haag",
                PreferredTransport = "Bike",
                EstimatedTravelMinutes = 15,
                Status = ApplicationStatus.Pending,
                ConsentAcceptedAt = DateTime.UtcNow,
                ConsentVersion = PrivacyConstants.CurrentConsentVersion,
                WorkPermitConfirmed = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static void MarkJourneyStepsDone(CandidateOnboarding onboarding)
    {
        var json = string.IsNullOrWhiteSpace(onboarding.StepsJson) ? "[]" : onboarding.StepsJson;
        var now = DateTime.UtcNow;
        for (var step = 1; step <= OnboardingWizardCatalog.V3StepCount; step++)
        {
            var ev = OnboardingStepAnalytics.Parse(json).FirstOrDefault(e => e.Step == step);
            if (ev?.CompletedAtUtc is null)
            {
                json = OnboardingStepAnalytics.MarkCompleted(
                    json, step, now, OnboardingWizardCatalog.WizardVersionV3);
            }
        }

        onboarding.StepsJson = json;
    }

    private async Task EnsureCompletedProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await EnsureSeedCompetencyAsync(userId, now, cancellationToken);
        await EnsureSeedValuesAsync(userId, now, cancellationToken);
        await EnsureSeedCultureAsync(userId, now, cancellationToken);
        await EnsureSeedCareerAsync(userId, now, cancellationToken);
    }

    /// <summary>
    /// Rewrites the seeded complete candidate's assessments when answers or scores
    /// diverge from the canonical seed (placeholder 70/65/60/55/50, gaps, career "done" without scores).
    /// Does not touch preferences or the new candidate. Caller saves.
    /// </summary>
    public async Task<bool> RepairCompleteCandidateAssessmentsAsync(CancellationToken cancellationToken = default)
    {
        var domain = _configuration["TestAccounts:EmailDomain"] ?? "lobsy.nl";
        var entry = TestAccountCatalog.FindByKey("Candidate");
        if (entry is null)
        {
            return false;
        }

        var email = TestAccountCatalog.BuildEmail(entry.EmailSlug, domain);
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.IsTestAccount && u.Email == email,
            cancellationToken);
        if (user is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        await EnsureSeedCompetencyAsync(user.Id, now, cancellationToken);
        await EnsureSeedValuesAsync(user.Id, now, cancellationToken);
        await EnsureSeedCultureAsync(user.Id, now, cancellationToken);
        await EnsureSeedCareerAsync(user.Id, now, cancellationToken);

        var onboarding = await _db.CandidateOnboardings
            .FirstOrDefaultAsync(o => o.UserId == user.Id, cancellationToken);
        if (onboarding is not null)
        {
            MarkJourneyStepsDone(onboarding);
        }

        return true;
    }

    private static bool SameAnswers(IReadOnlyDictionary<int, int> stored, IReadOnlyDictionary<int, int> expected)
    {
        if (stored.Count != expected.Count)
        {
            return false;
        }

        foreach (var (id, value) in expected)
        {
            if (!stored.TryGetValue(id, out var got) || got != value)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<int, int> FullLikert(int count)
    {
        var map = new Dictionary<int, int>(count);
        for (var i = 1; i <= count; i++)
        {
            // Prime stride so domain blocks do not all average to the same likert.
            map[i] = ((i * 3 + (i / 7)) % 5) + 1;
        }

        return map;
    }

    private async Task EnsureSeedCompetencyAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var answers = FullLikert(CompetencyTestCatalog.QuestionCount);
        var scores = CompetencyTestCatalog.Score(answers)!;
        var row = await _db.CandidateCompetencies.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is not null
            && row.Status == CandidateCompetencyStatuses.Completed
            && SameAnswers(CompetencyTestCatalog.ParseAnswersJson(row.AnswersJson), answers)
            && row.SamenwerkenPercent == scores.Samenwerken
            && row.ResultaatgerichtheidPercent == scores.Resultaatgerichtheid
            && row.StressbestendigheidPercent == scores.Stressbestendigheid
            && row.InnovatiePercent == scores.Innovatie
            && row.ExtraversiePercent == scores.Extraversie)
        {
            return;
        }

        row ??= _db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = now
        }).Entity;
        row.Status = CandidateCompetencyStatuses.Completed;
        row.AnswersJson = CompetencyTestCatalog.SerializeAnswers(answers);
        row.SamenwerkenPercent = scores.Samenwerken;
        row.ResultaatgerichtheidPercent = scores.Resultaatgerichtheid;
        row.StressbestendigheidPercent = scores.Stressbestendigheid;
        row.InnovatiePercent = scores.Innovatie;
        row.ExtraversiePercent = scores.Extraversie;
        row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(CompetencyTestCatalog.DeriveMatchTags(scores));
        row.UpdatedAtUtc = now;
        row.CompletedAtUtc ??= now;
    }

    private async Task EnsureSeedValuesAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var answers = FullLikert(SchwartzValuesCatalog.QuestionCount);
        var scores = SchwartzValuesCatalog.Score(answers)!;
        var row = await _db.CandidateValuesProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is not null
            && row.Status == CandidateCompetencyStatuses.Completed
            && SameAnswers(SchwartzValuesCatalog.ParseAnswers(row.AnswersJson), answers)
            && row.AutonomyPercent == scores.Autonomy
            && row.ConnectionPercent == scores.Connection
            && row.AchievementPercent == scores.Achievement
            && row.StabilityPercent == scores.Stability
            && row.ImpactPercent == scores.Impact)
        {
            return;
        }

        row ??= _db.CandidateValuesProfiles.Add(new CandidateValuesProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = now
        }).Entity;
        row.Status = CandidateCompetencyStatuses.Completed;
        row.AnswersJson = SchwartzValuesCatalog.SerializeAnswers(answers);
        row.AutonomyPercent = scores.Autonomy;
        row.ConnectionPercent = scores.Connection;
        row.AchievementPercent = scores.Achievement;
        row.StabilityPercent = scores.Stability;
        row.ImpactPercent = scores.Impact;
        row.MatchTagsJson = SchwartzValuesCatalog.SerializeTags(SchwartzValuesCatalog.DeriveMatchTags(scores));
        row.UpdatedAtUtc = now;
        row.CompletedAtUtc ??= now;
    }

    private async Task EnsureSeedCultureAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var answers = FullLikert(CulturePersonalityCatalog.QuestionCount);
        var scores = CulturePersonalityCatalog.Score(answers)!;
        var row = await _db.CandidateCulturePersonalityProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is not null
            && row.Status == CandidateCompetencyStatuses.Completed
            && SameAnswers(CulturePersonalityCatalog.ParseAnswers(row.AnswersJson), answers)
            && row.AutonomyPercent == scores.Autonomy
            && row.InformalPercent == scores.Informal
            && row.CollaborationPercent == scores.Collaboration
            && row.FlexibilityPercent == scores.Flexibility
            && row.InnovationPercent == scores.Innovation
            && row.PeopleFirstPercent == scores.PeopleFirst
            && row.OpennessPercent == scores.Openness
            && row.ConscientiousnessPercent == scores.Conscientiousness
            && row.ExtraversionPercent == scores.Extraversion
            && row.AgreeablenessPercent == scores.Agreeableness
            && row.EmotionalStabilityPercent == scores.EmotionalStability)
        {
            return;
        }

        row ??= _db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = now
        }).Entity;
        row.Status = CandidateCompetencyStatuses.Completed;
        row.AnswersJson = CulturePersonalityCatalog.SerializeAnswers(answers);
        row.AutonomyPercent = scores.Autonomy;
        row.InformalPercent = scores.Informal;
        row.CollaborationPercent = scores.Collaboration;
        row.FlexibilityPercent = scores.Flexibility;
        row.InnovationPercent = scores.Innovation;
        row.PeopleFirstPercent = scores.PeopleFirst;
        row.OpennessPercent = scores.Openness;
        row.ConscientiousnessPercent = scores.Conscientiousness;
        row.ExtraversionPercent = scores.Extraversion;
        row.AgreeablenessPercent = scores.Agreeableness;
        row.EmotionalStabilityPercent = scores.EmotionalStability;
        row.UpdatedAtUtc = now;
        row.CompletedAtUtc ??= now;
    }

    private async Task EnsureSeedCareerAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var answers = FullLikert(CareerTestCatalog.QuestionCount);
        var scores = CareerTestCatalog.Score(answers)!;
        var tags = CareerTestCatalog.DeriveRiasecTags(scores);
        var row = await _db.CandidateCareerInterests.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is not null
            && row.Status == CandidateCompetencyStatuses.Completed
            && SameAnswers(CareerTestCatalog.ParseAnswersJson(row.AnswersJson), answers)
            && row.RealisticPercent == scores.Realistic
            && row.InvestigativePercent == scores.Investigative
            && row.ArtisticPercent == scores.Artistic
            && row.SocialPercent == scores.Social
            && row.EnterprisingPercent == scores.Enterprising
            && row.ConventionalPercent == scores.Conventional)
        {
            return;
        }

        row ??= _db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = now
        }).Entity;
        row.Status = CandidateCompetencyStatuses.Completed;
        row.AnswersJson = CareerTestCatalog.SerializeAnswers(answers);
        row.RealisticPercent = scores.Realistic;
        row.InvestigativePercent = scores.Investigative;
        row.ArtisticPercent = scores.Artistic;
        row.SocialPercent = scores.Social;
        row.EnterprisingPercent = scores.Enterprising;
        row.ConventionalPercent = scores.Conventional;
        row.HollandCode = CareerTestCatalog.HollandCode(scores);
        row.RiasecTagsJson = CareerTestCatalog.SerializeTags(tags);
        row.MatchTagsJson = CareerTestCatalog.SerializeTags(tags);
        row.UpdatedAtUtc = now;
        row.CompletedAtUtc ??= now;
    }

    private async Task EnsureSalesProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existing = await _db.SalesManagerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            existing.TrackingCode ??= "TEST-SM01";
            existing.OnboardingCompletedAt ??= now;
            existing.AgreementSignedAt ??= now;
            existing.AgreementVersion ??= "test-seed";
            existing.UpdatedAt = now;
            return;
        }

        _db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = TestAccountsIds.SalesProfile,
            UserId = userId,
            CompanyName = "Test Sales Lobsy (test)",
            KvkNumber = "00000995",
            City = "Den Haag",
            Country = "NL",
            TrackingCode = "TEST-SM01",
            AgreementSignedAt = now,
            AgreementVersion = "test-seed",
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private async Task EnsureAmbassadeurProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existing = await _db.AmbassadeurProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            existing.TrackingCode ??= "TEST-AM01";
            existing.OnboardingCompletedAt ??= now;
            existing.AgreementSignedAt ??= now;
            existing.AgreementVersion ??= AmbassadeurCommissionRules.CurrentAgreementVersion;
            existing.UpdatedAt = now;
            return;
        }

        _db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = TestAccountsIds.AmbassadeurProfile,
            UserId = userId,
            CompanyName = "Test Ambassadeur Lobsy (test)",
            KvkNumber = "00000996",
            City = "Den Haag",
            Country = "NL",
            TrackingCode = "TEST-AM01",
            BaseCommissionPercentage = 5.0m,
            AgreementSignedAt = now,
            AgreementVersion = AmbassadeurCommissionRules.CurrentAgreementVersion,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private async Task EnsureSchoolAsync(
        User teacher,
        User schoolAdmin,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == TestAccountsIds.School, cancellationToken);
        if (school is null)
        {
            school = new School
            {
                Id = TestAccountsIds.School,
                Name = "Testschool Lobsy (test)",
                City = "Den Haag",
                AllowedEmailDomains = """["lobsy.nl"]""",
                IsActive = true,
                IsTestData = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = schoolAdmin.Id,
                ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
                ProcessorAgreementVersion = "test-seed"
            };
            _db.Schools.Add(school);
        }
        else
        {
            school.IsTestData = true;
            school.IsActive = true;
        }

        teacher.SchoolId = school.Id;
        schoolAdmin.SchoolId = school.Id;
        var schoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));

        var schoolClass = await _db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == TestAccountsIds.SchoolClass, cancellationToken);
        var insertedVo = schoolClass is null;
        if (schoolClass is null)
        {
            schoolClass = new SchoolClass
            {
                Id = TestAccountsIds.SchoolClass,
                SchoolId = school.Id,
                Name = "1A",
                Level = SchoolLevel.VmboGt,
                QuestionSet = PupilQuestionSet.Vo,
                Year = 1,
                SchoolYearStart = schoolYearStart,
                PupilCount = 5,
                IsTestData = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.SchoolClasses.Add(schoolClass);
        }
        else
        {
            schoolClass.IsTestData = true;
            schoolClass.Name = "1A";
            schoolClass.Level = SchoolLevel.VmboGt;
            schoolClass.QuestionSet = PupilQuestionSet.Vo;
            schoolClass.Year = 1;
            schoolClass.SchoolYearStart = schoolYearStart;
            schoolClass.PupilCount = 5;
        }

        await EnsurePupilCodesAsync(schoolClass, insertedVo, dryRun, cancellationToken);
        await EnsureTeacherAssignmentAsync(teacher.Id, schoolClass.Id, cancellationToken);

        var groepClass = await _db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == TestAccountsIds.SchoolClassGroep78, cancellationToken);
        var insertedGroep = groepClass is null;
        if (groepClass is null)
        {
            groepClass = new SchoolClass
            {
                Id = TestAccountsIds.SchoolClassGroep78,
                SchoolId = school.Id,
                Name = "7A",
                Level = SchoolLevel.Groep78,
                QuestionSet = PupilQuestionSet.Groep78,
                Year = 7,
                SchoolYearStart = schoolYearStart,
                PupilCount = 5,
                IsTestData = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.SchoolClasses.Add(groepClass);
        }
        else
        {
            groepClass.IsTestData = true;
            groepClass.Name = "7A";
            groepClass.Level = SchoolLevel.Groep78;
            groepClass.QuestionSet = PupilQuestionSet.Groep78;
            groepClass.Year = 7;
            groepClass.SchoolYearStart = schoolYearStart;
            groepClass.PupilCount = 5;
        }

        await EnsurePupilCodesAsync(groepClass, insertedGroep, dryRun, cancellationToken);
        await EnsureTeacherAssignmentAsync(teacher.Id, groepClass.Id, cancellationToken);
    }

    private async Task EnsureTeacherAssignmentAsync(
        Guid teacherId,
        Guid schoolClassId,
        CancellationToken cancellationToken)
    {
        if (!await _db.TeacherClassAssignments.AnyAsync(
                a => a.TeacherUserId == teacherId && a.SchoolClassId == schoolClassId,
                cancellationToken))
        {
            _db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = teacherId,
                SchoolClassId = schoolClassId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Inserts the class before generating codes (FK), then tops up to <see cref="SchoolClass.PupilCount"/>.
    /// A later seed with <c>Scholen:CodeHmacKey</c> fills codes that were skipped earlier.
    /// </summary>
    private async Task EnsurePupilCodesAsync(
        SchoolClass schoolClass,
        bool inserted,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (dryRun || _pupilCodes is null)
        {
            return;
        }

        if (inserted)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var existing = await _db.PupilCodes.CountAsync(
            c => c.SchoolClassId == schoolClass.Id, cancellationToken);
        var missing = schoolClass.PupilCount - existing;
        if (missing > 0)
        {
            await _pupilCodes.GenerateAsync(missing, schoolClass, cancellationToken);
        }
    }
}
