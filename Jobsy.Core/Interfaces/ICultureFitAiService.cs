using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICultureFitAiService
{
    Task<CultureFitResult?> TryRefineAsync(
        CultureFitResult local,
        CompetencyScores scores,
        IReadOnlyList<string> pillarLabels,
        CulturePersonalityScores? culture = null,
        CancellationToken cancellationToken = default);
}
