using Jobsy.Core.Features;

namespace Jobsy.Web.Features;

/// <summary>
/// Illustrated "Wie ben jij" test journey intro (step 7). Resolved via platform feature flags API.
/// </summary>
public interface ITestJourneyIllustrationGate
{
    bool Enabled { get; }
}

public sealed class TestJourneyIllustrationGate(IFeatureFlags featureFlags) : ITestJourneyIllustrationGate
{
    public bool Enabled
    {
        get
        {
            var pending = featureFlags.GetAsync();
            if (pending.IsCompletedSuccessfully)
            {
                return pending.Result.TestJourneyIllustrationEnabled;
            }

            return pending.AsTask().GetAwaiter().GetResult().TestJourneyIllustrationEnabled;
        }
    }
}
