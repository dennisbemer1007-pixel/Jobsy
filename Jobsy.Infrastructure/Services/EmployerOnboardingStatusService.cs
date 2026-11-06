using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class EmployerOnboardingStatusService : IEmployerOnboardingStatusService
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyVerificationFlowService _verification;
    private readonly IVestigingSuggestionService _suggestions;
    private readonly ILenderRegistrationCheck _lender;

    public EmployerOnboardingStatusService(
        JobsyDbContext db,
        ICompanyVerificationFlowService verification,
        IVestigingSuggestionService suggestions,
        ILenderRegistrationCheck lender)
    {
        _db = db;
        _verification = verification;
        _suggestions = suggestions;
        _lender = lender;
    }

    public async Task<EmployerOnboardingStatusDto?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !JobsyRoles.IsEmployer(user.Role))
        {
            return null;
        }

        var membershipIds = await _db.UserCompanies.AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CompanyId)
            .ToListAsync(cancellationToken);
        if (user.CompanyId is Guid primary)
        {
            membershipIds.Add(primary);
        }

        membershipIds = membershipIds.Distinct().ToList();
        if (membershipIds.Count == 0)
        {
            return null;
        }

        var companies = await _db.Companies.AsNoTracking()
            .Where(c => membershipIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        if (companies.Count == 0)
        {
            return null;
        }

        // Prefer an organisation root the user belongs to; else walk parent of first membership.
        var root = companies.FirstOrDefault(c => c.ParentCompanyId is null)
                   ?? await ResolveRootAsync(companies[0], cancellationToken);
        if (root is null)
        {
            return null;
        }

        CompanyVerificationOptionsView? options = null;
        try
        {
            options = await _verification.GetOptionsAsync(userId, cancellationToken);
        }
        catch
        {
            // Options require manager role on the root; still show panel from company row.
        }

        var letter = options?.ActiveLetterId is Guid letterId
            ? await _db.CompanyVerificationLetters.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == letterId, cancellationToken)
            : null;

        var rejectionReason = root.VerificationStatus == CompanyVerificationStatus.Rejected
            ? await _db.CompanyVerificationDecisions.AsNoTracking()
                .Where(d => d.CompanyId == root.Id && d.Outcome == CompanyVerificationStatus.Rejected)
                .OrderByDescending(d => d.CreatedAtUtc)
                .Select(d => d.Reason)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var manualPending = options?.ManualPending == true
            || (root.ManualVerificationOpenedAtUtc is not null
                && root.ManualVerificationClosedAtUtc is null
                && root.VerificationStatus is CompanyVerificationStatus.Pending
                    or CompanyVerificationStatus.Unverified);

        DateTime? manualReplyBy = null;
        if (manualPending && root.ManualVerificationOpenedAtUtc is DateTime opened)
        {
            manualReplyBy = AddBusinessDays(opened, 2);
        }

        var letterUnderway = options?.ActiveLetterId is not null
            && root.VerificationStatus != CompanyVerificationStatus.Verified;

        var failed = letter?.FailedAttempts ?? 0;
        var attemptsLeft = Math.Max(0, VerificationCodes.MaxFailedAttempts - failed);

        var isBureau = root.Type == CompanyType.Intermediary
            || await _db.CompanyRegistrations.AsNoTracking()
                .AnyAsync(
                    r => (r.CreatedOrganizationCompanyId == root.Id || r.CreatedBranchCompanyId == root.Id)
                         && r.Status == CompanyRegistrationStatus.Activated
                         && r.PrimarySbiCode != null
                         && r.PrimarySbiCode.StartsWith("78"),
                    cancellationToken);

        var lenderPending = false;
        if (isBureau && root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            var lender = await _lender.GetStateAsync(root.Id, cancellationToken);
            lenderPending = !_lender.CanPublish(lender);
        }

        var treeIds = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == root.Id || c.ParentCompanyId == root.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var hasReady = await _db.Vacancies.AsNoTracking()
            .AnyAsync(
                v => treeIds.Contains(v.CompanyId) && v.PublishOnVerification,
                cancellationToken);
        var hasAnyVacancy = await _db.Vacancies.AsNoTracking()
            .AnyAsync(v => treeIds.Contains(v.CompanyId), cancellationToken);

        var branchesFilled = !string.IsNullOrWhiteSpace(root.WorkTypeLabels);
        // IsCompleted() is case-insensitive C# and cannot be translated. Compare the stored status.
        var cultureDone = await _db.CompanyCultureProfiles.AsNoTracking()
            .AnyAsync(
                p => p.CompanyId == root.Id && p.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        var valuesDone = await _db.CompanyValuesProfiles.AsNoTracking()
            .AnyAsync(p => p.CompanyId == root.Id, cancellationToken);
        var engagementDone = await _db.CompanyEngagementClaims.AsNoTracking()
            .AnyAsync(
                c => c.CompanyId == root.Id
                     && c.Status != CompanyEngagementStatuses.Removed,
                cancellationToken);
        var aboutDone = branchesFilled && cultureDone && valuesDone;

        var colleagueInvited = await _db.UserCompanies.AsNoTracking()
            .Where(uc => treeIds.Contains(uc.CompanyId) && uc.UserId != userId)
            .Select(uc => uc.UserId)
            .Distinct()
            .AnyAsync(cancellationToken)
            || await _db.Users.AsNoTracking()
                .AnyAsync(
                    u => u.CompanyId != null
                         && treeIds.Contains(u.CompanyId ?? Guid.Empty)
                         && u.Id != userId,
                    cancellationToken);

        var verified = root.VerificationStatus == CompanyVerificationStatus.Verified;
        var showUnverified = !verified
            && root.VerificationStatus is CompanyVerificationStatus.Unverified
                or CompanyVerificationStatus.Pending
                or CompanyVerificationStatus.Rejected;
        // Success banner window: within 7 days of verification (session dismiss is client-side).
        var showSuccess = verified
            && root.VerifiedAtUtc is DateTime vat
            && vat > DateTime.UtcNow.AddDays(-7);

        var suggestions = verified
            ? await _suggestions.ListOpenAsync(root.Id, cancellationToken)
            : [];

        var checklist = new List<EmployerChecklistItemDto>
        {
            new("account", true, null, null),
            new(
                "verify",
                verified,
                verified ? null : "/register/verifieren",
                letterUnderway ? "letter" : manualPending ? "manual" : null),
            new(
                "about",
                aboutDone,
                aboutDone ? null : "/register/bedrijf",
                DetailAbout(branchesFilled, cultureDone, valuesDone, engagementDone)),
            new(
                "vacancy",
                hasReady || (verified && hasAnyVacancy),
                "/branch/vacancies/new",
                hasReady ? "ready" : null),
            new(
                "invite",
                colleagueInvited,
                colleagueInvited ? null : "/employer/users",
                null)
        };

        foreach (var s in suggestions)
        {
            checklist.Add(new EmployerChecklistItemDto(
                "suggested_vestiging",
                false,
                "/employer/branches",
                $"{s.KvkEstablishmentId}|{s.Address}"));
        }

        var visibility = EmployerVisibilityPanel.ForCompany(root)
            .Select(r => new EmployerVisibilityRowDto(r.Key, r.Visible))
            .ToList();

        var checklistComplete = verified
            && checklist.Where(c => c.Key != "suggested_vestiging").All(c => c.Done)
            && suggestions.Count == 0;

        return new EmployerOnboardingStatusDto(
            root.Id,
            root.Name,
            root.VerificationStatus,
            root.VerificationMethod,
            rejectionReason,
            manualPending,
            manualReplyBy,
            letterUnderway,
            options?.ActiveLetterId,
            options?.LetterSentAtUtc ?? letter?.SentAtUtc,
            options?.LetterExpiresAtUtc ?? letter?.ExpiresAtUtc,
            failed,
            attemptsLeft,
            options?.LetterResendAvailableAtUtc,
            options?.LetterResendsRemaining ?? 0,
            options?.LetterAddressMasked,
            options?.EmailAvailable ?? false,
            showUnverified,
            showSuccess,
            root.LastAutoPublishedVacancyCount,
            root.VerifiedAtUtc,
            isBureau,
            lenderPending,
            hasReady,
            checklist,
            visibility,
            suggestions,
            checklistComplete);
    }

    private async Task<Company?> ResolveRootAsync(Company company, CancellationToken cancellationToken)
    {
        if (company.ParentCompanyId is null)
        {
            return company;
        }

        return await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == company.ParentCompanyId.Value, cancellationToken);
    }

    private static string DetailAbout(bool branches, bool culture, bool values, bool engagement)
    {
        var parts = new List<string>(4);
        if (branches) parts.Add("branche");
        if (culture) parts.Add("cultuur");
        if (values) parts.Add("waarden");
        if (engagement) parts.Add("betrokkenheid");
        return string.Join(',', parts);
    }

    private static DateTime AddBusinessDays(DateTime start, int days)
    {
        var d = start;
        var left = days;
        while (left > 0)
        {
            d = d.AddDays(1);
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                left--;
            }
        }

        return d;
    }
}
