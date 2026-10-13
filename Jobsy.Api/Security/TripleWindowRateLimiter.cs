using System.Threading.RateLimiting;

namespace Jobsy.Api.Security;

/// <summary>
/// Three fixed windows (burst / hour / day) behind one limiter for authenticated candidate apply.
/// </summary>
internal sealed class TripleWindowRateLimiter : RateLimiter
{
    public const string LimitWindowMetadata = "applyLimitWindow";

    private readonly FixedWindowRateLimiter _burstWindow;
    private readonly FixedWindowRateLimiter _hourWindow;
    private readonly FixedWindowRateLimiter _dayWindow;

    public TripleWindowRateLimiter(
        int burstPermitLimit,
        TimeSpan burstWindow,
        int hourlyPermitLimit,
        TimeSpan hourlyWindow,
        int dailyPermitLimit,
        TimeSpan dailyWindow)
    {
        _burstWindow = CreateWindow(burstPermitLimit, burstWindow);
        _hourWindow = CreateWindow(hourlyPermitLimit, hourlyWindow);
        _dayWindow = CreateWindow(dailyPermitLimit, dailyWindow);
    }

    public override TimeSpan? IdleDuration
    {
        get
        {
            var values = new[] { _burstWindow.IdleDuration, _hourWindow.IdleDuration, _dayWindow.IdleDuration }
                .Where(v => v is not null)
                .Select(v => v!.Value)
                .ToArray();
            return values.Length == 0 ? null : values.Min();
        }
    }

    public override RateLimiterStatistics? GetStatistics() => _hourWindow.GetStatistics();

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        var burstLease = _burstWindow.AttemptAcquire(permitCount);
        if (!burstLease.IsAcquired)
        {
            return new WindowRejectedLease(burstLease, "minute");
        }

        var hourLease = _hourWindow.AttemptAcquire(permitCount);
        if (!hourLease.IsAcquired)
        {
            burstLease.Dispose();
            return new WindowRejectedLease(hourLease, "hour");
        }

        var dayLease = _dayWindow.AttemptAcquire(permitCount);
        if (!dayLease.IsAcquired)
        {
            burstLease.Dispose();
            hourLease.Dispose();
            return new WindowRejectedLease(dayLease, "day");
        }

        return new TripleLease(burstLease, hourLease, dayLease);
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
            _burstWindow.Dispose();
            _hourWindow.Dispose();
            _dayWindow.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _burstWindow.DisposeAsync();
        await _hourWindow.DisposeAsync();
        await _dayWindow.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    private static FixedWindowRateLimiter CreateWindow(int permitLimit, TimeSpan window)
        => new(new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, permitLimit),
            Window = window,
            QueueLimit = 0
        });

    private sealed class TripleLease(RateLimitLease burst, RateLimitLease hour, RateLimitLease day) : RateLimitLease
    {
        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames
            => burst.MetadataNames.Concat(hour.MetadataNames).Concat(day.MetadataNames).Distinct(StringComparer.Ordinal);

        public override bool TryGetMetadata(string metadataName, out object? metadata)
            => burst.TryGetMetadata(metadataName, out metadata)
               || hour.TryGetMetadata(metadataName, out metadata)
               || day.TryGetMetadata(metadataName, out metadata);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                burst.Dispose();
                hour.Dispose();
                day.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class WindowRejectedLease(RateLimitLease inner, string window) : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames
            => inner.MetadataNames.Append(LimitWindowMetadata).Distinct(StringComparer.Ordinal);

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (string.Equals(metadataName, LimitWindowMetadata, StringComparison.Ordinal))
            {
                metadata = window;
                return true;
            }

            return inner.TryGetMetadata(metadataName, out metadata);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
