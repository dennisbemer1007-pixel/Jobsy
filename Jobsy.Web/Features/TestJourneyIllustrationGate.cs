using Jobsy.Core.Features;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Features;

/// <summary>
/// Illustrated "Wie ben jij" test journey intro (step 7). Resolved via platform feature flags API.
/// Never blocks the Blazor circuit: returns last known / config default and refreshes in the background.
/// </summary>
public interface ITestJourneyIllustrationGate
{
    bool Enabled { get; }
}

public sealed class TestJourneyIllustrationGate(IFeatureFlags featureFlags, IConfiguration configuration)
    : ITestJourneyIllustrationGate
{
    private bool _enabled = configuration.GetValue("TestJourneyIllustrationEnabled", false);
    private int _refreshScheduled;

    public bool Enabled
    {
        get
        {
            var pending = featureFlags.GetAsync();
            if (pending.IsCompletedSuccessfully)
            {
                _enabled = pending.Result.TestJourneyIllustrationEnabled;
                return _enabled;
            }

            ScheduleBackgroundRefresh(pending.AsTask());
            return _enabled;
        }
    }

    private void ScheduleBackgroundRefresh(Task<FeatureFlagSnapshot> pending)
    {
        if (Interlocked.CompareExchange(ref _refreshScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = RefreshInBackgroundAsync(pending);
    }

    private async Task RefreshInBackgroundAsync(Task<FeatureFlagSnapshot> pending)
    {
        try
        {
            var snap = await pending.ConfigureAwait(false);
            _enabled = snap.TestJourneyIllustrationEnabled;
        }
        catch
        {
            // Keep last known / config default.
        }
        finally
        {
            Interlocked.Exchange(ref _refreshScheduled, 0);
        }
    }
}
