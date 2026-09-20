using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICultureFitAiService
{
    Task<CultureFitResult?> TryRefineAsync(
        CultureFitResult local,
        CompetencyScores scores,
        IReadOnlyList<string> pillarLabels,
        CancellationToken cancellationToken = default);
}
