using System.Net;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/company-users")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
public class CompanyUsersController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IEmailService _email;
    private readonly IUserLookupService _users;
    private readonly IPlatformFeatureService _features;

    public CompanyUsersController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IEmailService email,
        IUserLookupService users,
        IPlatformFeatureService features)
    {
        _db = db;
        _companyAuth = companyAuth;
        _email = email;
        _users = users;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyUserDto>>> List(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .Include(u => u.CompanyMemberships)
            .Where(u => JobsyRoles.IsEmployer(u.Role))
            .AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(u =>
                (u.CompanyId != null && accessible.Contains(u.CompanyId.Value))
                || u.CompanyMemberships.Any(m => accessible.Contains(m.CompanyId)));
        }

        var users = await query.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        return Ok(users.Select(Map));
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
        if (!EmployerInviteRules.CanAssignRole(callerRole, request.Role))
        {
            return BadRequest(new { message = "Je mag deze rol niet toekennen (alleen lagere rollen dan jezelf)." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);

        if (request.PrimaryCompanyId is Guid primary)
        {
            if (accessible is not null && !accessible.Contains(primary) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }
        }

        var membershipIds = (request.MembershipCompanyIds ?? [])
            .Distinct()
            .Where(id => accessible is null || _companyAuth.IsAdmin(User) || accessible.Contains(id))
            .ToList();

        if (request.PrimaryCompanyId is Guid p && !membershipIds.Contains(p))
        {
            membershipIds.Add(p);
        }

        var existing = await _db.Users
            .Include(u => u.CompanyMemberships)
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        User user;
        if (existing is not null)
        {
            if (existing.Role is UserRole.Candidate or UserRole.Admin)
            {
                return BadRequest(new { message = "Dit e-mailadres is al in gebruik met een andere rol." });
            }

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

            user = existing;
            user.FullName = request.FullName.Trim();
            // Same-org only (checked above): update role when caller may assign the target role.
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

        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var loginUrl = features.PublicWebBaseUrl.TrimEnd('/') + "/login";
        var name = WebUtility.HtmlEncode(user.FullName);
        await _email.SendAsync(new EmailMessage(
            user.Email,
            "Uitnodiging voor Jobsy",
            $"""
             <p>Hoi {name},</p>
             <p>Je bent uitgenodigd als <strong>{user.Role}</strong> op Jobsy.</p>
             <p><a href="{loginUrl}">Log in om te beginnen</a></p>
             <p><em>Invite stub — geen echte mail.</em></p>
             """,
            "UserInvite"), cancellationToken);

        var loaded = await _db.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .Include(u => u.CompanyMemberships)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        return Ok(Map(loaded));
    }

    private static CompanyUserDto Map(User u) => new(
        u.Id,
        u.Email,
        u.FullName,
        u.Role.ToString(),
        u.CompanyId,
        u.Company?.Name,
        u.CompanyMemberships.Select(m => m.CompanyId).ToList());
}
