using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class PendingTokenActionService : IPendingTokenActionService
{
    private readonly JobsyDbContext _db;
    private readonly IVacancyProductService _products;
    private readonly ITokenLedgerService _tokens;
    private readonly ILogger<PendingTokenActionService> _logger;

    public PendingTokenActionService(
        JobsyDbContext db,
        IVacancyProductService products,
        ITokenLedgerService tokens,
        ILogger<PendingTokenActionService> logger)
    {
        _db = db;
        _products = products;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<PendingTokenAction> AttachAsync(
        Guid checkoutId,
        Guid spendCompanyId,
        Guid vacancyId,
        PendingTokenActionKind actionKind,
        bool optionHighlight,
        bool optionPushBom,
        bool optionExtend,
        decimal requiredTokens,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.PendingTokenActions
            .FirstOrDefaultAsync(a => a.TokenPurchaseCheckoutId == checkoutId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var row = new PendingTokenAction
        {
            Id = Guid.NewGuid(),
            TokenPurchaseCheckoutId = checkoutId,
            CompanyId = spendCompanyId,
            VacancyId = vacancyId,
            ActionKind = actionKind,
            OptionHighlight = optionHighlight,
            OptionPushBom = optionPushBom,
            OptionExtend = optionExtend,
            RequiredTokens = requiredTokens,
            ActorUserId = actorUserId,
            Status = PendingTokenActionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.PendingTokenActions.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<PendingTokenActionExecutionResult?> TryExecuteForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken = default)
    {
        var action = await _db.PendingTokenActions
            .FirstOrDefaultAsync(a => a.TokenPurchaseCheckoutId == checkoutId, cancellationToken);
        if (action is null)
        {
            return null;
        }

        if (action.Status == PendingTokenActionStatus.Executed)
        {
            return new PendingTokenActionExecutionResult(
                action.Id,
                action.ActionKind,
                action.VacancyId,
                Succeeded: true,
                action.ErrorMessage,
                AlreadyExecuted: true);
        }

        if (action.Status == PendingTokenActionStatus.Cancelled)
        {
            return new PendingTokenActionExecutionResult(
                action.Id,
                action.ActionKind,
                action.VacancyId,
                Succeeded: false,
                action.ErrorMessage ?? "Actie geannuleerd.",
                AlreadyExecuted: true);
        }

        if (action.Status == PendingTokenActionStatus.Executing)
        {
            // Another worker is mid-flight.
            return null;
        }

        // Claim Pending (or retry Failed after a re-fulfill / redirect).
        var claimed = await TryClaimAsync(action, cancellationToken);
        if (claimed == 0)
        {
            await _db.Entry(action).ReloadAsync(cancellationToken);
            if (action.Status == PendingTokenActionStatus.Executed)
            {
                return new PendingTokenActionExecutionResult(
                    action.Id,
                    action.ActionKind,
                    action.VacancyId,
                    Succeeded: true,
                    action.ErrorMessage,
                    AlreadyExecuted: true);
            }

            return null;
        }

        await _db.Entry(action).ReloadAsync(cancellationToken);

        try
        {
            var vacancy = await _db.Vacancies
                .Include(v => v.Company)
                .FirstOrDefaultAsync(v => v.Id == action.VacancyId, cancellationToken);

            if (vacancy is null)
            {
                return await MarkFailedAsync(action, "Vacature niet gevonden.", cancellationToken);
            }

            // EM purchases credit the org pot; branch spends need a quick allocation first.
            await EnsureSpendBalanceAsync(action, cancellationToken);

            VacancyProductOutcome outcome = action.ActionKind switch
            {
                PendingTokenActionKind.Publish => await _products.PublishAsync(
                    vacancy,
                    new VacancyPublishOptions(
                        action.OptionHighlight,
                        action.OptionPushBom,
                        action.OptionExtend),
                    action.ActorUserId,
                    cancellationToken,
                    allowPendingApproval: false),
                PendingTokenActionKind.Highlight => await _products.HighlightAsync(
                    vacancy, action.ActorUserId, cancellationToken),
                PendingTokenActionKind.PushBom => await _products.PushBomAsync(
                    vacancy, action.ActorUserId, cancellationToken),
                PendingTokenActionKind.Extend => await _products.ExtendAsync(
                    vacancy, action.ActorUserId, cancellationToken),
                _ => new VacancyProductOutcome(false, "Onbekende actie.", vacancy)
            };

            if (!outcome.Succeeded)
            {
                return await MarkFailedAsync(
                    action,
                    outcome.ErrorMessage ?? "Actie mislukt na betaling.",
                    cancellationToken);
            }

            action.Status = PendingTokenActionStatus.Executed;
            action.ExecutedAt = DateTime.UtcNow;
            action.ErrorMessage = outcome.ErrorMessage
                ?? (outcome.PendingApproval
                    ? "Publicatie wacht nog op goedkeuring."
                    : null);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Pending token action {ActionId} ({Kind}) executed for vacancy {VacancyId} after checkout {CheckoutId}",
                action.Id, action.ActionKind, action.VacancyId, checkoutId);

            return new PendingTokenActionExecutionResult(
                action.Id,
                action.ActionKind,
                action.VacancyId,
                Succeeded: true,
                action.ErrorMessage,
                AlreadyExecuted: false,
                outcome.PushBomRecipientCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Pending token action {ActionId} failed for checkout {CheckoutId}",
                action.Id, checkoutId);
            return await MarkFailedAsync(action, "Actie mislukt na betaling. Tokens zijn bijgeschreven.", cancellationToken);
        }
    }

    private async Task<int> TryClaimAsync(PendingTokenAction action, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
        {
            return await _db.PendingTokenActions
                .Where(a => a.Id == action.Id
                            && (a.Status == PendingTokenActionStatus.Pending
                                || a.Status == PendingTokenActionStatus.Failed))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Status, PendingTokenActionStatus.Executing),
                    cancellationToken);
        }

        if (action.Status is PendingTokenActionStatus.Pending or PendingTokenActionStatus.Failed)
        {
            action.Status = PendingTokenActionStatus.Executing;
            await _db.SaveChangesAsync(cancellationToken);
            return 1;
        }

        return 0;
    }

    private async Task EnsureSpendBalanceAsync(PendingTokenAction action, CancellationToken cancellationToken)
    {
        var checkout = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == action.TokenPurchaseCheckoutId, cancellationToken);
        if (checkout is null || checkout.CompanyId == action.CompanyId)
        {
            return;
        }

        var branchBalance = await _tokens.GetBalanceAsync(action.CompanyId, cancellationToken);
        var need = Math.Max(0m, action.RequiredTokens - branchBalance);
        if (need <= 0)
        {
            return;
        }

        var potBalance = await _tokens.GetBalanceAsync(checkout.CompanyId, cancellationToken);
        var allocate = Math.Min(potBalance, Math.Max(need, (decimal)checkout.PackSize));
        if (allocate <= 0)
        {
            return;
        }

        await _tokens.AllocateAsync(
            checkout.CompanyId,
            action.CompanyId,
            allocate,
            action.ActorUserId,
            "Auto-allocatie na token-aankoop (pending actie)",
            cancellationToken);
    }

    private async Task<PendingTokenActionExecutionResult> MarkFailedAsync(
        PendingTokenAction action,
        string message,
        CancellationToken cancellationToken)
    {
        action.Status = PendingTokenActionStatus.Failed;
        action.ExecutedAt = DateTime.UtcNow;
        action.ErrorMessage = message;
        await _db.SaveChangesAsync(cancellationToken);
        return new PendingTokenActionExecutionResult(
            action.Id,
            action.ActionKind,
            action.VacancyId,
            Succeeded: false,
            message,
            AlreadyExecuted: false);
    }
}
