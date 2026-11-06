using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Microsoft.Extensions.Configuration;
namespace Jobsy.Tests;

public sealed class TestJourneyIllustrationGateBunitTests
{
    [Fact]
    public void Enabled_does_not_block_when_feature_flags_still_loading()
    {
        var slow = new SlowFeatureFlags();
        var gate = new TestJourneyIllustrationGate(
            slow,
            new ConfigurationBuilder().Build());

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var enabled = gate.Enabled;
        sw.Stop();

        Assert.False(enabled);
        Assert.True(sw.ElapsedMilliseconds < 200, $"Gate blocked for {sw.ElapsedMilliseconds}ms");

        slow.Complete(new FeatureFlagSnapshot(
            EmployersEnabled: false,
            CandidatePassportEnabled: true,
            TestJourneyIllustrationEnabled: true));
        Thread.Sleep(100);
        Assert.True(gate.Enabled);
    }

    [Fact]
    public void Enabled_uses_config_default_before_first_fetch()
    {
        var slow = new SlowFeatureFlags();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TestJourneyIllustrationEnabled"] = "true" })
            .Build();
        var gate = new TestJourneyIllustrationGate(slow, config);

        Assert.True(gate.Enabled);
    }

    private sealed class SlowFeatureFlags : IFeatureFlags
    {
        private readonly TaskCompletionSource<FeatureFlagSnapshot> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Complete(FeatureFlagSnapshot snap) => _tcs.TrySetResult(snap);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => new(_tcs.Task);

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }
}
