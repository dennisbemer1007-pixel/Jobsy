using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Day-60 cleanup of unverified registrations (D17).
/// </summary>
public sealed class UnverifiedCompanyCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnverifiedCompanyCleanupHostedService> _logger;
    private readonly TimeProvider _clock;

    public UnverifiedCompanyCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<UnverifiedCompanyCleanupHostedService> logger,
        TimeProvider clock)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(8), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unverified company cleanup job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    internal async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
        var snap = await features.GetAsync(cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        var cutoff = now.AddDays(-60);

        var candidates = await db.CompanyRegistrations
            .Include(r => r.CreatedUser)
            .Include(r => r.CreatedOrganizationCompany)
            .Include(r => r.CreatedBranchCompany)
            .Where(r => r.Status == CompanyRegistrationStatus.Activated
                        && r.ActivatedAt != null
                        && r.ActivatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var registration in candidates)
        {
            var root = registration.CreatedOrganizationCompany
                       ?? registration.CreatedBranchCompany;
            if (root is null)
            {
                continue;
            }

            if (root.ParentCompanyId is Guid parentId)
            {
                root = await db.Companies.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken)
                       ?? root;
            }

            if (root.VerificationStatus == CompanyVerificationStatus.Verified
                || root.VerificationStatus == CompanyVerificationStatus.Pending)
            {
                // Pending = open manual check (06) — skip until decided.
                continue;
            }

            // After a closed manual check, wait 7 more days.
            if (root.ManualVerificationClosedAtUtc is DateTime closed
                && now < closed.AddDays(7))
            {
                continue;
            }

            var treeIds = await db.Companies
                .Where(c => c.Id == root.Id || c.ParentCompanyId == root.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            // Only delete companies this registration created that have no other members.
            var createdIds = new List<Guid>();
            if (registration.CreatedOrganizationCompanyId is Guid orgId
                && treeIds.Contains(orgId))
            {
                createdIds.Add(orgId);
            }

            if (registration.CreatedBranchCompanyId is Guid branchId
                && treeIds.Contains(branchId)
                && !createdIds.Contains(branchId))
            {
                createdIds.Add(branchId);
            }

            // Also include sibling vestigingen claimed under the org at activation.
            foreach (var id in treeIds.Where(id => !createdIds.Contains(id)))
            {
                createdIds.Add(id);
            }

            var skipCompany = false;
            foreach (var companyId in createdIds)
            {
                var memberCount = await db.UserCompanies.CountAsync(
                    m => m.CompanyId == companyId, cancellationToken);
                var primaryUsers = await db.Users.CountAsync(
                    u => u.CompanyId == companyId && u.Id != registration.CreatedUserId,
                    cancellationToken);
                if (memberCount > 1 || primaryUsers > 0)
                {
                    // Colleague present — do not delete this company (or the whole tree).
                    skipCompany = true;
                    break;
                }
            }

            if (skipCompany)
            {
                continue;
            }

            var contactEmail = registration.ContactEmail;
            var contactName = registration.ContactName;
            var companyName = root.Name;
            var kvk = root.KvkNumber;
            var vacancyCount = await db.Vacancies.CountAsync(
                v => createdIds.Contains(v.CompanyId), cancellationToken);

            // Delete drafts / klaar vacancies for these companies.
            var vacancies = await db.Vacancies
                .Where(v => createdIds.Contains(v.CompanyId)
                            && (v.Status == VacancyStatus.Draft
                                || v.Status == VacancyStatus.PendingApproval
                                || v.PublishOnVerification))
                .ToListAsync(cancellationToken);
            db.Vacancies.RemoveRange(vacancies);

            // Token rows / checkouts for the companies.
            var tokens = await db.TokenTransactions
                .Where(t => createdIds.Contains(t.CompanyId))
                .ToListAsync(cancellationToken);
            db.TokenTransactions.RemoveRange(tokens);

            var checkouts = await db.TokenPurchaseCheckouts
                .Where(c => createdIds.Contains(c.CompanyId))
                .ToListAsync(cancellationToken);
            db.TokenPurchaseCheckouts.RemoveRange(checkouts);

            var memberships = await db.UserCompanies
                .Where(m => createdIds.Contains(m.CompanyId))
                .ToListAsync(cancellationToken);
            db.UserCompanies.RemoveRange(memberships);

            var companies = await db.Companies
                .Where(c => createdIds.Contains(c.Id))
                .ToListAsync(cancellationToken);
            db.Companies.RemoveRange(companies);

            // User: only when no other role/membership remains.
            if (registration.CreatedUserId is Guid userId)
            {
                var user = await db.Users
                    .Include(u => u.CompanyMemberships)
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user is not null)
                {
                    var otherMemberships = user.CompanyMemberships
                        .Any(m => !createdIds.Contains(m.CompanyId));
                    var otherCompany = user.CompanyId is Guid home && !createdIds.Contains(home);
                    if (!otherMemberships && !otherCompany
                        && user.Role is UserRole.EnterpriseManager
                            or UserRole.BranchManager
                            or UserRole.RegionalManager
                            or UserRole.Intermediary)
                    {
                        var creds = await db.LocalAuthCredentials
                            .Where(c => c.UserId == user.Id)
                            .ToListAsync(cancellationToken);
                        db.LocalAuthCredentials.RemoveRange(creds);
                        db.Users.Remove(user);
                    }
                }
            }

            db.CompanyRegistrations.Remove(registration);

            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "company.unverified-deleted",
                Message =
                    $"kvk={kvk};companies={createdIds.Count};vacancies={vacancyCount};userDeleted={(registration.CreatedUserId is not null)}",
                CreatedAt = now
            });

            var mail = TransactionalEmails.CompanyUnverifiedDeleted(
                snap.PublicWebBaseUrl, contactName, companyName);
            await email.SendAsync(
                new EmailMessage(contactEmail, mail.Subject, mail.Html, mail.Category),
                cancellationToken);

            deleted++;
        }

        if (deleted > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} unverified registration(s) at day 60.", deleted);
        }

        return deleted;
    }
}
