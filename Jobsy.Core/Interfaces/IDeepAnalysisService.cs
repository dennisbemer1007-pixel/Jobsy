using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IDeepAnalysisService
{
    Task<DeepAnalysisStateDto> GetStateAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default);

    Task<DeepAnalysisCheckoutResult> StartCheckoutAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a pending stub checkout as paid (Development / AllowStubPayments only) and unlocks.
    /// When <paramref name="expectedUserId"/> is set, the checkout must belong to that user.
    /// </summary>
    Task<bool> TryFulfillPaidCheckoutAsync(
        string paymentId,
        Guid? expectedUserId = null,
        bool allowDevStubMarkPaid = false,
        CancellationToken cancellationToken = default);

    Task UnlockForUserAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default);

    Task<DeepAnalysisStateDto> SaveAsync(
        Guid userId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);
}

public sealed record DeepAnalysisStateDto(
    AssessmentKind Kind,
    string Status,
    bool IsUnlocked,
    bool IsCompleted,
    int AnsweredCount,
    int QuestionCount,
    decimal PriceEuro,
    IReadOnlyList<string> Tags,
    DateTime? UnlockedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ReportGeneratedAtUtc,
    IReadOnlyDictionary<int, int> Answers,
    IReadOnlyList<DeepAnalysisQuestionDto> Questions,
    string UpsellCopy);

public sealed record DeepAnalysisQuestionDto(
    int Id,
    string Family,
    string Domain,
    bool Reverse,
    string PromptNl);

public sealed record DeepAnalysisCheckoutResult(
    Guid CheckoutId,
    string PaymentId,
    string CheckoutUrl,
    decimal AmountEuro,
    bool IsStub,
    AssessmentKind Kind);
