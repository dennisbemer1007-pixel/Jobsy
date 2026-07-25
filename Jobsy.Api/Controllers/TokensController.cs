using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/tokens")]
public class TokensController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly ITokenLedgerService _tokenLedger;
    private readonly IPaymentService _payments;
    private readonly IUserLookupService _users;
    private readonly IHostEnvironment _environment;

    public TokensController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        ITokenLedgerService tokenLedger,
        IPaymentService payments,
        IUserLookupService users,
        IHostEnvironment environment)
    {
        _db = db;
        _companyAuth = companyAuth;
        _tokenLedger = tokenLedger;
        _payments = payments;
        _users = users;
        _environment = environment;
    }

    [HttpGet("balance")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TokenBalanceDto>>> GetBalances(CancellationToken cancellationToken)
    {
        if (!_companyAuth.IsAdmin(User) && !_companyAuth.IsEmployer(User))
        {
            return Forbid();
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Companies.AsNoTracking().AsQueryable();
        if (accessible is not null)
        {
            query = query.Where(c => accessible.Contains(c.Id));
        }

        var balances = await query
            .OrderBy(c => c.Name)
            .Select(c => new TokenBalanceDto(
                c.Id,
                c.Name,
                c.TokenTransactions.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);

        return Ok(balances);
    }

    [HttpGet("packs")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    public async Task<ActionResult<IEnumerable<TokenPackDto>>> GetPacks(CancellationToken cancellationToken)
    {
        var packs = await _db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PackSize)
            .Select(p => new TokenPackDto(p.PackSize, p.PriceEuro))
            .ToListAsync(cancellationToken);
        return Ok(packs);
    }

    [HttpGet("costs")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    public async Task<ActionResult<IEnumerable<TokenSpendCostDto>>> GetCosts(CancellationToken cancellationToken)
    {
        var costs = await _db.TokenSpendCosts.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Reason)
            .Select(c => new TokenSpendCostDto(c.Reason.ToString(), c.CostTokens))
            .ToListAsync(cancellationToken);
        return Ok(costs);
    }

    [HttpPost("checkout")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequireCompanyAccess]
    public async Task<ActionResult<CheckoutResultDto>> CreateCheckout(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PackSize <= 0)
        {
            return BadRequest(new { message = "PackSize must be positive." });
        }

        var packExists = await _db.TokenPricings.AsNoTracking()
            .AnyAsync(p => p.IsActive && p.PackSize == request.PackSize, cancellationToken);
        if (!packExists && request.PackSize is not (1 or 5 or 10 or 50 or 100))
        {
            return BadRequest(new { message = "Onbekend tokenpakket." });
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        var result = await _payments.CreateTokenPurchaseCheckoutAsync(
            request.CompanyId,
            request.PackSize,
            cancellationToken);

        return Ok(new CheckoutResultDto(
            result.PaymentId,
            result.CheckoutUrl,
            result.PackSize,
            result.AmountEuro,
            result.IsStub));
    }

    /// <summary>
    /// Credits tokens for a persisted checkout session. Client may only send PaymentId —
    /// PackSize and CompanyId come from the server-side session.
    /// </summary>
    [HttpPost("checkout/complete")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<TokenBalanceDto>> CompleteCheckout(
        [FromBody] CompleteCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId))
        {
            return BadRequest(new { message = "Ongeldige checkout." });
        }

        var session = await _db.TokenPurchaseCheckouts
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.PaymentId == request.PaymentId, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Checkout-sessie niet gevonden." });
        }

        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, session.CompanyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        if (session.Status == TokenPurchaseCheckoutStatus.Credited)
        {
            var bal = await _tokenLedger.GetBalanceAsync(session.CompanyId, cancellationToken);
            return Ok(new TokenBalanceDto(session.CompanyId, session.Company.Name, bal));
        }

        if (session.Status == TokenPurchaseCheckoutStatus.Cancelled)
        {
            return BadRequest(new { message = "Checkout is geannuleerd." });
        }

        // Development stub only: simulate provider webhook by marking Pending → Paid.
        if (_environment.IsDevelopment()
            && session.PaymentId.StartsWith("stub_pay_", StringComparison.Ordinal)
            && session.Status == TokenPurchaseCheckoutStatus.Pending)
        {
            session.Status = TokenPurchaseCheckoutStatus.Paid;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var status = await _payments.GetPaymentStatusAsync(request.PaymentId, cancellationToken);
        if (!status.IsPaid)
        {
            return BadRequest(new { message = "Betaling is nog niet afgerond." });
        }

        // Atomic claim: only one concurrent complete can transition Pending/Paid → Credited.
        var creditedAt = DateTime.UtcNow;
        var claimed = await _db.TokenPurchaseCheckouts
            .Where(c => c.Id == session.Id
                        && (c.Status == TokenPurchaseCheckoutStatus.Pending
                            || c.Status == TokenPurchaseCheckoutStatus.Paid))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(c => c.Status, TokenPurchaseCheckoutStatus.Credited)
                    .SetProperty(c => c.CreditedAt, creditedAt),
                cancellationToken);

        if (claimed == 0)
        {
            var bal = await _tokenLedger.GetBalanceAsync(session.CompanyId, cancellationToken);
            return Ok(new TokenBalanceDto(session.CompanyId, session.Company.Name, bal));
        }

        try
        {
            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            var entry = await _tokenLedger.RecordPurchaseAsync(
                session.CompanyId,
                session.PackSize,
                actor?.Id,
                $"Mollie stub {session.PaymentId}",
                cancellationToken);

            return Ok(new TokenBalanceDto(session.CompanyId, session.Company.Name, entry.NewBalance));
        }
        catch
        {
            // Compensate claim so the user can retry.
            await _db.TokenPurchaseCheckouts
                .Where(c => c.Id == session.Id && c.Status == TokenPurchaseCheckoutStatus.Credited)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(c => c.Status, TokenPurchaseCheckoutStatus.Paid)
                        .SetProperty(c => c.CreditedAt, (DateTime?)null),
                    cancellationToken);
            throw;
        }
    }

    [HttpPost("allocate")]
    [Authorize(Roles = $"{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<object>> Allocate(
        [FromBody] AllocateTokensRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be positive." });
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null
            && (!accessible.Contains(request.FromCompanyId) || !accessible.Contains(request.ToCompanyId))
            && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        var from = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.FromCompanyId, cancellationToken);
        var to = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ToCompanyId, cancellationToken);
        if (from is null || to is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        // Prefer parent→child or same-KVK org edges when hierarchy exists.
        var related = to.ParentCompanyId == from.Id
            || from.ParentCompanyId == to.Id
            || (!string.IsNullOrEmpty(from.KvkNumber)
                && string.Equals(from.KvkNumber, to.KvkNumber, StringComparison.OrdinalIgnoreCase));
        if (!related && !_companyAuth.IsAdmin(User))
        {
            // Still allow if both share a region under caller's org.
            var sharedRegion = await _db.RegionCompanies.AsNoTracking()
                .Where(rc => rc.CompanyId == from.Id || rc.CompanyId == to.Id)
                .GroupBy(rc => rc.RegionId)
                .AnyAsync(g => g.Select(x => x.CompanyId).Distinct().Count() == 2, cancellationToken);
            if (!sharedRegion)
            {
                return BadRequest(new { message = "Allocatie alleen tussen gekoppelde vestigingen (zelfde KVK, parent/child of regio)." });
            }
        }

        try
        {
            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            var (fromEntry, toEntry) = await _tokenLedger.AllocateAsync(
                request.FromCompanyId,
                request.ToCompanyId,
                request.Amount,
                actor?.Id,
                request.Note,
                cancellationToken);

            return Ok(new
            {
                from = new TokenBalanceDto(from.Id, from.Name, fromEntry.NewBalance),
                to = new TokenBalanceDto(to.Id, to.Name, toEntry.NewBalance)
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("grant")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<TokenBalanceDto>> Grant([FromBody] GrantTokensRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be positive." });
        }

        var note = request.Note?.Trim();
        if (string.IsNullOrWhiteSpace(note))
        {
            return BadRequest(new { message = "Reden is verplicht voor de tokenlog." });
        }

        if (note.Length > 512)
        {
            return BadRequest(new { message = "Reden mag maximaal 512 tekens zijn." });
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        try
        {
            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            var entry = await _tokenLedger.GrantAsync(
                request.CompanyId,
                request.Amount,
                actorUserId: actor?.Id,
                note: note,
                cancellationToken: cancellationToken);

            return Ok(new TokenBalanceDto(company.Id, company.Name, entry.NewBalance));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
