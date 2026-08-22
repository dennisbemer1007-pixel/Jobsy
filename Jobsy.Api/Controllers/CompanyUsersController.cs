using System.Net;
using System.Security.Cryptography;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/company-users")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}")]
public class CompanyUsersController : ControllerBase
{
    private static readonly UserRole[] EmployerRoleFilter =
    [
        UserRole.BranchManager,
        UserRole.RegionalManager,
        UserRole.EnterpriseManager,
        UserRole.Intermediary
    ];

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IEmailService _email;
    private readonly IUserLookupService _users;
    private readonly IPlatformFeatureService _features;
    private readonly IPartnerAffiliateService _partnerAffiliates;
    private readonly IWebHostEnvironment _environment;

    public CompanyUsersController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IEmailService email,
        IUserLookupService users,
        IPlatformFeatureService features,
        IPartnerAffiliateService partnerAffiliates,
        IWebHostEnvironment environment)
    {
        _db = db;
        _companyAuth = companyAuth;
        _email = email;
        _users = users;
        _features = features;
        _partnerAffiliates = partnerAffiliates;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyUserDto>>> List(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        // Inline enum filter — JobsyRoles.IsEmployer() is not EF-translatable and caused HTTP 500.
        var query = _db.Users
            .AsNoTracking()
            .Where(u => EmployerRoleFilter.Contains(u.Role))
            .AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(u =>
                (u.CompanyId != null && accessible.Contains(u.CompanyId.Value))
                || u.CompanyMemberships.Any(m => accessible.Contains(m.CompanyId)));
        }

        var caller = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (caller?.Role == UserRole.Intermediary && !_companyAuth.IsAdmin(User))
        {
            query = query.Where(u =>
                u.Role == UserRole.Intermediary && u.CompanyId == caller.CompanyId);
        }

        var users = await query
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                Role = u.Role.ToString(),
                u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null,
                MembershipCompanyIds = u.CompanyMemberships.Select(m => m.CompanyId).ToList(),
                u.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(users.Select(u => new CompanyUserDto(
            u.Id,
            u.Email,
            u.FullName,
            u.Role,
            u.CompanyId,
            u.CompanyName,
            u.MembershipCompanyIds,
            u.IsActive)));
    }

    [HttpPost("invite")]
    public async Task<ActionResult<CompanyUserDto>> Invite(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "E-mail en naam zijn verplicht." });
        }

        var caller = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (caller is null)
        {
            return Unauthorized();
        }

        var callerRole = _companyAuth.IsAdmin(User) ? UserRole.Admin : caller.Role;

        // Intermediair team invite: always peer Intermediary on the same organisatie + gedeelde memberships.
        if (callerRole == UserRole.Intermediary)
        {
            if (caller.CompanyId is null)
            {
                return BadRequest(new { message = "Intermediair-organisatie ontbreekt op je account." });
            }

            var sharedMemberships = await _db.UserCompanies.AsNoTracking()
                .Where(m => m.UserId == caller.Id)
                .Select(m => m.CompanyId)
                .ToListAsync(cancellationToken);
            if (!sharedMemberships.Contains(caller.CompanyId.Value))
            {
                sharedMemberships.Add(caller.CompanyId.Value);
            }

            request = request with
            {
                Role = UserRole.Intermediary,
                PrimaryCompanyId = caller.CompanyId,
                MembershipCompanyIds = sharedMemberships.Distinct().ToArray(),
                RegionId = null
            };
        }

        if (!EmployerInviteRules.CanAssignRole(callerRole, request.Role))
        {
            return BadRequest(new { message = "Je mag deze rol niet toekennen." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);

        // Optional: resolve memberships from a region (regiomanager invite).
        var membershipIds = (request.MembershipCompanyIds ?? []).Distinct().ToList();
        if (request.RegionId is Guid regionId)
        {
            var region = await _db.Regions
                .AsNoTracking()
                .Include(r => r.Companies)
                .FirstOrDefaultAsync(r => r.Id == regionId, cancellationToken);
            if (region is null)
            {
                return NotFound(new { message = "Regio niet gevonden." });
            }

            if (accessible is not null
                && !accessible.Contains(region.OrganizationCompanyId)
                && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            if (request.Role != UserRole.RegionalManager)
            {
                return BadRequest(new { message = "Een regio-uitnodiging is alleen voor regiomanagers." });
            }

            membershipIds = region.Companies.Select(c => c.CompanyId).Distinct().ToList();
            if (!membershipIds.Contains(region.OrganizationCompanyId))
            {
                membershipIds.Add(region.OrganizationCompanyId);
            }

            if (request.PrimaryCompanyId is null)
            {
                request = request with { PrimaryCompanyId = region.OrganizationCompanyId };
            }
        }

        if (request.PrimaryCompanyId is Guid primary)
        {
            if (accessible is not null && !accessible.Contains(primary) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            // Scope rules per role
            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == primary, cancellationToken);
            if (company is null)
            {
                return NotFound(new { message = "Bedrijf niet gevonden." });
            }

            if (request.Role == UserRole.EnterpriseManager && company.ParentCompanyId is not null)
            {
                return BadRequest(new { message = "Bedrijfsmanagers horen bij het bedrijf (organisatie), niet bij een vestiging." });
            }

            if (request.Role == UserRole.BranchManager && company.ParentCompanyId is null
                && await _db.Companies.AnyAsync(c => c.ParentCompanyId == company.Id, cancellationToken))
            {
                // Allow BM on org only when there are no child vestigingen (single-location org).
                // Prefer inviting against a vestiging when children exist.
                return BadRequest(new { message = "Kies een vestiging voor de vestigingsmanager." });
            }
        }
        else if (request.Role is UserRole.BranchManager or UserRole.EnterpriseManager or UserRole.RegionalManager)
        {
            return BadRequest(new { message = "Primaire vestiging/bedrijf is verplicht voor deze rol." });
        }

        membershipIds = membershipIds
            .Where(id => accessible is null || _companyAuth.IsAdmin(User) || accessible.Contains(id))
            .ToList();

        if (request.PrimaryCompanyId is Guid p && !membershipIds.Contains(p))
        {
            membershipIds.Add(p);
        }

        // Enterprise managers get membership on all org vestigingen so ExpandChild stays consistent.
        if (request.Role == UserRole.EnterpriseManager && request.PrimaryCompanyId is Guid orgId)
        {
            var childIds = await _db.Companies
                .AsNoTracking()
                .Where(c => c.ParentCompanyId == orgId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            foreach (var childId in childIds)
            {
                if (!membershipIds.Contains(childId)
                    && (accessible is null || _companyAuth.IsAdmin(User) || accessible.Contains(childId)))
                {
                    membershipIds.Add(childId);
                }
            }
        }

        var existing = await _db.Users
            .Include(u => u.CompanyMemberships)
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        User user;
        var promotedFromCandidate = false;
        if (existing is not null)
        {
            if (existing.Role is UserRole.Admin or UserRole.SalesManager)
            {
                return BadRequest(new { message = "Dit e-mailadres is al in gebruik met een andere rol." });
            }

            if (existing.Role == UserRole.Candidate)
            {
                // Behoud User.Id zodat sollicitaties/profiel blijven bestaan; rol wordt manager.
                promotedFromCandidate = true;
            }
            else if (callerRole == UserRole.Intermediary && existing.Role != UserRole.Intermediary)
            {
                return BadRequest(new
                {
                    message = "Je kunt geen bestaande werkgever overnemen als intermediair. Nodig alleen nieuwe collega's of andere intermediairs uit."
                });
            }
            else
            {
                var existingMemberships = existing.CompanyMemberships.Select(m => m.CompanyId).ToList();
                if (!EmployerInviteRules.IsWithinCallerScope(
                        existing.CompanyId,
                        existingMemberships,
                        accessible,
                        _companyAuth.IsAdmin(User)))
                {
                    return BadRequest(new
                    {
                        message = "Deze gebruiker hoort bij een andere organisatie en kan niet worden overgenomen."
                    });
                }
            }

            user = existing;
            user.FullName = request.FullName.Trim();
            user.Role = request.Role;
            if (request.PrimaryCompanyId is Guid newPrimary)
            {
                user.CompanyId = newPrimary;
            }

            user.IsActive = true;

            foreach (var companyId in membershipIds)
            {
                if (user.CompanyMemberships.All(m => m.CompanyId != companyId))
                {
                    _db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
                }
            }
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = request.FullName.Trim(),
                Role = request.Role,
                CompanyId = request.PrimaryCompanyId,
                IsActive = true
            };
            _db.Users.Add(user);
            foreach (var companyId in membershipIds)
            {
                _db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
            }
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        if (credential is null)
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword)
            });
        }
        else
        {
            credential.Email = email;
            credential.PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword);
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (user.Role is UserRole.EnterpriseManager or UserRole.Intermediary)
        {
            await _partnerAffiliates.EnsureProfileAsync(user.Id, cancellationToken);
        }

        var features = await _features.GetAsync(cancellationToken);
        var loginUrl = EmailLayout.LoginUrl(features.PublicWebBaseUrl);
        var roleLabel = RoleLabel(user.Role);
        var invite = TransactionalEmails.UserInvite(
            features.PublicWebBaseUrl,
            user.FullName,
            roleLabel,
            user.Email,
            temporaryPassword,
            promotedFromCandidate);
        await _email.SendAsync(new EmailMessage(
            user.Email,
            invite.Subject,
            invite.Html,
            invite.Category), cancellationToken);

        var loaded = await _db.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .Include(u => u.CompanyMemberships)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        return Ok(Map(
            loaded,
            temporaryPassword: _environment.IsDevelopment() ? temporaryPassword : null,
            loginUrl: loginUrl));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyUserDto>> Update(
        Guid id,
        [FromBody] UpdateCompanyUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Naam is verplicht." });
        }

        var caller = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (caller is null)
        {
            return Unauthorized();
        }

        var callerRole = _companyAuth.IsAdmin(User) ? UserRole.Admin : caller.Role;
        if (!EmployerInviteRules.CanAssignRole(callerRole, request.Role))
        {
            return BadRequest(new { message = "Je mag deze rol niet toekennen." });
        }

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null || !EmployerRoleFilter.Contains(user.Role))
        {
            return NotFound(new { message = "Gebruiker niet gevonden." });
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var existingMemberships = user.CompanyMemberships.Select(m => m.CompanyId).ToList();
        if (!EmployerInviteRules.IsWithinCallerScope(
                user.CompanyId,
                existingMemberships,
                accessible,
                _companyAuth.IsAdmin(User)))
        {
            return Forbid();
        }

        if (!EmployerInviteRules.CanAssignRole(callerRole, user.Role)
            && caller.Id != user.Id)
        {
            return BadRequest(new { message = "Je mag deze gebruiker niet bewerken." });
        }

        if (caller.Id == user.Id)
        {
            if (request.Role != user.Role)
            {
                return BadRequest(new { message = "Je kunt je eigen rol niet wijzigen." });
            }

            if (!request.IsActive)
            {
                return BadRequest(new { message = "Je kunt jezelf niet deactiveren." });
            }
        }

        if (request.PrimaryCompanyId is Guid primary)
        {
            if (accessible is not null && !accessible.Contains(primary) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == primary, cancellationToken);
            if (company is null)
            {
                return NotFound(new { message = "Bedrijf niet gevonden." });
            }

            if (request.Role == UserRole.EnterpriseManager && company.ParentCompanyId is not null)
            {
                return BadRequest(new { message = "Bedrijfsmanagers horen bij het bedrijf (organisatie), niet bij een vestiging." });
            }

            if (request.Role == UserRole.BranchManager && company.ParentCompanyId is null
                && await _db.Companies.AnyAsync(c => c.ParentCompanyId == company.Id, cancellationToken))
            {
                return BadRequest(new { message = "Kies een vestiging voor de vestigingsmanager." });
            }
        }
        else if (request.Role is UserRole.BranchManager or UserRole.EnterpriseManager or UserRole.RegionalManager)
        {
            return BadRequest(new { message = "Primaire vestiging/bedrijf is verplicht voor deze rol." });
        }

        var membershipIds = (request.MembershipCompanyIds ?? [])
            .Distinct()
            .Where(mid => accessible is null || _companyAuth.IsAdmin(User) || accessible.Contains(mid))
            .ToList();

        if (request.PrimaryCompanyId is Guid p && !membershipIds.Contains(p))
        {
            membershipIds.Add(p);
        }

        if (request.Role == UserRole.EnterpriseManager && request.PrimaryCompanyId is Guid orgId)
        {
            var childIds = await _db.Companies
                .AsNoTracking()
                .Where(c => c.ParentCompanyId == orgId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            foreach (var childId in childIds)
            {
                if (!membershipIds.Contains(childId)
                    && (accessible is null || _companyAuth.IsAdmin(User) || accessible.Contains(childId)))
                {
                    membershipIds.Add(childId);
                }
            }
        }

        user.FullName = request.FullName.Trim();
        user.Role = request.Role;
        user.CompanyId = request.PrimaryCompanyId;
        user.IsActive = request.IsActive;

        var toRemove = user.CompanyMemberships
            .Where(m => !membershipIds.Contains(m.CompanyId))
            .ToList();
        foreach (var membership in toRemove)
        {
            // Only remove memberships the caller can manage.
            if (accessible is null || _companyAuth.IsAdmin(User) || accessible.Contains(membership.CompanyId))
            {
                _db.UserCompanies.Remove(membership);
            }
        }

        foreach (var companyId in membershipIds)
        {
            if (user.CompanyMemberships.All(m => m.CompanyId != companyId))
            {
                _db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (user.Role is UserRole.EnterpriseManager or UserRole.Intermediary)
        {
            await _partnerAffiliates.EnsureProfileAsync(user.Id, cancellationToken);
        }

        var loaded = await _db.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .Include(u => u.CompanyMemberships)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        return Ok(Map(loaded));
    }

    private static string RoleLabel(UserRole role) => role switch
    {
        UserRole.EnterpriseManager => "bedrijfsmanager",
        UserRole.RegionalManager => "regiomanager",
        UserRole.BranchManager => "vestigingsmanager",
        UserRole.Intermediary => "intermediair",
        _ => role.ToString()
    };

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#";
        Span<char> chars = stackalloc char[12];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }

    private static CompanyUserDto Map(User u, string? temporaryPassword = null, string? loginUrl = null) => new(
        u.Id,
        u.Email,
        u.FullName,
        u.Role.ToString(),
        u.CompanyId,
        u.Company?.Name,
        u.CompanyMemberships.Select(m => m.CompanyId).ToList(),
        u.IsActive,
        temporaryPassword,
        loginUrl);
}
