namespace Jobsy.Core.Entities;

/// <summary>
/// Official Dutch evaluation of a diploma obtained abroad. The candidate types the
/// stated equivalent; Lobsy never estimates or maps a level. The document bytes are
/// for the candidate only and are not copied onto applications or partner views.
/// </summary>
public class CandidateDiplomaEvaluation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Optional name of the foreign diploma this evaluation belongs to.</summary>
    public string? DiplomaTitle { get; set; }

    /// <summary><c>nuffic</c>, <c>sbb</c>, or <c>other</c>.</summary>
    public string IssuingBody { get; set; } = string.Empty;

    /// <summary>Required when <see cref="IssuingBody"/> is <c>other</c>.</summary>
    public string? IssuingBodyOther { get; set; }

    /// <summary>Dutch equivalent exactly as written on the evaluation. Never derived.</summary>
    public string EquivalentLevelText { get; set; } = string.Empty;

    /// <summary>Optional pick-list code the candidate chose. Never inferred from the text.</summary>
    public string? EquivalentLevelCode { get; set; }

    public DateOnly EvaluationDate { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;

    public string? DocumentFileName { get; set; }
    public string? DocumentContentType { get; set; }
    public byte[]? DocumentContent { get; set; }
    public int? DocumentSizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
