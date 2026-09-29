using System.Security.Claims;
using Jobsy.Api.Models;
using Jobsy.Api.Privacy;
using Jobsy.Core.Authorization;
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

    public AdminController(
        JobsyDbContext db,
        IKvkService kvk,
        IVacancyProductService products,
        IUserLookupService users,
        ICompanyApiKeyService apiKeys,
        IPersonalDataAccessLogger accessLog,
        ISupportAccessService supportAccess,
        ISecretProtector secrets,
        IDeviceSessionService deviceSessions)
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
    }

    [HttpGet("companies")]
    public async Task<ActionResult<IEnumerable<AdminCompanyDetailDto>>> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                Type = c.Type.ToString(),
                c.ParentCompanyId,
                c.ReferredBySalesManagerUserId,
                SalesManagerName = c.ReferredBySalesManagerUser != null
                    ? c.ReferredBySalesManagerUser.FullName
                    : null
            })
            .ToListAsync(cancellationToken);

        var companyIds = companies.Select(c => c.Id).ToList();
        if (companyIds.Count == 0)
        {
            return Ok(Array.Empty<AdminCompanyDetailDto>());
        }

        // Users counted via primary CompanyId OR membership (same semantics as before).
        var activeUserIds = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var activeUserSet = activeUserIds.ToHashSet();

        var membershipPairs = await _db.UserCompanies.AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId))
            .Select(m => new { m.CompanyId, m.UserId })
            .ToListAsync(cancellationToken);
        membershipPairs = membershipPairs
            .Where(m => activeUserSet.Contains(m.UserId))
            .ToList();

        var primaryPairs = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.CompanyId != null && companyIds.Contains(u.CompanyId.Value))
            .Select(u => new { CompanyId = u.CompanyId!.Value, UserId = u.Id })
            .ToListAsync(cancellationToken);
        var userCounts = primaryPairs.Concat(membershipPairs)
            .GroupBy(x => x.CompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());

        var vacancyStats = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId))
            .GroupBy(v => v.CompanyId)
            .Select(g => new
            {
                CompanyId = g.Key,
                Active = g.Count(v => v.Status == VacancyStatus.Active),
                Total = g.Count()
            })
            .ToDictionaryAsync(x => x.CompanyId, cancellationToken);

        var companyByVacancy = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId))
            .Select(v => new { v.Id, v.CompanyId })
            .ToListAsync(cancellationToken);
        var vacancyCompanyMap = companyByVacancy.ToDictionary(v => v.Id, v => v.CompanyId);

        var applicationRows = await _db.Applications.AsNoTracking()
            .Select(a => a.VacancyId)
            .ToListAsync(cancellationToken);
        var applicationCounts = applicationRows
            .Where(vacancyCompanyMap.ContainsKey)
            .Select(vacancyId => vacancyCompanyMap[vacancyId])
            .GroupBy(companyId => companyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.CompanyId, x => x.Count);

        var tokenBalances = await _db.TokenTransactions.AsNoTracking()
            .Where(t => companyIds.Contains(t.CompanyId))
            .GroupBy(t => t.CompanyId)
            .Select(g => new { CompanyId = g.Key, Balance = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Balance, cancellationToken);

        return Ok(companies.Select(c =>
        {
            vacancyStats.TryGetValue(c.Id, out var vac);
            return new AdminCompanyDetailDto(
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                c.Type,
                c.ParentCompanyId,
                userCounts.GetValueOrDefault(c.Id),
                vac?.Active ?? 0,
                vac?.Total ?? 0,
                applicationCounts.GetValueOrDefault(c.Id),
                tokenBalances.GetValueOrDefault(c.Id),
                c.ReferredBySalesManagerUserId,
                c.SalesManagerName);
        }));
    }

    [HttpPost("companies/from-kvk")]
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
            ParentCompanyId = request.ParentCompanyId
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
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var baseQuery = _db.Users.AsNoTracking().AsQueryable();

        // Aggregates over the full (unfiltered) set for overview cards.
        var allRows = await _db.Users.AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.Role,
                u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null,
                u.IsActive,
                u.TermsAcceptedAt
            })
            .ToListAsync(cancellationToken);

        var byRole = allRows
            .GroupBy(u => u.Role.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
        var activeCount = allRows.Count(u => u.IsActive);
        var inactiveCount = allRows.Count - activeCount;
        var topCompanies = allRows
            .Where(u => u.CompanyId != null)
            .GroupBy(u => new { u.CompanyId, Name = u.CompanyName ?? "—" })
            .Select(g => new AdminUsersCompanyCountDto(g.Key.CompanyId, g.Key.Name, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToList();
        var byWeek = allRows
            .Select(u =>
            {
                var stamp = u.TermsAcceptedAt ?? DateTime.UnixEpoch;
                var weekStart = stamp.Date.AddDays(-(int)stamp.DayOfWeek);
                return weekStart;
            })
            .GroupBy(d => d)
            .OrderByDescending(g => g.Key)
            .Take(12)
            .Select(g => new AdminUsersWeekBucketDto(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .ToList();

        if (companyId is Guid cid)
        {
            baseQuery = baseQuery.Where(u =>
                u.CompanyId == cid || u.CompanyMemberships.Any(m => m.CompanyId == cid));
        }

        if (earlyOnly)
        {
            baseQuery = baseQuery.Where(u => u.IsEarlyAdapter);
        }

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
            baseQuery = baseQuery.Where(u =>
                u.Email.Contains(term)
                || u.FullName.Contains(term)
                || (u.Company != null && u.Company.Name.Contains(term)));
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var pageRows = await baseQuery
            .OrderBy(u => u.Email)
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
                HasExternalLogin = u.ExternalLogins.Any()
            })
            .ToListAsync(cancellationToken);

        var revealedAny = false;
        var items = new List<AdminUserDetailDto>(pageRows.Count);
        foreach (var u in pageRows)
        {
            // Own admin record may stay unmasked; others need an active Contact grant.
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
                ResolveMfaStatus(u.AuthenticatorEnabled, u.HasExternalLogin)));
        }

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "admin.users.list",
            "list",
            reason: $"page={page};pageSize={pageSize};q={(q ?? "")};role={(role ?? "")};companyId={companyId};earlyOnly={earlyOnly};revealed={revealedAny}",
            cancellationToken: cancellationToken);

        return Ok(new AdminUsersPageDto(
            new AdminUsersAggregateDto(byRole, topCompanies, activeCount, inactiveCount, byWeek),
            items,
            page,
            pageSize,
            total,
            Masked: !revealedAny));
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
        var items = await query
            .OrderByDescending(l => l.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new PersonalDataAccessLogItemDto(
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
                l.CorrelationId))
            .ToListAsync(cancellationToken);

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
                return Unauthorized(new { message = "De authenticatorcode is onjuist." });
            }
        }

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        target.AuthenticatorSecret = null;
        target.RecoveryCodesHash = null;
        target.AuthenticatorEnabled = false;
        target.AuthenticatorEnrolledAtUtc = null;
        target.SessionVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        await _deviceSessions.RevokeAllAsync(userId, "mfa-reset", bumpSessionVersion: false, cancellationToken);

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "user.mfa",
            "reset",
            subjectUserId: userId,
            reason: reason,
            cancellationToken: cancellationToken);

        return NoContent();
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

    [HttpGet("vacancies")]
    public async Task<ActionResult<IEnumerable<AdminVacancyDetailDto>>> GetVacancies(CancellationToken cancellationToken)
    {
        var vacancies = await _db.Vacancies
            .AsNoTracking()
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
                CreatedVia = v.CreatedVia.ToString()
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

        return Ok(vacancies.Select(v => new AdminVacancyDetailDto(
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
            v.CreatedVia)));
    }

    [HttpGet("api-keys")]
    public async Task<ActionResult<IEnumerable<AdminApiKeyView>>> GetApiKeys(CancellationToken cancellationToken)
    {
        var items = await _apiKeys.ListAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("api-keys/{id:guid}/deactivate")]
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
