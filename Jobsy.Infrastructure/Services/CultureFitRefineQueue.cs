using System.Threading.Channels;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

/// <summary>Channel-backed queue with in-flight deduplication per (userId, vacancyId).</summary>
public sealed class CultureFitRefineQueue : ICultureFitRefineQueue
{
    private readonly Channel<(Guid UserId, Guid VacancyId)> _channel =
        Channel.CreateUnbounded<(Guid, Guid)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private readonly HashSet<(Guid, Guid)> _pending = [];
    private readonly object _gate = new();

    public bool TryEnqueue(Guid userId, Guid vacancyId)
    {
        if (userId == Guid.Empty || vacancyId == Guid.Empty)
        {
            return false;
        }

        var key = (userId, vacancyId);
        lock (_gate)
        {
            if (!_pending.Add(key))
            {
                return false;
            }
        }

        if (!_channel.Writer.TryWrite(key))
        {
            lock (_gate)
            {
                _pending.Remove(key);
            }

            return false;
        }

        return true;
    }

    public ValueTask<(Guid UserId, Guid VacancyId)> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    public void MarkCompleted(Guid userId, Guid vacancyId)
    {
        lock (_gate)
        {
            _pending.Remove((userId, vacancyId));
        }
    }
}
