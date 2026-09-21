using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IWhoAmIGenerationService
{
    Task<WhoAmIGeneratedStory> GenerateAsync(
        CompetencyScores competency,
        RiasecScores career,
        DiscScores disc,
        CancellationToken cancellationToken = default);
}

public sealed record WhoAmIGeneratedStory(
    string Story,
    IReadOnlyList<string> Keywords,
    bool FromOpenAi);
