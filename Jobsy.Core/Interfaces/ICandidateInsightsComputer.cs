namespace Jobsy.Core.Interfaces;

/// <summary>Recomputes WhoAmI story, career compass, and top vacancy matches for one user.</summary>
public interface ICandidateInsightsComputer
{
    Task RecomputeAsync(Guid userId, CancellationToken cancellationToken = default);
}
