using Jobsy.Core.Enums;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;

namespace Jobsy.Core.Interfaces;

public interface IDeepAnalysisService
{
    Task<DeepAnalysisStateDto> GetStateAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<AssessmentKind, DeepAnalysisStateDto>> GetStatesAsync(
        Guid userId,
        IReadOnlyList<AssessmentKind> kinds,
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
    string UpsellCopy,
    /// <summary>Populated only for a completed <see cref="AssessmentKind.Competence"/> report; null otherwise.</summary>
    CompetenceDeepReport? CompetenceReport = null,
    CareerDeepReport? CareerReport = null,
    CultureDeepReport? CultureReport = null,
    ValuesDeepReport? ValuesReport = null);

public sealed record DeepAnalysisQuestionDto(
    int Id,
    string Family,
    string Domain,
    bool Reverse,
    string PromptNl,
    string ExampleNl = "",
    string DomainLabel = "");

/// <summary>Kept for API JSON shape compatibility with older Web clients.</summary>
public sealed record DeepAnalysisCheckoutResult(
    Guid CheckoutId,
    string PaymentId,
    string CheckoutUrl,
    decimal AmountEuro,
    bool IsStub,
    AssessmentKind Kind);
