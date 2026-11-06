using System.Threading.Channels;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

/// <summary>Channel-backed queue with in-flight deduplication per vacancy id.</summary>
public sealed class ProactiveVacancyInstantMatchQueue : IProactiveVacancyInstantMatchQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private readonly HashSet<Guid> _pending = [];
    private readonly object _gate = new();

    public bool TryEnqueue(Guid vacancyId)
    {
        if (vacancyId == Guid.Empty)
        {
            return false;
        }

        lock (_gate)
        {
            if (!_pending.Add(vacancyId))
            {
                return false;
            }
        }

        if (!_channel.Writer.TryWrite(vacancyId))
        {
            lock (_gate)
            {
                _pending.Remove(vacancyId);
            }

            return false;
        }

        return true;
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    public void MarkCompleted(Guid vacancyId)
    {
        lock (_gate)
        {
            _pending.Remove(vacancyId);
        }
    }
}
