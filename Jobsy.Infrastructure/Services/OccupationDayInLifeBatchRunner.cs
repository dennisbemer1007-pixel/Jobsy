using Jobsy.Core.Careers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Admin one-shot that walks missing occupations until it is stopped or the catalogue is filled.
/// Each occupation is saved before the next call, so a restart continues where it left off.
/// </summary>
public sealed class OccupationDayInLifeBatchRunner : IDisposable
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OccupationDayInLifeBatchRunner> _logger;
    private readonly object _gate = new();
    private CancellationTokenSource? _run;
    private Snapshot _status = Snapshot.Idle();

    public OccupationDayInLifeBatchRunner(
        IServiceScopeFactory scopes,
        ILogger<OccupationDayInLifeBatchRunner> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public Snapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _status;
            }
        }
    }

    public bool TryStart(int? limit)
    {
        lock (_gate)
        {
            if (_run is not null)
            {
                return false;
            }

            _run = new CancellationTokenSource();
            _status = Snapshot.Started();
            var token = _run.Token;
            _ = Task.Run(() => ExecuteAsync(limit, token));
            return true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _run?.Cancel();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _run?.Cancel();
            _run?.Dispose();
            _run = null;
        }
    }

    private async Task ExecuteAsync(int? limit, CancellationToken cancellationToken)
    {
        var generated = 0;
        var failed = 0;
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? lastError = null;
        string? lastEscoId = null;
        try
        {
            var cap = limit is int value ? Math.Clamp(value, 1, 5000) : (int?)null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var take = cap is int max ? Math.Min(8, max - (generated + failed)) : 8;
                if (take <= 0)
                {
                    break;
                }

                await using var scope = _scopes.CreateAsyncScope();
                var generator = scope.ServiceProvider.GetRequiredService<OccupationDayInLifeGenerator>();
                var result = await generator.GenerateMissingAsync(take, skip, onlyEscoIds: null, cancellationToken);
                generated += result.Generated;
                failed += result.Failed;
                foreach (var failure in result.Failures)
                {
                    skip.Add(failure.EscoId);
                    lastEscoId = failure.EscoId;
                    lastError = failure.Reason;
                }

                lock (_gate)
                {
                    _status = _status with
                    {
                        Generated = generated,
                        Failed = failed,
                        LastError = result.KeyMissing ? OccupationDayWriteErrors.KeyMissing : lastError,
                        LastEscoId = lastEscoId
                    };
                }

                if (result.KeyMissing)
                {
                    lastError = OccupationDayWriteErrors.KeyMissing;
                    _logger.LogWarning("Dag-in-het-leven gestopt: OpenAI-sleutel ontbreekt.");
                    break;
                }

                if (result.Generated == 0 && result.Failed == 0)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dag-in-het-leven batch gestopt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dag-in-het-leven batch mislukt.");
            lastError = "batch-fout";
        }
        finally
        {
            lock (_gate)
            {
                _run?.Dispose();
                _run = null;
                _status = _status with { Running = false, Generated = generated, Failed = failed, LastError = lastError ?? _status.LastError };
            }
        }
    }

    public sealed record Snapshot(bool Running, int Generated, int Failed, string? LastError, string? LastEscoId)
    {
        public static Snapshot Idle() => new(false, 0, 0, null, null);

        public static Snapshot Started() => new(true, 0, 0, null, null);
    }
}
