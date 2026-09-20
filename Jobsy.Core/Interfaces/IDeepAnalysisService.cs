using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IDeepAnalysisService
{
    Task<DeepAnalysisStateDto> GetStateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<DeepAnalysisCheckoutResult> StartCheckoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> TryFulfillPaidCheckoutAsync(
        string paymentId,
        CancellationToken cancellationToken = default);

    Task UnlockForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<DeepAnalysisStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);
}

public sealed record DeepAnalysisStateDto(
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
    bool IsStub);
