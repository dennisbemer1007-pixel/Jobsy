using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services.LenderRegistration;

/// <summary>Marker provider for admin-recorded decisions (RecordDecisionAsync).</summary>
public sealed class AdminManualLenderProvider : ILenderRegistrationProvider
{
    public string Name => LenderRegistrationSources.AdminManual;
    public bool IsEnabled => true;

    public Task<LenderProviderResult> CheckAsync(string kvkNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(new LenderProviderResult(
            LenderProviderOutcomes.Unknown,
            Note: "Admin records the decision via RecordDecisionAsync."));
}
