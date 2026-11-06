using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICandidateDiscoveryOverviewService
{
    Task<CandidateDiscoveryOverviewPanelDto> GetPanelAsync(Guid userId, CancellationToken cancellationToken = default);
}
