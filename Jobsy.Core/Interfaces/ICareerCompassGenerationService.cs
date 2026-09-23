using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICareerCompassGenerationService
{
    /// <summary>
    /// Builds a general-occupation compass from the paid 150-item career test.
    /// Uses OpenAI when configured; otherwise a local labour-market catalog (not live Lobsy vacancies).
    /// </summary>
    Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
        IReadOnlyDictionary<int, int> answers,
        CancellationToken cancellationToken = default);
}
