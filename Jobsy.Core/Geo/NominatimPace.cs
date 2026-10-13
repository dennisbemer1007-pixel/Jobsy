namespace Jobsy.Core.Geo;

/// <summary>
/// Nominatim usage policy: at most one request per second from this process.
/// The first call does not wait.
/// </summary>
public sealed class NominatimPace : IDisposable
{
    public static readonly TimeSpan DefaultGap = TimeSpan.FromSeconds(1);

    private readonly TimeSpan _gap;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _nextUtc = DateTime.MinValue;

    public NominatimPace()
        : this(DefaultGap)
    {
    }

    public NominatimPace(TimeSpan gap)
    {
        _gap = gap < TimeSpan.Zero ? TimeSpan.Zero : gap;
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTime.UtcNow;
            if (_nextUtc > now)
            {
                await Task.Delay(_nextUtc - now, cancellationToken).ConfigureAwait(false);
            }

            _nextUtc = DateTime.UtcNow.Add(_gap);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
