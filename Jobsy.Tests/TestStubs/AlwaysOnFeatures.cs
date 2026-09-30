using Jobsy.Core.Interfaces;

namespace Jobsy.Tests;

internal sealed class AlwaysOnFeatures : IPlatformFeatureService
{
    public bool AmbassadorsEnabled { get; set; } = true;

    public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new PlatformFeatureSnapshot(
            true, true, false, "http://localhost", null, AmbassadorsEnabled: AmbassadorsEnabled));

    public Task<PlatformFeatureSnapshot> UpdateAsync(
        PlatformFeatureUpdate update,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
