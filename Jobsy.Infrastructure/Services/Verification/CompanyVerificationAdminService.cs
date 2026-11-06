using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.Verification;

public sealed class CompanyVerificationAdminService : ICompanyVerificationAdminService
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyVerificationService _verification;
    private readonly IKvkService _kvk;
    private readonly IStubLetterStore _stubLetters;
    private readonly ITransactionalMailer _mailer;
    private readonly ILogger<CompanyVerificationAdminService> _logger;

    public CompanyVerificationAdminService(
        JobsyDbContext db,
        ICompanyVerificationService verification,
        IKvkService kvk,
        IStubLetterStore stubLetters,
        ITransactionalMailer mailer,
        ILogger<CompanyVerificationAdminService> logger)
    {
        _db = db;
        _verification = verification;
        _kvk = kvk;
        _stubLetters = stubLetters;
        _mailer = mailer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdminVerificationQueueItem>> ListQueueAsync(
        string? tab,
        CancellationToken cancellationToken = default)
    {
        var normalized = (tab ?? "handmatig").Trim().ToLowerInvariant();
        var items = new List<AdminVerificationQueueItem>();

        if (normalized is "handmatig" or "all")
        {
            var manuals = await _db.CompanyManualVerificationRequests
                .AsNoTracking()
                .Include(r => r.Company)
                .Include(r => r.RequestedByUser)
                .Where(r => r.DecidedAtUtc == null)
                .OrderBy(r => r.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            foreach (var m in manuals)
            {
                if (m.Company is null)
                {
                    continue;
                }

                items.Add(await ToItemAsync(m.Company, "handmatig", m, null, cancellationToken));
            }
        }

        if (normalized is "gemarkeerd" or "all")
        {
            var flagged = await _db.Companies
                .AsNoTracking()
                .Where(c => c.ParentCompanyId == null
                            && c.VerificationStatus != CompanyVerificationStatus.Verified
                            && c.VerificationStatus != CompanyVerificationStatus.Rejected)
                .ToListAsync(cancellationToken);

            foreach (var company in flagged)
            {
                var heuristics = await CollectHeuristicsAsync(company, cancellationToken);
                if (heuristics.Count == 0)
                {
                    continue;
                }

                if (items.Any(i => i.CompanyId == company.Id && i.Tab == "handmatig"))
                {
                    continue;
                }

                items.Add(await ToItemAsync(company, "gemarkeerd", null, null, cancellationToken, heuristics));
            }
        }

        if (normalized is "brieven" or "all")
        {
            var letters = await _db.CompanyVerificationLetters
                .AsNoTracking()
                .Include(l => l.Company)
                .Include(l => l.RequestedByUser)
                .Where(l => l.Status == CompanyVerificationLetterStatus.Blocked
                            || l.Status == CompanyVerificationLetterStatus.Undeliverable)
                .OrderByDescending(l => l.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            foreach (var letter in letters)
            {
                if (letter.Company is null)
                {
                    continue;
                }

                items.Add(await ToItemAsync(letter.Company, "brieven", null, letter, cancellationToken));
            }
        }

        return items;
    }

    public async Task<int> CountOpenAsync(CancellationToken cancellationToken = default)
    {
        var manuals = await _db.CompanyManualVerificationRequests
            .CountAsync(r => r.DecidedAtUtc == null, cancellationToken);
        var letters = await _db.CompanyVerificationLetters
            .CountAsync(l => l.Status == CompanyVerificationLetterStatus.Blocked
                             || l.Status == CompanyVerificationLetterStatus.Undeliverable, cancellationToken);
        return manuals + letters;
    }

    public async Task ApproveAsync(
        Guid companyId,
        Guid adminUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.FirstAsync(c => c.Id == companyId, cancellationToken);
        var open = await _db.CompanyManualVerificationRequests
            .Where(r => r.CompanyId == companyId && r.DecidedAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var r in open)
        {
            r.DecidedAtUtc = now;
            r.DecidedByUserId = adminUserId;
            r.DecisionStatus = CompanyVerificationStatus.Verified;
            r.DecisionNote = note;
        }

        _db.CompanyVerificationDecisions.Add(new CompanyVerificationDecision
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            KvkNumber = company.KvkNumber,
            AdminUserId = adminUserId,
            Outcome = CompanyVerificationStatus.Verified,
            Method = CompanyVerificationMethod.Manual,
            Reason = note,
            CreatedAtUtc = now
        });

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "admin.company_verification.approve",
            Message = $"company={companyId};admin={adminUserId};note={(note is null ? "-" : "set")}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _verification.MarkVerifiedAsync(
            companyId, CompanyVerificationMethod.Manual, adminUserId, note, cancellationToken);
    }

    public async Task RejectAsync(
        Guid companyId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var company = await _db.Companies.FirstAsync(c => c.Id == companyId, cancellationToken);
        var open = await _db.CompanyManualVerificationRequests
            .Where(r => r.CompanyId == companyId && r.DecidedAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var r in open)
        {
            r.DecidedAtUtc = now;
            r.DecidedByUserId = adminUserId;
            r.DecisionStatus = CompanyVerificationStatus.Rejected;
            r.DecisionNote = reason.Trim();
        }

        _db.CompanyVerificationDecisions.Add(new CompanyVerificationDecision
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            KvkNumber = company.KvkNumber,
            AdminUserId = adminUserId,
            Outcome = CompanyVerificationStatus.Rejected,
            Method = CompanyVerificationMethod.Manual,
            Reason = reason.Trim(),
            CreatedAtUtc = now
        });

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "admin.company_verification.reject",
            Message = $"company={companyId};admin={adminUserId};reason=set",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _verification.MarkRejectedAsync(
            companyId, CompanyVerificationMethod.Manual, adminUserId, reason.Trim(), cancellationToken);

        var managers = await _db.Users
            .Where(u => u.CompanyId == companyId
                        && u.Role == UserRole.EnterpriseManager
                        && u.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var manager in managers)
        {
            var mail = TransactionalEmails.CompanyVerificationRejected(
                "https://lobsy.nl", manager.FullName, company.Name, reason.Trim());
            await _mailer.SendAsync(mail, manager.Email, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Company {CompanyId} verification rejected by admin {Admin}", companyId, adminUserId);
    }

    public Task<byte[]?> GetStubLetterPdfAsync(Guid letterId, CancellationToken cancellationToken = default)
        => GetStubLetterPdfCoreAsync(letterId, cancellationToken);

    private async Task<byte[]?> GetStubLetterPdfCoreAsync(Guid letterId, CancellationToken cancellationToken)
    {
        var letter = await _db.CompanyVerificationLetters
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == letterId, cancellationToken);
        if (letter is null
            || letter.Provider != LetterProviderKind.Stub
            || string.IsNullOrWhiteSpace(letter.ProviderLetterId))
        {
            return null;
        }

        return _stubLetters.TryGet(letter.ProviderLetterId, out var pdf, out _) ? pdf : null;
    }

    private async Task<AdminVerificationQueueItem> ToItemAsync(
        Company company,
        string tab,
        CompanyManualVerificationRequest? manual,
        CompanyVerificationLetter? letter,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? heuristics = null)
    {
        var requester = manual?.RequestedByUser
            ?? letter?.RequestedByUser
            ?? await _db.Users.AsNoTracking()
                .Where(u => u.CompanyId == company.Id && u.Role == UserRole.EnterpriseManager)
                .OrderBy(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);

        var priorRejections = await _db.CompanyVerificationDecisions
            .CountAsync(d => d.KvkNumber == company.KvkNumber
                             && d.Outcome == CompanyVerificationStatus.Rejected, cancellationToken);

        KvkCompanyProfile? profile = null;
        try
        {
            profile = await _kvk.GetProfileAsync(company.KvkNumber, cancellationToken);
            if (profile.Status != KvkLookupStatus.Ok)
            {
                profile = null;
            }
        }
        catch
        {
            profile = null;
        }

        var stubAvailable = letter is { Provider: LetterProviderKind.Stub, ProviderLetterId: not null }
            && _stubLetters.TryGet(letter.ProviderLetterId, out _, out _);

        return new AdminVerificationQueueItem(
            company.Id,
            company.Name,
            company.KvkNumber,
            tab,
            company.VerificationStatus,
            manual?.Reason ?? letter?.Status.ToString(),
            requester?.FullName ?? "—",
            requester?.Email,
            requester?.Role.ToString(),
            manual?.CreatedAtUtc ?? letter?.CreatedAtUtc ?? company.VerificationUpdatedAtUtc ?? DateTime.UtcNow,
            heuristics ?? await CollectHeuristicsAsync(company, cancellationToken),
            priorRejections,
            profile?.Address ?? company.Address,
            profile?.Websites ?? [],
            profile?.SbiCodes ?? [],
            manual?.Id,
            letter?.Id,
            stubAvailable);
    }

    private async Task<IReadOnlyList<string>> CollectHeuristicsAsync(
        Company company,
        CancellationToken cancellationToken)
    {
        var flags = new List<string>();
        var rejections = await _db.CompanyVerificationDecisions
            .CountAsync(d => d.KvkNumber == company.KvkNumber
                             && d.Outcome == CompanyVerificationStatus.Rejected, cancellationToken);
        if (rejections >= 3)
        {
            flags.Add("kvk_3_rejections");
        }

        if (company.KvkVerificationStatus == KvkVerificationStatus.Failed)
        {
            flags.Add("kvk_failed");
        }

        var dayAgo = DateTime.UtcNow.AddHours(-24);
        var regs = await _db.CompanyRegistrations
            .CountAsync(r => r.KvkNumber == company.KvkNumber && r.CreatedAt >= dayAgo, cancellationToken);
        if (regs >= 3)
        {
            flags.Add("kvk_burst_registrations");
        }

        // Heuristic (10.5 / §A): SBI-78 (intermediair) registration with a free-mail contact.
        if (company.Type == CompanyType.Intermediary
            || await _db.CompanyRegistrations.AnyAsync(
                r => r.KvkNumber == company.KvkNumber && r.IsIntermediarySbi, cancellationToken))
        {
            var contactEmails = await _db.CompanyRegistrations
                .Where(r => r.KvkNumber == company.KvkNumber)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => r.ContactEmail)
                .Take(5)
                .ToListAsync(cancellationToken);
            var managerEmails = await _db.Users
                .Where(u => u.CompanyId == company.Id && u.IsActive)
                .Select(u => u.Email)
                .Take(5)
                .ToListAsync(cancellationToken);
            if (contactEmails.Concat(managerEmails).Any(FreeMailDomains.IsFreeMail))
            {
                flags.Add("intermediary_freemail");
            }
        }

        return flags;
    }
}
