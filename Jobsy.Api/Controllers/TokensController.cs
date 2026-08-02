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
    private const int MaxExactMatchTokens = 500;

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly ITokenLedgerService _tokenLedger;
    private readonly IPaymentService _payments;
    private readonly IUserLookupService _users;
    private readonly ITokenPurchaseFulfillmentService _fulfillment;
    private readonly IPendingTokenActionService _pendingActions;

    public TokensController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        ITokenLedgerService tokenLedger,
        IPaymentService payments,
        IUserLookupService users,
        ITokenPurchaseFulfillmentService fulfillment,
        IPendingTokenActionService pendingActions)
    {
        _db = db;
        _companyAuth = companyAuth;
        _tokenLedger = tokenLedger;
        _payments = payments;
        _users = users;
        _fulfillment = fulfillment;
        _pendingActions = pendingActions;
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
                c.TokenTransactions.Sum(t => t.Amount),
                c.ParentCompanyId,
                c.TokensManagedByEnterprise))
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

    /// <summary>
    /// Quote for in-context top-up: exact-match tokens needed + bulk packs.
    /// </summary>
    [HttpGet("top-up-quote")]
    [Authorize(Roles = JobsyRoles.TokenPurchaseRoles)]
    public async Task<ActionResult<TokenTopUpQuoteDto>> GetTopUpQuote(
        [FromQuery] Guid companyId,
        [FromQuery] decimal requiredTokens,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty || requiredTokens <= 0)
        {
            return BadRequest(new { message = "companyId en requiredTokens zijn verplicht." });
        }

        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var balance = await _tokenLedger.GetBalanceAsync(companyId, cancellationToken);
        var deficit = Math.Max(0m, requiredTokens - balance);
        var exactMatch = deficit <= 0
            ? 0
            : (int)Math.Clamp(Math.Ceiling(deficit), 1, MaxExactMatchTokens);

        var packs = await _db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PackSize)
            .Select(p => new TokenPackDto(p.PackSize, p.PriceEuro))
            .ToListAsync(cancellationToken);

        if (packs.Count == 0)
        {
            packs =
            [
                new TokenPackDto(1, 5.00m),
                new TokenPackDto(5, 22.50m),
                new TokenPackDto(10, 40.00m),
                new TokenPackDto(50, 175.00m),
                new TokenPackDto(100, 300.00m)
            ];
        }

        var exactPrice = exactMatch <= 0
            ? 0m
            : await ResolvePackPriceAsync(exactMatch, cancellationToken);

        return Ok(new TokenTopUpQuoteDto(
            companyId,
            balance,
            requiredTokens,
            deficit,
            exactMatch,
            exactPrice,
            packs));
    }

    [HttpPost("checkout")]
    [Authorize(Roles = JobsyRoles.TokenPurchaseRoles)]
    [RequireCompanyAccess]
    public async Task<ActionResult<CheckoutResultDto>> CreateCheckout(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PackSize <= 0 || request.PackSize > MaxExactMatchTokens)
        {
            return BadRequest(new { message = $"PackSize moet tussen 1 en {MaxExactMatchTokens} liggen." });
        }

        // Checkout UI always sends an explicit choice; empty falls back to company preference / iDEAL+CC list.
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod)
            && !Jobsy.Core.Rules.MolliePaymentMethods.IsSupported(request.PaymentMethod))
        {
            return BadRequest(new
            {
                message = "Ongeldige betaalmethode. Kies iDEAL of creditcard voordat je naar Mollie gaat."
            });
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        if (!Jobsy.Core.Rules.KvkVerificationRules.CanPublishOrSpend(company.KvkVerificationStatus))
        {
            return BadRequest(new
            {
                message = Jobsy.Core.Rules.KvkVerificationRules.BlockedMessage(company.KvkVerificationStatus)
            });
        }

        var purchaseTargetId = request.CompanyId;
        var spendCompanyId = request.CompanyId;
        var isEnterprise = User.IsInRole(JobsyRoles.EnterpriseManager);
        var isBranch = User.IsInRole(JobsyRoles.BranchManager);
        var isAdmin = _companyAuth.IsAdmin(User);
        var isIntermediary = User.IsInRole(JobsyRoles.Intermediary);

        if (isBranch && !isAdmin && !isEnterprise && !isIntermediary)
        {
            if (company.TokensManagedByEnterprise)
            {
                return BadRequest(new
                {
                    message = "Tokenbeheer voor deze vestiging ligt bij de bedrijfsmanager. Je kunt hier geen tokens kopen."
                });
            }
        }
        else if (isEnterprise && !isAdmin)
        {
            // Bedrijfsmanager koopt altijd in de organisatiopot (parent / org-root).
            if (company.ParentCompanyId is Guid parentId)
            {
                purchaseTargetId = parentId;
            }

            var pot = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == purchaseTargetId, cancellationToken);
            if (pot is null)
            {
                return NotFound(new { message = "Organisatiepot niet gevonden." });
            }

            if (pot.ParentCompanyId is not null)
            {
                return BadRequest(new { message = "Tokens worden gekocht in de organisatiopot, niet per vestiging." });
            }

            try
            {
                await _companyAuth.EnsureCanAccessCompanyAsync(User, purchaseTargetId, cancellationToken);
            }
            catch (Core.Exceptions.ForbiddenCompanyAccessException)
            {
                return Forbid();
            }
        }

        PendingTokenActionKind? pendingKind = null;
        if (request.PendingAction is { } pendingReq)
        {
            if (!TryParsePendingAction(pendingReq.Action, out var kind))
            {
                return BadRequest(new { message = "Onbekende pending action." });
            }

            pendingKind = kind;
            var vacancy = await _db.Vacancies.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == pendingReq.VacancyId, cancellationToken);
            if (vacancy is null)
            {
                return NotFound(new { message = "Vacature niet gevonden." });
            }

            try
            {
                await _companyAuth.EnsureCanAccessCompanyAsync(User, vacancy.CompanyId, cancellationToken);
            }
            catch (Core.Exceptions.ForbiddenCompanyAccessException)
            {
                return Forbid();
            }

            spendCompanyId = vacancy.CompanyId;
        }

        var result = await _payments.CreateTokenPurchaseCheckoutAsync(
            purchaseTargetId,
            request.PackSize,
            request.PaymentMethod,
            cancellationToken);

        if (pendingKind is PendingTokenActionKind actionKind && request.PendingAction is { } attach)
        {
            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            var required = attach.RequiredTokens is > 0
                ? attach.RequiredTokens.Value
                : request.PackSize;
            await _pendingActions.AttachAsync(
                result.CheckoutId != Guid.Empty ? result.CheckoutId : await LookupCheckoutIdAsync(result.PaymentId, cancellationToken),
                spendCompanyId,
                attach.VacancyId,
                actionKind,
                attach.Highlight,
                attach.PushBom,
                attach.Extend,
                required,
                actor?.Id,
                cancellationToken);
        }

        return Ok(new CheckoutResultDto(
            result.PaymentId,
            result.CheckoutUrl,
            result.PackSize,
            result.AmountEuro,
            result.IsStub,
            result.CheckoutId,
            result.PaymentMethod));
    }

    /// <summary>
    /// Credits tokens for a persisted checkout session. Client may send PaymentId or CheckoutId —
    /// PackSize and CompanyId come from the server-side session.
    /// Also generates the official invoice and queues the BTW-buffer transfer.
    /// </summary>
    [HttpPost("checkout/complete")]
    [Authorize(Roles = JobsyRoles.TokenPurchaseRoles)]
    public async Task<ActionResult<CompleteCheckoutResultDto>> CompleteCheckout(
        [FromBody] CompleteCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        TokenPurchaseCheckout? session = null;
        if (request.CheckoutId is Guid checkoutId && checkoutId != Guid.Empty)
        {
            session = await _db.TokenPurchaseCheckouts
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.Id == checkoutId, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.PaymentId))
        {
            session = await _db.TokenPurchaseCheckouts
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.PaymentId == request.PaymentId, cancellationToken);
        }
        else
        {
            return BadRequest(new { message = "Ongeldige checkout." });
        }

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

        if (session.Status == TokenPurchaseCheckoutStatus.Cancelled)
        {
            return BadRequest(new { message = "Checkout is geannuleerd." });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _fulfillment.TryFulfillPaidCheckoutAsync(
            session.Id,
            actor?.Id,
            allowDevStubMarkPaid: true,
            cancellationToken);

        if (result is null)
        {
            return BadRequest(new { message = "Betaling is nog niet afgerond." });
        }

        PendingActionResultDto? pendingDto = null;
        if (result.PendingAction is { } pending)
        {
            pendingDto = new PendingActionResultDto(
                pending.VacancyId,
                pending.ActionKind.ToString(),
                pending.Succeeded,
                pending.Message,
                pending.PushBomRecipientCount);
        }

        return Ok(new CompleteCheckoutResultDto(
            result.CompanyId,
            result.CompanyName,
            result.NewBalance,
            result.CheckoutId,
            pendingDto));
    }

    [HttpPost("allocate")]
    [Authorize(Roles = JobsyRoles.TokenAllocateRoles)]
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

        // Pot → vestiging: only when the bedrijfsmanager has opted in for that branch.
        if (!_companyAuth.IsAdmin(User)
            && to.ParentCompanyId == from.Id
            && !to.TokensManagedByEnterprise)
        {
            return BadRequest(new
            {
                message = "Deze vestiging is niet aangevinkt voor tokenbeheer door de bedrijfsmanager."
            });
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
                from = new TokenBalanceDto(from.Id, from.Name, fromEntry.NewBalance, from.ParentCompanyId, from.TokensManagedByEnterprise),
                to = new TokenBalanceDto(to.Id, to.Name, toEntry.NewBalance, to.ParentCompanyId, to.TokensManagedByEnterprise)
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
            // Admin grants via this endpoint are logged as Goodwill (€ 0,00 — no BTW/omzet).
            var entry = await _tokenLedger.GrantGoodwillAsync(
                request.CompanyId,
                request.Amount,
                note,
                actorUserId: actor?.Id,
                cancellationToken: cancellationToken);

            return Ok(new TokenBalanceDto(company.Id, company.Name, entry.NewBalance));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Explicit goodwill / compensatie endpoint (same behaviour as grant; clearer for admin tooling).
    /// </summary>
    [HttpPost("goodwill")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<TokenBalanceDto>> GrantGoodwill(
        [FromBody] GrantTokensRequest request,
        CancellationToken cancellationToken)
        => await Grant(request, cancellationToken);

    private async Task<Guid> LookupCheckoutIdAsync(string paymentId, CancellationToken cancellationToken)
    {
        var id = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .Where(c => c.PaymentId == paymentId)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Checkout-sessie niet gevonden na aanmaken.");
        }

        return id;
    }

    private async Task<decimal> ResolvePackPriceAsync(int packSize, CancellationToken cancellationToken)
    {
        var priced = await _db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive && p.PackSize == packSize)
            .Select(p => (decimal?)p.PriceEuro)
            .FirstOrDefaultAsync(cancellationToken);

        return priced ?? packSize switch
        {
            1 => 5.00m,
            5 => 22.50m,
            10 => 40.00m,
            50 => 175.00m,
            100 => 300.00m,
            _ => packSize * 5.00m
        };
    }

    private static bool TryParsePendingAction(string? action, out PendingTokenActionKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        return Enum.TryParse(action.Trim(), ignoreCase: true, out kind)
               && Enum.IsDefined(kind);
    }
}
