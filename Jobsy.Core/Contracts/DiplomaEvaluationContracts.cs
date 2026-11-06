namespace Jobsy.Core.Contracts;

/// <summary>
/// Candidate-entered evaluation fact safe to show on the passport, the Lobsy-CV PDF,
/// and a partner passport view. No document name and no file bytes.
/// </summary>
public sealed record DiplomaEvaluationSharedFact(
    string? DiplomaTitle,
    string IssuingBody,
    string? IssuingBodyOther,
    string EquivalentLevelText,
    string? EquivalentLevelCode,
    DateOnly EvaluationDate,
    string ReferenceNumber);

/// <summary>Owner API shape. <see cref="DocumentFileName"/> is for the candidate only.</summary>
public sealed record DiplomaEvaluationFactDto(
    Guid Id,
    string? DiplomaTitle,
    string IssuingBody,
    string? IssuingBodyOther,
    string EquivalentLevelText,
    string? EquivalentLevelCode,
    DateOnly EvaluationDate,
    string ReferenceNumber,
    bool HasDocument,
    string? DocumentFileName);

public sealed record UpsertDiplomaEvaluationRequest(
    string? DiplomaTitle,
    string? IssuingBody,
    string? IssuingBodyOther,
    string? EquivalentLevelText,
    string? EquivalentLevelCode,
    DateOnly? EvaluationDate,
    string? ReferenceNumber);
