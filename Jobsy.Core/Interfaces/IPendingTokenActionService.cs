using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IPendingTokenActionService
{
    Task<PendingTokenAction> AttachAsync(
        Guid checkoutId,
        Guid spendCompanyId,
        Guid vacancyId,
        PendingTokenActionKind actionKind,
        bool optionHighlight,
        bool optionPushBom,
        bool optionExtend,
        decimal requiredTokens,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotently runs the deferred vacancy action after tokens were credited.
    /// Safe to call from webhook and redirect-complete.
    /// </summary>
    Task<PendingTokenActionExecutionResult?> TryExecuteForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken = default);
}

public sealed record PendingTokenActionExecutionResult(
    Guid PendingActionId,
    PendingTokenActionKind ActionKind,
    Guid VacancyId,
    bool Succeeded,
    string? Message,
    bool AlreadyExecuted,
    int PushBomRecipientCount = 0);
