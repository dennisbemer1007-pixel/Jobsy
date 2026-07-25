using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/tokens/logs")]
[Authorize]
public class TokenLogsController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;

    public TokenLogsController(JobsyDbContext db, ICompanyAuthorizationService companyAuth)
    {
        _db = db;
        _companyAuth = companyAuth;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TokenLogItemDto>>> GetLogs(
        [FromQuery] string? companyName = null,
        CancellationToken cancellationToken = default)
    {
        if (!_companyAuth.IsAdmin(User) && !_companyAuth.IsEmployer(User))
        {
            return Forbid();
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.TokenTransactions.AsNoTracking().AsQueryable();
        if (accessible is not null)
        {
            query = query.Where(t => accessible.Contains(t.CompanyId));
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            query = query.Where(t => t.Company.Name.Contains(companyName));
        }

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TokenLogItemDto(
                t.Id,
                t.CompanyId,
                t.Company.Name,
                t.Kind.ToString(),
                t.Reason.ToString(),
                t.Amount,
                t.OldBalance,
                t.NewBalance,
                t.Note,
                t.VacancyId,
                t.BranchCompanyId,
                t.CreatedAt))
            .Take(500)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
