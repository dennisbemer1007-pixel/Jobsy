using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Entities;

public sealed class CandidateTestReflection
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AssessmentKind Kind { get; set; }

    public TestDepthLevel Level { get; set; }

    /// <summary>Two-letter language code (nl, en).</summary>
    public string Language { get; set; } = "nl";

    public string ContentJson { get; set; } = "{}";

    /// <summary>Hash of score inputs; regeneration when scores change.</summary>
    public string SourceHash { get; set; } = "";

    public bool FromOpenAi { get; set; }

    public string? Feedback { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
