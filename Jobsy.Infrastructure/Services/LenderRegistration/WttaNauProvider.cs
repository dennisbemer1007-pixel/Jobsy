using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Services.LenderRegistration;

/// <summary>
/// Slot for the NAU public Wtta register (expected 1 July 2027). Disabled by default — no scraping.
/// </summary>
public sealed class WttaNauProvider : ILenderRegistrationProvider
{
    // TODO(2027-07-01): enable when NAU publishes a documented public Wtta register API.
    public const string ProviderName = LenderRegistrationSources.WttaNau;

    private readonly bool _enabled;

    public WttaNauProvider(IConfiguration configuration)
    {
        _enabled = configuration.GetValue("LenderRegistration:WttaNau:Enabled", false);
    }

    public string Name => ProviderName;
    public bool IsEnabled => _enabled;

    public Task<LenderProviderResult> CheckAsync(string kvkNumber, CancellationToken cancellationToken = default)
    {
        // No scraping. When enabled later, call the documented NAU API only.
        return Task.FromResult(new LenderProviderResult(
            LenderProviderOutcomes.Unknown,
            Note: "Wtta/NAU provider is not yet available (expected 1 July 2027)."));
    }
}
