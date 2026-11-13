using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Scholen;

/// <summary>
/// Toggle for G78 contrast bank (g78-2). Config default off; Acceptatie sets
/// <c>G78ContrastQuestionsEnabled</c> true. Admin platform flag overrides when present.
/// </summary>
public interface IG78ContrastQuestionsGate
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
}

public sealed class G78ContrastQuestionsGate : IG78ContrastQuestionsGate
{
    private readonly IConfiguration _configuration;
    private readonly IPlatformFeatureService _features;

    public G78ContrastQuestionsGate(IConfiguration configuration, IPlatformFeatureService features)
    {
        _configuration = configuration;
        _features = features;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _features.GetAsync(cancellationToken);
        if (snap.G78ContrastQuestionsEnabled)
        {
            return true;
        }

        return _configuration.GetValue("G78ContrastQuestionsEnabled", false);
    }
}
