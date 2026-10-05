using System.Security.Claims;
using System.Text.Json;
using Jobsy.Api.Admin;
using Jobsy.Api.Models;
using Jobsy.Api.Privacy;
using Jobsy.Core.Admin;
using Jobsy.Core.Contracts;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly IVacancyProductService _products;
    private readonly IUserLookupService _users;
    private readonly ICompanyApiKeyService _apiKeys;
    private readonly IPersonalDataAccessLogger _accessLog;
    private readonly ISupportAccessService _supportAccess;
    private readonly ISecretProtector _secrets;
    private readonly IDeviceSessionService _deviceSessions;
    private readonly IMfaTrustedDeviceService _trustedDevices;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly IAdminAuditLog _audit;
    private readonly IAdminAuditContext _auditContext;
    private readonly IMetricsQueryService _metrics;
    private readonly ICandidateInsightsQueue _insightsQueue;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        JobsyDbContext db,
        IKvkService kvk,
        IVacancyProductService products,
        IUserLookupService users,
        ICompanyApiKeyService apiKeys,
        IPersonalDataAccessLogger accessLog,
        ISupportAccessService supportAccess,
        ISecretProtector secrets,
        IDeviceSessionService deviceSessions,
        IMfaTrustedDeviceService trustedDevices,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        IAdminAuditLog audit,
        IAdminAuditContext auditContext,
        IMetricsQueryService metrics,
        ICandidateInsightsQueue insightsQueue,
        ILogger<AdminController> logger)
    {
        _db = db;
        _kvk = kvk;
        _products = products;
        _users = users;
        _apiKeys = apiKeys;
        _accessLog = accessLog;
        _supportAccess = supportAccess;
        _secrets = secrets;
        _deviceSessions = deviceSessions;
        _trustedDevices = trustedDevices;
        _mailer = mailer;
        _features = features;
        _audit = audit;
        _auditContext = auditContext;
        _metrics = metrics;
        _insightsQueue = insightsQueue;
        _logger = logger;
    }

    [HttpGet("companies")]
    public async Task<ActionResult> GetCompanies(
        [FromQuery] string? q = null,
        [FromQuery] string? type = null,
        [FromQuery] string? region = null,
        [FromQuery] string? status = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var features = await _features.GetAsync(cancellationToken);
        var result = await AdminCompaniesQuery.QueryAsync(
            _db,
            new AdminCompaniesQuery.QueryArgs(
                Q: q,
                Type: type,
                Region: region,
                Status: status,
                Page: page,
                PageSize: pageSize,
                InactiveCompanyDays: features.InactiveCompanyDays),
            cancellationToken);

        if (result.Paged)
        {
            return Ok(new AdminCompaniesPageDto(result.Items, result.Page, result.PageSize, result.TotalCount));
        }

        // No paging params → today's array shape (extended fields are additive).
        return Ok(result.Items);
    }

    [HttpGet("companies/kvk-issues")]
    public async Task<ActionResult<IEnumerable<AdminKvkIssueDto>>> GetKvkIssues(CancellationToken cancellationToken)
        => Ok(await AdminCompaniesQuery.ListKvkIssuesAsync(_db, cancellationToken));

    [HttpPost("companies/{id:guid}/kvk-retry")]
    [AdminAuditExempt("KvK retry is operational")]
    public async Task<ActionResult<object>> RetryKvkVerification(
        Guid id,
        [FromServices] IKvkVerificationRetryService kvkRetry,
        CancellationToken cancellationToken)
    {
        var updated = await kvkRetry.RetryNowAsync(id, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen openstaande KvK-controle." });
        }

        return Ok(new { ok = true, companyId = id });
    }

    [HttpPost("companies/from-kvk")]
    [AdminAuditExempt("Company create from KvK")]
    public async Task<ActionResult<AdminCompanyDetailDto>> RegisterCompanyFromKvk(
        [FromBody] RegisterAdminCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KvkNumber) || string.IsNullOrWhiteSpace(request.KvkEstablishmentId))
        {
            return BadRequest(new { message = "KVK-nummer en vestigings-id zijn verplicht." });
        }

        var establishments = await _kvk.GetEstablishmentsAsync(request.KvkNumber.Trim(), cancellationToken);
        var match = establishments.FirstOrDefault(e =>
            e.KvkEstablishmentId.Equals(request.KvkEstablishmentId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return NotFound(new { message = "Vestiging niet gevonden in KVK." });
        }

        if (match.IsInUse || await _db.Companies.AnyAsync(
                c => c.KvkEstablishmentId == match.KvkEstablishmentId, cancellationToken))
        {
            return BadRequest(new { message = "Deze vestiging is al geregistreerd." });
        }

        if (request.ParentCompanyId is Guid parentId)
        {
            var parent = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            if (parent is null)
            {
                return NotFound(new { message = "Parent-bedrijf niet gevonden." });
            }

            if (!string.Equals(parent.KvkNumber, match.KvkNumber, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "KVK-nummer moet overeenkomen met het parent-bedrijf." });
            }
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = match.Name,
            KvkNumber = match.KvkNumber,
            KvkEstablishmentId = match.KvkEstablishmentId,
            Address = match.Address,
            Location = new GeoPoint(match.Latitude, match.Longitude),
            Type = request.Type,
            ParentCompanyId = request.ParentCompanyId,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };

        _db.Companies.Add(company);
        await Jobsy.Infrastructure.Services.WmlSalaryTableService.EnsureForCompanyAsync(_db, company.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompanies), new AdminCompanyDetailDto(
            company.Id,
            company.Name,
            company.KvkNumber,
            company.Address,
            company.LogoUrl,
            company.Type.ToString(),
            company.ParentCompanyId,
            0,
            0,
            0,
            0,
            0));
    }

    /// <summary>
    /// Aggregated admin users overview + searchable paginated masked list (AVG default).
    /// Unmasked values require support access (prompt 06).
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<AdminUsersPageDto>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? q = null,
        [FromQuery] string? role = null,
        [FromQuery] string? companyType = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] bool earlyOnly = false,
        [FromQuery] string? mfa = null,
        [FromQuery] string? active = null,
        [FromQuery] string? tab = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var aggregates = await AdminUsersAggregatesQuery.QueryAsync(_db, cancellationToken);

        var baseQuery = _db.Users.AsNoTracking().AsQueryable();

        if (companyId is Guid cid)
        {
            baseQuery = baseQuery.Where(u =>
                u.CompanyId == cid || u.CompanyMemberships.Any(m => m.CompanyId == cid));
        }

        if (earlyOnly)
        {
            baseQuery = baseQuery.Where(u => u.IsEarlyAdapter);
        }

        ApplyTabFilter(ref baseQuery, tab);

        if (!string.IsNullOrWhiteSpace(role)
            && Enum.TryParse<UserRole>(role, ignoreCase: true, out var roleEnum))
        {
            baseQuery = baseQuery.Where(u => u.Role == roleEnum);
        }

        if (string.Equals(companyType, "none", StringComparison.OrdinalIgnoreCase))
        {
            baseQuery = baseQuery.Where(u => u.CompanyId == null);
        }
        else if (!string.IsNullOrWhiteSpace(companyType)
                 && Enum.TryParse<CompanyType>(companyType, ignoreCase: true, out var typeEnum))
        {
            baseQuery = baseQuery.Where(u => u.Company != null && u.Company.Type == typeEnum);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            if (Guid.TryParse(term, out var idTerm))
            {
                baseQuery = baseQuery.Where(u =>
                    u.Id == idTerm
                    || u.Email.Contains(term)
                    || u.FullName.Contains(term)
                    || (u.Company != null && u.Company.Name.Contains(term)));
            }
            else
            {
                baseQuery = baseQuery.Where(u =>
                    u.Email.Contains(term)
                    || u.FullName.Contains(term)
                    || (u.Company != null && u.Company.Name.Contains(term)));
            }
        }

        if (!string.IsNullOrWhiteSpace(mfa))
        {
            baseQuery = mfa.Trim().ToLowerInvariant() switch
            {
                "enrolled" or "aan" => baseQuery.Where(u => u.AuthenticatorEnabled),
                "not-enrolled" or "niet" => baseQuery.Where(u =>
                    !u.AuthenticatorEnabled && !u.ExternalLogins.Any()),
                "external" or "external-only" => baseQuery.Where(u =>
                    !u.AuthenticatorEnabled && u.ExternalLogins.Any()),
                _ => baseQuery
            };
        }

        if (!string.IsNullOrWhiteSpace(active))
        {
            baseQuery = active.Trim().ToLowerInvariant() switch
            {
                "true" or "1" or "actief" => baseQuery.Where(u => u.IsActive),
                "false" or "0" or "geblokkeerd" => baseQuery.Where(u => !u.IsActive),
                _ => baseQuery
            };
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var descending = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (sort ?? "").Trim().ToLowerInvariant() switch
        {
            "role" => descending
                ? baseQuery.OrderByDescending(u => u.Role).ThenBy(u => u.FullName)
                : baseQuery.OrderBy(u => u.Role).ThenBy(u => u.FullName),
            "created" => descending
                ? baseQuery.OrderByDescending(u => u.TermsAcceptedAt).ThenBy(u => u.FullName)
                : baseQuery.OrderBy(u => u.TermsAcceptedAt).ThenBy(u => u.FullName),
            "lastlogin" => descending
                ? baseQuery.OrderByDescending(u => u.LastLoginAtUtc).ThenBy(u => u.FullName)
                : baseQuery.OrderBy(u => u.LastLoginAtUtc).ThenBy(u => u.FullName),
            "name" => descending
                ? baseQuery.OrderByDescending(u => u.FullName)
                : baseQuery.OrderBy(u => u.FullName),
            _ => baseQuery.OrderBy(u => u.Email)
        };
        var pageRows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                Role = u.Role.ToString(),
                u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null,
                CompanyType = u.Company != null ? u.Company.Type.ToString() : null,
                u.IsEarlyAdapter,
                u.IsActive,
                MembershipCompanyIds = u.CompanyMemberships.Select(m => m.CompanyId).ToList(),
                u.AuthenticatorEnabled,
                HasExternalLogin = u.ExternalLogins.Any(),
                u.PhoneNumber,
                u.TermsAcceptedAt,
                u.AuthenticatorEnrolledAtUtc,
                u.LastLoginAtUtc,
                u.IsTestAccount
            })
            .ToListAsync(cancellationToken);

        var pageIds = pageRows.Select(u => u.Id).ToList();
        var now = DateTime.UtcNow;
        var sessionRows = await _db.UserDeviceSessions.AsNoTracking()
            .Where(s => pageIds.Contains(s.UserId) && s.RevokedAtUtc == null)
            .Select(s => new { s.UserId, s.LastUsedAtUtc, s.ExpiresAtUtc })
            .ToListAsync(cancellationToken);
        var sessionByUser = sessionRows
            .GroupBy(s => s.UserId)
            .ToDictionary(
                g => g.Key,
                g => (
                    LastActiveAtUtc: g.Max(s => s.LastUsedAtUtc),
                    ActiveSessionCount: g.Count(s => s.ExpiresAtUtc > now)));

        var trustedRows = await _db.MfaTrustedDevices.AsNoTracking()
            .Where(d => pageIds.Contains(d.UserId) && d.RevokedAtUtc == null && d.ExpiresAtUtc > now)
            .GroupBy(d => d.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var trustedByUser = trustedRows.ToDictionary(x => x.UserId, x => x.Count);

        var membershipNames = await _db.UserCompanies.AsNoTracking()
            .Where(m => pageIds.Contains(m.UserId))
            .Select(m => new { m.UserId, Name = m.Company != null ? m.Company.Name : "—" })
            .ToListAsync(cancellationToken);
        var namesByUser = membershipNames
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).ToList());

        var revealedAny = false;
        var items = new List<AdminUserDetailDto>(pageRows.Count);
        foreach (var u in pageRows)
        {
            var self = u.Id == actor.Id;
            Guid? grantId = null;
            var reveal = self;
            if (!self)
            {
                grantId = await _supportAccess.FindActiveGrantIdAsync(
                    actor.Id, u.Id, null, SupportAccessScope.Contact, cancellationToken);
                reveal = grantId is not null;
            }

            if (reveal && !self)
            {
                revealedAny = true;
                await this.LogPersonalDataAccessAsync(
                    _accessLog,
                    actor.Id,
                    PersonalDataAccessLogExtensions.ResolveActorRole(User),
                    "admin.users.reveal",
                    "reveal",
                    subjectUserId: u.Id,
                    subjectCompanyId: u.CompanyId,
                    reason: "support-access",
                    supportAccessGrantId: grantId,
                    cancellationToken: cancellationToken);
            }

            sessionByUser.TryGetValue(u.Id, out var sess);
            namesByUser.TryGetValue(u.Id, out var names);
            trustedByUser.TryGetValue(u.Id, out var trustedCount);
            DateTime? lastActive = sessionByUser.ContainsKey(u.Id) ? sess.LastActiveAtUtc : null;
            if (u.LastLoginAtUtc is DateTime login && (lastActive is null || login > lastActive))
            {
                lastActive = login;
            }
            var activeSessions = sessionByUser.ContainsKey(u.Id) ? sess.ActiveSessionCount : 0;
            items.Add(new AdminUserDetailDto(
                u.Id,
                reveal ? u.Email : PersonalDataMasker.MaskEmail(u.Email),
                reveal ? u.FullName : PersonalDataMasker.MaskName(u.FullName),
                u.Role,
                u.CompanyId,
                u.CompanyName,
                u.CompanyType,
                u.IsEarlyAdapter,
                u.IsActive,
                u.MembershipCompanyIds,
                ResolveMfaStatus(u.AuthenticatorEnabled, u.HasExternalLogin),
                reveal
                    ? u.PhoneNumber
                    : PersonalDataMasker.MaskPhone(u.PhoneNumber),
                u.TermsAcceptedAt,
                lastActive,
                u.AuthenticatorEnrolledAtUtc,
                activeSessions,
                names,
                trustedCount,
                u.IsTestAccount));
        }

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "admin.users.list",
            "list",
            reason: $"page={page};pageSize={pageSize};q=;role={(role ?? "")};companyId={companyId};earlyOnly={earlyOnly};mfa={(mfa ?? "")};active={(active ?? "")};tab={(tab ?? "")};revealed={revealedAny}",
            cancellationToken: cancellationToken);

        return Ok(new AdminUsersPageDto(
            aggregates,
            items,
            page,
            pageSize,
            total,
            Masked: !revealedAny));
    }

    private static void ApplyTabFilter(ref IQueryable<User> query, string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
        {
            return;
        }

        switch (tab.Trim().ToLowerInvariant())
        {
            case "kandidaten":
            case "candidates":
                query = query.Where(u => u.Role == UserRole.Candidate);
                break;
            case "werkgevers":
            case "employers":
                query = query.Where(u =>
                    u.Role == UserRole.BranchManager
                    || u.Role == UserRole.RegionalManager
                    || u.Role == UserRole.EnterpriseManager
                    || u.Role == UserRole.Intermediary);
                break;
            case "sales":
            case "partners":
                query = query.Where(u =>
                    u.Role == UserRole.SalesManager || u.Role == UserRole.Ambassadeur);
                break;
            case "beheerders":
            case "admins":
                query = query.Where(u => u.Role == UserRole.Admin);
                break;
        }
    }

    [HttpGet("personal-data-access-log")]
    public async Task<ActionResult<PersonalDataAccessLogPageDto>> GetPersonalDataAccessLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] Guid? subjectUserId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var query = _db.PersonalDataAccessLogs.AsNoTracking().AsQueryable();
        if (actorUserId is Guid aid)
        {
            query = query.Where(l => l.ActorUserId == aid);
        }

        if (subjectUserId is Guid sid)
        {
            query = query.Where(l => l.SubjectUserId == sid);
        }

        if (fromUtc is DateTime from)
        {
            query = query.Where(l => l.OccurredAt >= from.ToUniversalTime());
        }

        if (toUtc is DateTime to)
        {
            query = query.Where(l => l.OccurredAt <= to.ToUniversalTime());
        }

        var total = await query.CountAsync(cancellationToken);
        var raw = await query
            .OrderByDescending(l => l.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.OccurredAt,
                l.ActorUserId,
                l.ActorRole,
                l.SubjectUserId,
                l.SubjectCompanyId,
                l.Resource,
                l.Action,
                l.Reason,
                l.SupportAccessGrantId,
                l.CorrelationId
            })
            .ToListAsync(cancellationToken);

        var userIds = raw.Select(l => l.ActorUserId)
            .Concat(raw.Where(l => l.SubjectUserId is Guid).Select(l => l.SubjectUserId!.Value))
            .Distinct()
            .ToList();
        var people = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                CompanyName = u.Company != null ? u.Company.Name : null
            })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var extraCompanyIds = raw
            .Where(l => l.SubjectCompanyId is Guid)
            .Select(l => l.SubjectCompanyId!.Value)
            .Distinct()
            .ToList();
        var companyNames = await _db.Companies.AsNoTracking()
            .Where(c => extraCompanyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        string? MaskedName(Guid id)
            => people.TryGetValue(id, out var person) ? PersonalDataMasker.MaskName(person.FullName) : null;

        string? MaskedEmail(Guid id)
            => people.TryGetValue(id, out var person) ? PersonalDataMasker.MaskEmail(person.Email) : null;

        string? CompanyOf(Guid id)
            => people.TryGetValue(id, out var person) ? person.CompanyName : null;

        string? RoleOf(Guid id)
            => people.TryGetValue(id, out var person) ? person.Role : null;

        var items = raw.Select(l =>
        {
            var subjectRole = l.SubjectUserId is Guid subjectRoleId ? RoleOf(subjectRoleId) : null;
            string? subjectCompany = l.SubjectUserId is Guid sid ? CompanyOf(sid) : null;
            // A candidate has no employer. SubjectCompanyId is the company that was opened, not the person.
            if (string.Equals(subjectRole, "Candidate", StringComparison.OrdinalIgnoreCase))
            {
                subjectCompany = null;
            }
            else if (string.IsNullOrWhiteSpace(subjectCompany)
                && l.SubjectCompanyId is Guid cid
                && companyNames.TryGetValue(cid, out var named))
            {
                subjectCompany = named;
            }

            return new PersonalDataAccessLogItemDto(
                l.Id,
                l.OccurredAt,
                l.ActorUserId,
                l.ActorRole,
                l.SubjectUserId,
                l.SubjectCompanyId,
                l.Resource,
                l.Action,
                l.Reason,
                l.SupportAccessGrantId,
                l.CorrelationId,
                MaskedName(l.ActorUserId),
                MaskedEmail(l.ActorUserId),
                CompanyOf(l.ActorUserId),
                l.SubjectUserId is Guid subjectId ? MaskedName(subjectId) : null,
                l.SubjectUserId is Guid subjectMail ? MaskedEmail(subjectMail) : null,
                subjectRole,
                subjectCompany);
        }).ToList();

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "admin.personal_data_access_log.list",
            "list",
            reason: $"page={page};actor={actorUserId};subject={subjectUserId}",
            cancellationToken: cancellationToken);

        return Ok(new PersonalDataAccessLogPageDto(items, page, pageSize, total));
    }

    [HttpPost("support-access")]
    [AdminAudit(AdminAuditKeys.SupportAccessGrant, TargetType = AdminAuditKeys.TargetTypes.Grant)]
    public async Task<ActionResult<SupportAccessGrantDto>> RequestSupportAccess(
        [FromBody] SupportAccessRequestBody body,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            var grant = await _supportAccess.RequestAsync(
                actor.Id,
                new SupportAccessRequest(
                    body.SubjectUserId,
                    body.SubjectCompanyId,
                    body.Scope,
                    body.Reason ?? "",
                    body.TicketReference,
                    body.DurationMinutes),
                PersonalDataAccessLogExtensions.IsMfaVerifiedInSession(User),
                authMethod: User.FindFirstValue("auth_method"),
                cancellationToken: cancellationToken);

            await this.LogPersonalDataAccessAsync(
                _accessLog,
                actor.Id,
                PersonalDataAccessLogExtensions.ResolveActorRole(User),
                "admin.support_access.grant",
                "reveal",
                subjectUserId: grant.SubjectUserId,
                subjectCompanyId: grant.SubjectCompanyId,
                reason: grant.Reason,
                supportAccessGrantId: grant.Id,
                cancellationToken: cancellationToken);

            return Ok(grant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("support-access/{grantId:guid}/revoke")]
    [AdminAudit(AdminAuditKeys.SupportAccessRevoke, TargetType = AdminAuditKeys.TargetTypes.Grant, TargetRouteKey = "grantId")]
    public async Task<IActionResult> RevokeSupportAccess(Guid grantId, CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            await _supportAccess.RevokeAsync(grantId, actor.Id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Clears another user's authenticator so they re-enroll on the next password login.</summary>
    [HttpPost("users/{userId:guid}/mfa/reset")]
    [AdminAudit(AdminAuditKeys.UserMfaReset, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> ResetUserMfa(
        Guid userId,
        [FromBody] AdminMfaResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var authMethod = User.FindFirstValue("auth_method");
        var mfaOk = PersonalDataAccessLogExtensions.IsMfaVerifiedInSession(User)
                    || PersonalDataAccessLogExtensions.IsExternalAuthMethod(authMethod);
        if (!mfaOk)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bevestig eerst je authenticator." });
        }

        if (userId == actor.Id)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Reset je eigen 2FA via je profiel" });
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new { message = "Geef een reden van 5 tot 500 tekens." });
        }

        _auditContext.Reason = reason;
        _auditContext.TargetId = userId.ToString("D");

        // Local-password admins must re-auth with a fresh TOTP from their own authenticator.
        if (!PersonalDataAccessLogExtensions.IsExternalAuthMethod(authMethod))
        {
            if (string.IsNullOrWhiteSpace(request.ConfirmCode))
            {
                return BadRequest(new { message = "Vul je authenticatorcode in om te bevestigen." });
            }

            var adminSecret = _secrets.Unprotect(actor.AuthenticatorSecret);
            if (!TotpAuthenticator.VerifyCode(adminSecret, request.ConfirmCode.Trim(), DateTime.UtcNow))
            {
                _auditContext.TargetLabel = "user";
                // Filter maps 401 → denied.
                return Unauthorized(new { message = "De authenticatorcode is onjuist." });
            }
        }

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        var maskedLabel = PersonalDataMasker.MaskName(target.FullName);
        _auditContext.TargetLabel = maskedLabel;

        target.AuthenticatorSecret = null;
        target.RecoveryCodesHash = null;
        target.AuthenticatorEnabled = false;
        target.AuthenticatorEnrolledAtUtc = null;
        target.SessionVersion++;

        // Same transaction: audit row + MFA clear. Failure → 500, no reset.
        try
        {
            _audit.Stage(new AdminAuditEntry(
                Action: AdminAuditKeys.UserMfaReset,
                TargetType: AdminAuditKeys.TargetTypes.User,
                TargetId: userId.ToString("D"),
                TargetLabel: maskedLabel,
                Reason: reason,
                Result: AdminAuditKeys.Results.Success,
                ActorUserId: actor.Id,
                ActorRole: PersonalDataAccessLogExtensions.ResolveActorRole(User),
                CorrelationId: HttpContext.TraceIdentifier,
                IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()));
            await _db.SaveChangesAsync(cancellationToken);
            _auditContext.SuppressAutoWrite = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin audit write failed during MFA reset for {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Auditlog kon niet worden geschreven; 2FA is niet gereset." });
        }

        await _deviceSessions.RevokeAllAsync(userId, "mfa-reset", bumpSessionVersion: false, cancellationToken);
        await _trustedDevices.RevokeAllForUserAsync(userId, cancellationToken);

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.mfa",
            "reset",
            subjectUserId: userId,
            reason: reason,
            cancellationToken: cancellationToken);

        try
        {
            var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
            var mail = TransactionalEmails.MfaResetByAdmin(baseUrl, target.FullName);
            await _mailer.SendAsync(mail, target.Email, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MFA reset mail failed for user {UserId}", userId);
        }

        return NoContent();
    }

    [HttpGet("users/{userId:guid}/sessions")]
    public async Task<ActionResult<IReadOnlyList<AdminUserSessionDto>>> ListUserSessions(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        if (!await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            return NotFound();
        }

        Guid? currentId = null;
        if (userId == actor.Id
            && Guid.TryParse(User.FindFirstValue(JobsyClaimTypes.DeviceSessionId), out var parsed))
        {
            currentId = parsed;
        }

        var rows = await _deviceSessions.ListAsync(userId, cancellationToken);
        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.sessions",
            "list",
            subjectUserId: userId,
            cancellationToken: cancellationToken);

        return Ok(rows.Select(r => new AdminUserSessionDto(
            r.Id,
            r.DeviceName,
            r.LastUsedAtUtc,
            r.CreatedAtUtc,
            r.ExpiresAtUtc,
            currentId == r.Id)).ToList());
    }

    [HttpPost("users/{userId:guid}/test-unlock/reset")]
    [AdminAudit(AdminAuditKeys.UserTestUnlockReset, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> ResetTestUnlocks(
        Guid userId,
        [FromBody] AdminTestUnlockResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        // Name the person before the reason check, so a too-short reason still shows who was targeted.
        _auditContext.TargetId = userId.ToString("D");
        _auditContext.TargetLabel = PersonalDataMasker.MaskName(target.FullName);

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new
            {
                code = "reset_reason_length",
                message = "Geef een reden van 5 tot 500 tekens."
            });
        }

        _auditContext.Reason = reason;
        if (!target.IsTestAccount)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "test_account_reset_only",
                message = "Alleen een testaccount kan zo worden gereset."
            });
        }

        var analyses = await _db.CandidateDeepAnalyses
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var row in analyses)
        {
            row.Status = CandidateDeepAnalysisStatuses.Locked;
            row.AnswersJson = "{}";
            row.TagsJson = "[]";
            row.ReportJson = "";
            row.ReportVersion = 0;
            row.UnlockedAtUtc = null;
            row.CompletedAtUtc = null;
            row.ReportGeneratedAtUtc = null;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        var checkouts = await _db.DeepAnalysisCheckouts
            .Where(c => c.UserId == userId
                && (c.Status == DeepAnalysisCheckoutStatus.Paid || c.Status == DeepAnalysisCheckoutStatus.Pending))
            .ToListAsync(cancellationToken);
        foreach (var checkout in checkouts)
        {
            checkout.Status = DeepAnalysisCheckoutStatus.Cancelled;
        }

        var career = await _db.CandidateCareerInterests
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (career is not null)
        {
            CareerInterestDeepReset.ClearDeepCompass(career, DateTime.UtcNow);
        }

        var plans = await _db.CandidateCareerPlans
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
        if (plans.Count > 0)
        {
            var planIds = plans.Select(p => p.Id).ToList();
            var steps = await _db.CandidateCareerStepProgress
                .Where(s => planIds.Contains(s.PlanId))
                .ToListAsync(cancellationToken);
            _db.CandidateCareerStepProgress.RemoveRange(steps);
            _db.CandidateCareerPlans.RemoveRange(plans);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _insightsQueue.TryEnqueue(userId);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/whoami/regenerate")]
    [AdminAudit(AdminAuditKeys.UserWhoAmIRegenerate, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> RegenerateWhoAmIStory(
        Guid userId,
        [FromBody] AdminTestUnlockResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var target = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        _auditContext.TargetId = userId.ToString("D");
        _auditContext.TargetLabel = PersonalDataMasker.MaskName(target.FullName);

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new
            {
                code = "reset_reason_length",
                message = "Geef een reden van 5 tot 500 tekens."
            });
        }

        _auditContext.Reason = reason;
        if (!target.IsTestAccount)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "test_account_story_only",
                message = "Alleen een testaccount kan het verhaal opnieuw laten maken."
            });
        }

        WhoAmIForceRegeneration.Request(userId);
        _insightsQueue.TryEnqueue(userId);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/sessions/{sessionId:guid}/revoke")]
    [AdminAudit(AdminAuditKeys.UserSessionsRevoke, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> RevokeUserSession(
        Guid userId,
        Guid sessionId,
        [FromBody] AdminReasonRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new { message = "Geef een reden van 5 tot 500 tekens." });
        }

        _auditContext.Reason = reason;
        _auditContext.TargetLabel = "user";
        _auditContext.SetDetailsObject(new { count = 1 });

        if (userId == actor.Id
            && Guid.TryParse(User.FindFirstValue(JobsyClaimTypes.DeviceSessionId), out var current)
            && current == sessionId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Je kunt je huidige sessie niet via beheer beëindigen." });
        }

        var ok = await _deviceSessions.RevokeAsync(
            userId, sessionId, "admin:" + reason, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.sessions",
            "revoke",
            subjectUserId: userId,
            reason: reason,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/sessions/revoke-all")]
    [AdminAudit(AdminAuditKeys.UserSessionsRevokeAll, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> RevokeAllUserSessions(
        Guid userId,
        [FromBody] AdminReasonRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new { message = "Geef een reden van 5 tot 500 tekens." });
        }

        _auditContext.Reason = reason;
        _auditContext.TargetLabel = "user";

        if (!await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            return NotFound();
        }

        // Refuse wiping your own current session via revoke-all: revoke each except current.
        if (userId == actor.Id
            && Guid.TryParse(User.FindFirstValue(JobsyClaimTypes.DeviceSessionId), out var current))
        {
            var sessions = await _deviceSessions.ListAsync(userId, cancellationToken);
            foreach (var s in sessions.Where(s => s.Id != current))
            {
                await _deviceSessions.RevokeAsync(userId, s.Id, "admin:" + reason, cancellationToken);
            }
        }
        else
        {
            await _deviceSessions.RevokeAllAsync(
                userId, "admin:" + reason, bumpSessionVersion: true, cancellationToken);
        }

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.sessions",
            "revoke",
            subjectUserId: userId,
            reason: reason,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/block")]
    [AdminAudit(AdminAuditKeys.UserBlock, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> BlockUser(
        Guid userId,
        [FromBody] AdminReasonRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new { message = "Geef een reden van 5 tot 500 tekens." });
        }

        _auditContext.Reason = reason;
        _auditContext.TargetLabel = "user";

        if (userId == actor.Id)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Je kunt jezelf niet blokkeren." });
        }

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        if (target.Role == UserRole.Admin && target.IsActive)
        {
            var otherActiveAdmins = await _db.Users.CountAsync(
                u => u.Role == UserRole.Admin && u.IsActive && u.Id != userId,
                cancellationToken);
            if (otherActiveAdmins == 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Je kunt de laatste actieve beheerder niet blokkeren." });
            }
        }

        target.IsActive = false;
        target.SessionVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        await _deviceSessions.RevokeAllAsync(
            userId, "admin:block:" + reason, bumpSessionVersion: false, cancellationToken);

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.status",
            "block",
            subjectUserId: userId,
            reason: reason,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/unblock")]
    [AdminAudit(AdminAuditKeys.UserUnblock, TargetType = AdminAuditKeys.TargetTypes.User, TargetRouteKey = "userId")]
    public async Task<IActionResult> UnblockUser(
        Guid userId,
        [FromBody] AdminReasonRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new { message = "Geef een reden van 5 tot 500 tekens." });
        }

        _auditContext.Reason = reason;
        _auditContext.TargetLabel = "user";

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        target.IsActive = true;
        await _db.SaveChangesAsync(cancellationToken);

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.status",
            "unblock",
            subjectUserId: userId,
            reason: reason,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("users/bulk/{bulkAction}")]
    [AdminAuditExempt("Bulk wraps block/unblock which each write their own audit")]
    public async Task<ActionResult<AdminBulkUsersResponseDto>> BulkUsers(
        string bulkAction,
        [FromBody] AdminBulkUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 5 || reason.Length > 500)
        {
            return BadRequest(new { message = "Geef een reden van 5 tot 500 tekens." });
        }

        var ids = (request.UserIds ?? []).Distinct().Take(100).ToList();
        if (ids.Count == 0)
        {
            return BadRequest(new { message = "Selecteer minstens één gebruiker." });
        }

        if (ids.Count > 100 || (request.UserIds?.Count ?? 0) > 100)
        {
            return BadRequest(new { message = "Maximaal 100 gebruikers per bulkactie." });
        }

        var results = new List<AdminBulkUserResultDto>();
        var succeeded = 0;
        var skipped = 0;
        var normalized = bulkAction.Trim().ToLowerInvariant();

        foreach (var id in ids)
        {
            if (id == actor.Id)
            {
                results.Add(new AdminBulkUserResultDto(id, false, "jezelf"));
                skipped++;
                continue;
            }

            if (normalized is "block" or "blokkeren")
            {
                var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
                if (target is null)
                {
                    results.Add(new AdminBulkUserResultDto(id, false, "niet gevonden"));
                    skipped++;
                    continue;
                }

                if (target.Role == UserRole.Admin && target.IsActive)
                {
                    var otherActiveAdmins = await _db.Users.CountAsync(
                        u => u.Role == UserRole.Admin && u.IsActive && u.Id != id,
                        cancellationToken);
                    if (otherActiveAdmins == 0)
                    {
                        results.Add(new AdminBulkUserResultDto(id, false, "laatste beheerder"));
                        skipped++;
                        continue;
                    }
                }

                target.IsActive = false;
                target.SessionVersion++;
                await _db.SaveChangesAsync(cancellationToken);
                await _deviceSessions.RevokeAllAsync(
                    id, "admin:bulk-block:" + reason, bumpSessionVersion: false, cancellationToken);
                await this.LogPersonalDataAccessAsync(
                    _accessLog, actor.Id, PersonalDataAccessLogExtensions.ResolveActorRole(User),
                    "user.status", "block", subjectUserId: id, reason: reason,
                    cancellationToken: cancellationToken);
                results.Add(new AdminBulkUserResultDto(id, true));
                succeeded++;
            }
            else if (normalized is "revoke-sessions" or "sessies" or "sessions")
            {
                if (!await _db.Users.AnyAsync(u => u.Id == id, cancellationToken))
                {
                    results.Add(new AdminBulkUserResultDto(id, false, "niet gevonden"));
                    skipped++;
                    continue;
                }

                await _deviceSessions.RevokeAllAsync(
                    id, "admin:bulk-sessions:" + reason, bumpSessionVersion: true, cancellationToken);
                await this.LogPersonalDataAccessAsync(
                    _accessLog, actor.Id, PersonalDataAccessLogExtensions.ResolveActorRole(User),
                    "user.sessions", "revoke", subjectUserId: id, reason: reason,
                    cancellationToken: cancellationToken);
                results.Add(new AdminBulkUserResultDto(id, true));
                succeeded++;
            }
            else
            {
                return BadRequest(new { message = "Onbekende bulkactie." });
            }
        }

        return Ok(new AdminBulkUsersResponseDto(succeeded, skipped, results));
    }

    private static string? ClipDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var text = description.Trim();
        return text.Length <= 400 ? text : text[..400];
    }

    private static string ResolveMfaStatus(bool authenticatorEnabled, bool hasExternalLogin)
    {
        if (authenticatorEnabled)
        {
            return "enrolled";
        }

        return hasExternalLogin ? "external-only" : "not-enrolled";
    }

    [HttpGet("support-access")]
    public async Task<ActionResult<IReadOnlyList<SupportAccessGrantDto>>> ListSupportAccess(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var rows = await _supportAccess.ListRecentAsync(take, activeOnly, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("vacancies/{id:guid}/metrics/{key}")]
    public async Task<ActionResult<IEnumerable<MetricDrilldownItemDto>>> GetVacancyMetricDrilldown(
        Guid id,
        string key,
        [FromQuery] string period = "week",
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Vacancies.AsNoTracking().AnyAsync(v => v.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var items = await _metrics.GetVacancyDrilldownAsync(key, id, period, cancellationToken);
        return Ok(items);
    }

    [HttpGet("vacancies")]
    public async Task<ActionResult<IEnumerable<AdminVacancyDetailDto>>> GetVacancies(
        [FromQuery] string? moderation = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] string? channel = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] bool extended = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Vacancies.AsNoTracking().AsQueryable();
        if (string.Equals(moderation, "flagged", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(v => !v.ContentModerationPassed);
        }

        var vacancies = await query
            .OrderByDescending(v => v.StartDate)
            .Select(v => new
            {
                v.Id,
                v.Title,
                Status = v.Status.ToString(),
                v.CompanyId,
                CompanyName = v.Company.Name,
                CompanyType = v.Company.Type.ToString(),
                v.IsHighlighted,
                v.ExtensionCount,
                v.StartDate,
                v.EndDate,
                CreatedVia = v.CreatedVia.ToString(),
                v.ContentModerationPassed,
                v.ClosedAtUtc,
                v.Description,
                Kind = v.Kind.ToString()
            })
            .ToListAsync(cancellationToken);

        var ids = vacancies.Select(v => v.Id).ToList();
        var impressions = await _db.VacancySearchImpressions.AsNoTracking()
            .Where(i => ids.Contains(i.VacancyId))
            .GroupBy(i => i.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var clicks = await _db.VacancyClicks.AsNoTracking()
            .Where(c => ids.Contains(c.VacancyId))
            .GroupBy(c => c.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var shares = await _db.VacancyShares.AsNoTracking()
            .Where(s => ids.Contains(s.VacancyId))
            .GroupBy(s => s.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var applications = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.VacancyId))
            .GroupBy(a => a.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var likes = await _db.VacancyLikes.AsNoTracking()
            .Where(l => ids.Contains(l.VacancyId))
            .GroupBy(l => l.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var all = vacancies.Select(v => new AdminVacancyDetailDto(
            v.Id,
            v.Title,
            v.Status,
            v.CompanyId,
            v.CompanyName,
            v.CompanyType,
            v.IsHighlighted,
            v.ExtensionCount,
            v.StartDate,
            v.EndDate,
            impressions.GetValueOrDefault(v.Id),
            clicks.GetValueOrDefault(v.Id),
            shares.GetValueOrDefault(v.Id),
            applications.GetValueOrDefault(v.Id),
            likes.GetValueOrDefault(v.Id),
            v.ExtensionCount > 0,
            v.CreatedVia,
            v.ContentModerationPassed,
            v.ClosedAtUtc,
            ClipDescription(v.Description),
            v.Kind)).ToList();

        if (page is null)
        {
            return Ok(all);
        }

        IEnumerable<AdminVacancyDetailDto> filtered = all;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            filtered = filtered.Where(v =>
                v.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || v.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || v.Id.ToString("D").Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(v => v.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(channel, "ats", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(v => string.Equals(v.CreatedVia, "ats", StringComparison.OrdinalIgnoreCase));
        }
        else if (string.Equals(channel, "regular", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(v => !string.Equals(v.CreatedVia, "ats", StringComparison.OrdinalIgnoreCase));
        }

        if (companyId is Guid cid)
        {
            filtered = filtered.Where(v => v.CompanyId == cid);
        }

        if (extended)
        {
            filtered = filtered.Where(v => v.IsExtended);
        }

        var list = filtered.ToList();
        var active = all.Where(v => v.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList();
        pageSize = Math.Clamp(pageSize, 1, 100);
        var pageNumber = Math.Max(1, page.Value);
        Response.Headers["X-Total-Count"] = list.Count.ToString();
        Response.Headers["X-Active-Ats"] = active.Count(v => string.Equals(v.CreatedVia, "ats", StringComparison.OrdinalIgnoreCase)).ToString();
        Response.Headers["X-Active-Regular"] = active.Count(v => !string.Equals(v.CreatedVia, "ats", StringComparison.OrdinalIgnoreCase)).ToString();
        return Ok(list.Skip((pageNumber - 1) * pageSize).Take(pageSize));
    }

    [HttpGet("api-keys")]
    public async Task<ActionResult<IEnumerable<AdminApiKeyView>>> GetApiKeys(CancellationToken cancellationToken)
    {
        var items = await _apiKeys.ListAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("api-keys/{id:guid}/deactivate")]
    [AdminAudit(AdminAuditKeys.ApiKeyDeactivate, TargetType = AdminAuditKeys.TargetTypes.ApiKey, TargetRouteKey = "id")]
    public async Task<IActionResult> DeactivateApiKey(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _apiKeys.DeactivateAsync(id, cancellationToken);
        if (!ok)
        {
            return NotFound(new { message = "API-key niet gevonden." });
        }

        return Ok(new { message = "API-key gedeactiveerd." });
    }

    [HttpPost("vacancies/{id:guid}/extend")]
    [AdminAudit(AdminAuditKeys.VacancyExtend, TargetType = AdminAuditKeys.TargetTypes.Vacancy, TargetRouteKey = "id")]
    public async Task<ActionResult<VacancyProductActionResultDto>> ExtendVacancy(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _products.ExtendAsync(vacancy, actor?.Id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    [HttpPost("vacancies/{id:guid}/inactive")]
    [AdminAudit(AdminAuditKeys.VacancyInactive, TargetType = AdminAuditKeys.TargetTypes.Vacancy, TargetRouteKey = "id")]
    public async Task<ActionResult<VacancyProductActionResultDto>> DeactivateVacancy(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var result = await _products.DeactivateAsync(vacancy, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    private static VacancyProductActionResultDto ToProductResult(VacancyProductOutcome result)
    {
        var v = result.Vacancy;
        return new VacancyProductActionResultDto(
            new VacancyListItemDto(
                v.Id,
                v.Title,
                v.Description,
                v.HourlyWage,
                v.StartDate,
                v.EndDate,
                v.Status.ToString(),
                v.CompanyId,
                v.Company?.Name ?? "",
                v.Company?.Address ?? "",
                v.Company?.LogoUrl,
                v.ImageUrl,
                v.Location.Latitude,
                v.Location.Longitude,
                TransportLabels.Expand(v.RequiredTransport),
                true,
                null,
                null,
                VacancyHighlightRules.IsActive(v.IsHighlighted, v.HighlightedUntil, DateTime.UtcNow),
                v.HighlightedUntil,
                v.ExtensionCount,
                v.VideoUrl,
                v.SalaryTableId,
                null,
                null,
                WorkTypeLabels.ResolveLabels(v.WorkTypes, v.WorkTypeLabels),
                0,
                0,
                0,
                v.RequiredDrivingLicense,
                v.RequiredEducation,
                v.MinimumEmployers,
                v.FulfilledByApplicationId,
                v.CreatedVia.ToString()),
            result.PendingApproval,
            result.ErrorMessage,
            result.PushBomRecipientCount);
    }
}
