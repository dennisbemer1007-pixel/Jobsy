using System.Text.Json;
using Jobsy.Core.Admin;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// DSA notice and action (public-pages 06): store a report, list it for an admin, and apply a
/// reasoned decision. No IP address is stored and the reporter's e-mail never reaches the employer.
/// </summary>
public sealed class ContentReportService : IContentReportService
{
    private readonly JobsyDbContext _db;
    private readonly IPublicCompanyQuery _companies;
    private readonly IVacancyProductService _products;
    private readonly ITransactionalMailer _mailer;
    private readonly IEmailLanguageResolver _languages;
    private readonly IPlatformFeatureService _features;
    private readonly IAdminAuditLog _audit;
    private readonly ILogger<ContentReportService> _logger;

    public ContentReportService(
        JobsyDbContext db,
        IPublicCompanyQuery companies,
        IVacancyProductService products,
        ITransactionalMailer mailer,
        IEmailLanguageResolver languages,
        IPlatformFeatureService features,
        IAdminAuditLog audit,
        ILogger<ContentReportService> logger)
    {
        _db = db;
        _companies = companies;
        _products = products;
        _mailer = mailer;
        _languages = languages;
        _features = features;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ContentReportSubmitResult> SubmitAsync(
        ContentReportSubmission submission,
        CancellationToken cancellationToken = default)
    {
        var targetType = ParseTargetType(submission.TargetType);
        var email = ContentReportRules.NormalizeEmail(submission.ReporterEmail);
        var details = ContentReportRules.NormalizeDetails(submission.Details);
        var resolved = await ResolveTargetAsync(targetType, submission.TargetRef, cancellationToken);

        if (email is not null && resolved.TargetId != Guid.Empty)
        {
            var since = DateTime.UtcNow - ContentReportRules.DuplicateWindow;
            var duplicate = await _db.ContentReports
                .AnyAsync(
                    r => r.TargetType == targetType
                         && r.TargetId == resolved.TargetId
                         && r.ReporterEmail == email
                         && r.CreatedAtUtc >= since,
                    cancellationToken);
            if (duplicate)
            {
                return new ContentReportSubmitResult(EmailConfirmationSent: false, Duplicate: true);
            }
        }

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = resolved.TargetId,
            TargetKvk = resolved.Kvk,
            TargetLabel = resolved.Label,
            Reason = NormalizeReason(submission.Reason),
            Details = details,
            ReporterEmail = email,
            ReporterUserId = submission.ReporterUserId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = resolved.IsPublic ? ContentReportStatus.Open : ContentReportStatus.NoAction,
            DecisionReason = resolved.IsPublic ? null : ContentReportRules.TargetNotPublicReason,
            DecidedAtUtc = resolved.IsPublic ? null : DateTime.UtcNow
        };

        _db.ContentReports.Add(report);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "report.created",
            Message = $"Content report {report.Id} ({targetType})",
            DetailsJson = JsonSerializer.Serialize(new
            {
                reportId = report.Id,
                targetType = targetType.ToString(),
                reason = report.Reason.ToString(),
                reporterEmail = email is null ? null : EmailServiceStub.RedactEmail(email)
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var confirmed = false;
        if (email is not null)
        {
            confirmed = await SendReportReceivedAsync(report, email, submission.Language, cancellationToken);
        }

        return new ContentReportSubmitResult(confirmed, Duplicate: false);
    }

    public async Task<IReadOnlyList<ContentReportListItem>> ListAsync(
        bool? openOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ContentReports.AsNoTracking();
        query = openOnly switch
        {
            true => query.Where(r => r.Status == ContentReportStatus.Open),
            false => query.Where(r => r.Status != ContentReportStatus.Open),
            _ => query
        };

        var rows = await query
            .OrderBy(r => r.Status == ContentReportStatus.Open ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        return await MapAsync(rows, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentReportListItem>> ListForTargetAsync(
        ContentReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.ContentReports.AsNoTracking()
            .Where(r => r.TargetType == targetType && r.TargetId == targetId)
            .OrderBy(r => r.Status == ContentReportStatus.Open ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapAsync(rows, cancellationToken);
    }

    public async Task<ContentReportDecisionResult> DecideAsync(
        ContentReportDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ContentReportRules.IsDecisionAllowed(request.TargetType, request.Decision))
        {
            return new ContentReportDecisionResult(false, "decision_not_allowed", 0);
        }

        var reason = ContentReportRules.NormalizeDecisionReason(request.Reason);
        if (ContentReportRules.RequiresReason(request.Decision) && reason is null)
        {
            return new ContentReportDecisionResult(false, "reason_required", 0);
        }

        var open = await _db.ContentReports
            .Where(r => r.TargetType == request.TargetType
                        && r.TargetId == request.TargetId
                        && r.Status == ContentReportStatus.Open)
            .ToListAsync(cancellationToken);
        if (open.Count == 0)
        {
            return new ContentReportDecisionResult(false, "not_found", 0);
        }

        Guid? ownerCompanyId = null;
        var label = open[0].TargetLabel;
        if (ContentReportRules.NotifiesOwner(request.Decision))
        {
            ownerCompanyId = await ApplyContentActionAsync(request, cancellationToken);
        }
        else if (request.TargetType == ContentReportTargetType.Vacancy)
        {
            ownerCompanyId = await _db.Vacancies.AsNoTracking()
                .Where(v => v.Id == request.TargetId)
                .Select(v => (Guid?)v.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        foreach (var report in open)
        {
            report.Status = request.Decision;
            report.DecisionReason = reason;
            report.DecidedAtUtc = now;
            report.DecidedByUserId = request.ActorUserId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(request, open, cancellationToken);
        await NotifyDecisionAsync(request, open, label, ownerCompanyId, now, cancellationToken);

        return new ContentReportDecisionResult(true, null, open.Count);
    }

    public async Task<string?> DescribeTargetAsync(
        string? targetType,
        string? targetRef,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveTargetAsync(ParseTargetType(targetType), targetRef, cancellationToken);
        return resolved.IsPublic ? resolved.Label : null;
    }

    /// <summary>
    /// Retention: drop the reporter e-mail 30 days after the decision and purge the report after a
    /// year. Open reports stay until an admin decides. Called by the daily cleanup job.
    /// </summary>
    public static async Task<(int Purged, int EmailsCleared)> ApplyRetentionAsync(
        JobsyDbContext db,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var purgeCutoff = utcNow.AddDays(-PrivacyConstants.ContentReportRetentionDays);
        var purged = await db.ContentReports
            .Where(r => r.Status != ContentReportStatus.Open
                        && r.DecidedAtUtc != null
                        && r.DecidedAtUtc < purgeCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var emailCutoff = utcNow.AddDays(-PrivacyConstants.ContentReportEmailRetentionDays);
        var stale = await db.ContentReports
            .Where(r => r.ReporterEmail != null
                        && r.Status != ContentReportStatus.Open
                        && r.DecidedAtUtc != null
                        && r.DecidedAtUtc < emailCutoff)
            .ToListAsync(cancellationToken);
        foreach (var report in stale)
        {
            report.ReporterEmail = null;
            report.ReporterEmailClearedAtUtc = utcNow;
        }

        if (stale.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return (purged, stale.Count);
    }

    private async Task<IReadOnlyList<ContentReportListItem>> MapAsync(
        List<ContentReport> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var targets = rows
            .Select(r => new { r.TargetType, r.TargetId })
            .Distinct()
            .ToList();
        var counts = new Dictionary<(ContentReportTargetType, Guid), int>();
        foreach (var target in targets)
        {
            counts[(target.TargetType, target.TargetId)] = await _db.ContentReports
                .CountAsync(
                    r => r.TargetType == target.TargetType && r.TargetId == target.TargetId,
                    cancellationToken);
        }

        return rows
            .Select(r => new ContentReportListItem(
                r.Id,
                r.TargetType,
                r.TargetId,
                r.TargetKvk,
                r.TargetLabel,
                r.Reason,
                r.Details,
                r.ReporterEmail is null ? null : PersonalDataMasker.MaskEmail(r.ReporterEmail),
                r.CreatedAtUtc,
                r.Status,
                r.DecisionReason,
                r.DecidedAtUtc,
                counts.GetValueOrDefault((r.TargetType, r.TargetId))))
            .ToList();
    }

    /// <summary>Takes the content offline and returns the company that owns it (for the mail).</summary>
    private async Task<Guid?> ApplyContentActionAsync(
        ContentReportDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TargetType == ContentReportTargetType.Vacancy)
        {
            var vacancy = await _db.Vacancies
                .Include(v => v.Company)
                .FirstOrDefaultAsync(v => v.Id == request.TargetId, cancellationToken);
            if (vacancy is null)
            {
                return null;
            }

            if (vacancy.Status == VacancyStatus.Active)
            {
                var outcome = await _products.DeactivateAsync(vacancy, cancellationToken);
                if (!outcome.Succeeded)
                {
                    _logger.LogWarning(
                        "Moderation could not deactivate vacancy {VacancyId}: {Error}",
                        vacancy.Id,
                        outcome.ErrorMessage);
                }
            }

            return vacancy.CompanyId;
        }

        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == request.TargetId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        company.PublicPageBlockedAtUtc ??= DateTime.UtcNow;
        return company.Id;
    }

    private async Task WriteAuditAsync(
        ContentReportDecisionRequest request,
        List<ContentReport> reports,
        CancellationToken cancellationToken)
    {
        try
        {
            await _audit.WriteAsync(
                new AdminAuditEntry(
                    Action: AdminAuditKeys.ReportDecided,
                    TargetType: AdminAuditKeys.TargetTypes.ContentReport,
                    TargetId: request.TargetId.ToString(),
                    TargetLabel: request.TargetType.ToString(),
                    // No free text: the statement of reasons lives on the report, not in the log.
                    DetailsJson: JsonSerializer.Serialize(new
                    {
                        decision = request.Decision.ToString(),
                        reportIds = reports.Select(r => r.Id).ToArray()
                    }),
                    Result: AdminAuditKeys.Results.Success,
                    ActorUserId: request.ActorUserId,
                    ActorRole: request.ActorRole),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to write report.decided audit event.");
        }
    }

    private async Task NotifyDecisionAsync(
        ContentReportDecisionRequest request,
        List<ContentReport> reports,
        string? label,
        Guid? ownerCompanyId,
        DateTime decidedAtUtc,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = features.PublicWebBaseUrl;
        var reason = reports[0].DecisionReason;

        var emails = reports
            .Select(r => r.ReporterEmail)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var email in emails)
        {
            try
            {
                var culture = await _languages.ResolveAsync(
                    new EmailRecipient.Address(email),
                    cancellationToken);
                var mail = TransactionalEmails.ReportDecided(
                    baseUrl,
                    DescribeTarget(culture, request.TargetType, label),
                    EmailStrings.Get(culture, ContentReportRules.DecisionEmailKey(request.Decision)),
                    reason,
                    decidedAtUtc,
                    culture);
                await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to mail the report decision to the notifier.");
            }
        }

        if (!ContentReportRules.NotifiesOwner(request.Decision)
            || ownerCompanyId is not Guid companyId
            || string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        await NotifyOwnerAsync(
            request,
            companyId,
            label,
            reason!,
            decidedAtUtc,
            baseUrl,
            cancellationToken);
    }

    private async Task NotifyOwnerAsync(
        ContentReportDecisionRequest request,
        Guid companyId,
        string? label,
        string reason,
        DateTime decidedAtUtc,
        string? baseUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var company = await _db.Companies.AsNoTracking()
                .Where(c => c.Id == companyId)
                .Select(c => new { c.Name })
                .FirstOrDefaultAsync(cancellationToken);
            var managers = await (
                from uc in _db.UserCompanies.AsNoTracking()
                join u in _db.Users.AsNoTracking() on uc.UserId equals u.Id
                where uc.CompanyId == companyId
                      && u.IsActive
                      && u.Email != null
                      && (u.Role == UserRole.EnterpriseManager
                          || u.Role == UserRole.BranchManager
                          || u.Role == UserRole.RegionalManager)
                select new { u.Id, u.Email }
            ).Distinct().ToListAsync(cancellationToken);

            foreach (var manager in managers)
            {
                var culture = await _languages.ResolveAsync(
                    new EmailRecipient.User(manager.Id),
                    cancellationToken);
                var mail = TransactionalEmails.ContentRemoved(
                    baseUrl,
                    company?.Name ?? string.Empty,
                    DescribeTarget(culture, request.TargetType, label),
                    EmailStrings.Get(culture, ContentReportRules.DecisionEmailKey(request.Decision)),
                    reason,
                    decidedAtUtc,
                    culture);
                await _mailer.SendAsync(mail, manager.Email!, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to mail the moderation decision to the employer.");
        }
    }

    private async Task<bool> SendReportReceivedAsync(
        ContentReport report,
        string email,
        string? language,
        CancellationToken cancellationToken)
    {
        try
        {
            var features = await _features.GetAsync(cancellationToken);
            var culture = language is null
                ? await _languages.ResolveAsync(new EmailRecipient.Address(email), cancellationToken)
                : EmailCulture.ForLanguage(language);
            var mail = TransactionalEmails.ReportReceived(
                features.PublicWebBaseUrl,
                DescribeTarget(culture, report.TargetType, report.TargetLabel),
                EmailStrings.Get(culture, ContentReportRules.ReasonEmailKey(report.Reason)),
                report.CreatedAtUtc,
                culture);
            var outcome = await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);
            return outcome.Sent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send the report confirmation mail.");
            return false;
        }
    }

    private static string DescribeTarget(
        EmailCulture culture,
        ContentReportTargetType targetType,
        string? label)
    {
        var kind = EmailStrings.Get(culture, ContentReportRules.TargetKindEmailKey(targetType));
        return string.IsNullOrWhiteSpace(label) ? kind : $"{kind}: {label}";
    }

    private static ContentReportTargetType ParseTargetType(string? value)
        => string.Equals(value?.Trim(), "company", StringComparison.OrdinalIgnoreCase)
            ? ContentReportTargetType.Company
            : ContentReportTargetType.Vacancy;

    private static ContentReportReason NormalizeReason(ContentReportReason reason)
        => Enum.IsDefined(reason) ? reason : ContentReportReason.Other;

    private async Task<ResolvedTarget> ResolveTargetAsync(
        ContentReportTargetType targetType,
        string? targetRef,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetRef))
        {
            return ResolvedTarget.Unknown;
        }

        if (targetType == ContentReportTargetType.Company)
        {
            var kvk = CompanyPublicPaths.NormalizeKvkNumber(targetRef);
            if (kvk is null)
            {
                return ResolvedTarget.Unknown;
            }

            var rows = await _companies.GetByKvkAsync(kvk, cancellationToken);
            if (rows.Count == 0)
            {
                return ResolvedTarget.Unknown;
            }

            var primary = rows
                .OrderBy(r => r.ParentCompanyId is null ? 0 : 1)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .First();
            return new ResolvedTarget(primary.Id, kvk, primary.Name, IsPublic: true);
        }

        if (!Guid.TryParse(targetRef.Trim(), out var vacancyId))
        {
            return ResolvedTarget.Unknown;
        }

        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null
            || !PublicVisibility.IsVacancyPublic(vacancy, DateOnly.FromDateTime(DateTime.UtcNow)))
        {
            return ResolvedTarget.Unknown;
        }

        return new ResolvedTarget(vacancy.Id, vacancy.Company?.KvkNumber, vacancy.Title, IsPublic: true);
    }

    private sealed record ResolvedTarget(Guid TargetId, string? Kvk, string? Label, bool IsPublic)
    {
        public static ResolvedTarget Unknown { get; } = new(Guid.Empty, null, null, false);
    }
}
