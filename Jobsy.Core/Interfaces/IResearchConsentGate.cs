namespace Jobsy.Core.Interfaces;

public interface IResearchConsentGate
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
}
