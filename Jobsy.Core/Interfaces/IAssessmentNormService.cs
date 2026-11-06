using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IAssessmentNormService
{
    /// <summary>Minimum completed deep attempts before Lobsy comparison cards are shown.</summary>
    public const int MinSampleSize = 100;

    /// <summary>
    /// Returns domain→mean for a kind when the latest snapshot has N ≥ 100; otherwise null/empty.
    /// </summary>
    Task<IReadOnlyDictionary<string, double>?> GetMeansIfReadyAsync(
        AssessmentKind kind,
        CancellationToken ct = default);

    Task<int> GetSampleSizeAsync(AssessmentKind kind, CancellationToken ct = default);

    Task RecomputeAsync(CancellationToken ct = default);
}
