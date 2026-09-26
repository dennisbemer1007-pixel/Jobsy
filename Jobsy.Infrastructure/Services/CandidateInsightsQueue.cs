using System.Threading.Channels;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

/// <summary>Channel-backed queue with in-flight deduplication per userId.</summary>
public sealed class CandidateInsightsQueue : ICandidateInsightsQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private readonly HashSet<Guid> _pending = [];
    private readonly object _gate = new();

    public bool TryEnqueue(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        lock (_gate)
        {
            if (!_pending.Add(userId))
            {
                return false;
            }
        }

        if (!_channel.Writer.TryWrite(userId))
        {
            lock (_gate)
            {
                _pending.Remove(userId);
            }

            return false;
        }

        return true;
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    public void MarkCompleted(Guid userId)
    {
        lock (_gate)
        {
            _pending.Remove(userId);
        }
    }
}
