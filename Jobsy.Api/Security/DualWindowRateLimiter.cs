using System.Threading.RateLimiting;

namespace Jobsy.Api.Security;

/// <summary>
/// Two fixed windows behind one limiter. The DSA report form needs a short and a long budget per
/// visitor (5/hour and 20/day); ASP.NET applies one policy per endpoint, so the policy composes them
/// here instead of stacking attributes.
/// </summary>
internal sealed class DualWindowRateLimiter : RateLimiter
{
    private readonly FixedWindowRateLimiter _shortWindow;
    private readonly FixedWindowRateLimiter _longWindow;

    public DualWindowRateLimiter(
        int shortPermitLimit,
        TimeSpan shortWindow,
        int longPermitLimit,
        TimeSpan longWindow)
    {
        _shortWindow = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, shortPermitLimit),
            Window = shortWindow,
            QueueLimit = 0
        });
        _longWindow = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, longPermitLimit),
            Window = longWindow,
            QueueLimit = 0
        });
    }

    public override TimeSpan? IdleDuration
        => _shortWindow.IdleDuration is { } shortIdle && _longWindow.IdleDuration is { } longIdle
            ? (shortIdle < longIdle ? shortIdle : longIdle)
            : null;

    public override RateLimiterStatistics? GetStatistics() => _shortWindow.GetStatistics();

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        var shortLease = _shortWindow.AttemptAcquire(permitCount);
        if (!shortLease.IsAcquired)
        {
            return shortLease;
        }

        var longLease = _longWindow.AttemptAcquire(permitCount);
        if (!longLease.IsAcquired)
        {
            shortLease.Dispose();
            return longLease;
        }

        return new PairLease(shortLease, longLease);
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AttemptAcquireCore(permitCount));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shortWindow.Dispose();
            _longWindow.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _shortWindow.DisposeAsync();
        await _longWindow.DisposeAsync();
    }

    private sealed class PairLease(RateLimitLease first, RateLimitLease second) : RateLimitLease
    {
        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames
            => first.MetadataNames.Concat(second.MetadataNames).Distinct(StringComparer.Ordinal);

        public override bool TryGetMetadata(string metadataName, out object? metadata)
            => first.TryGetMetadata(metadataName, out metadata)
               || second.TryGetMetadata(metadataName, out metadata);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                first.Dispose();
                second.Dispose();
            }
        }
    }
}
