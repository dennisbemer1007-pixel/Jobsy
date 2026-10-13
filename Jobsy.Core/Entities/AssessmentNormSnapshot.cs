using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Aggregate-only Lobsy norm for Career/Culture/Values deep completions.
/// No per-candidate data. Shown only when <see cref="N"/> ≥ 100.
/// </summary>
public sealed class AssessmentNormSnapshot
{
    public Guid Id { get; set; }

    public AssessmentKind Kind { get; set; }

    /// <summary>Domain code within the kind (e.g. RIASEC R, Autonomy).</summary>
    public string Domain { get; set; } = "";

    public int N { get; set; }

    public double Mean { get; set; }

    public double P25 { get; set; }

    public double P50 { get; set; }

    public double P75 { get; set; }

    public DateTime ComputedAtUtc { get; set; }
}
