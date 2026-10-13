using Jobsy.Core.Admin;
using Jobsy.Core.Careers;
using Jobsy.Core.Interfaces;
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

    public bool TryStart(
        int? limit,
        IReadOnlySet<string>? onlyEscoIds = null,
        Guid? actorUserId = null,
        string? actorRole = null)
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
            _ = Task.Run(() => ExecuteAsync(limit, onlyEscoIds, actorUserId, actorRole, token));
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

    private async Task ExecuteAsync(
        int? limit,
        IReadOnlySet<string>? onlyEscoIds,
        Guid? actorUserId,
        string? actorRole,
        CancellationToken cancellationToken)
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

                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var generator = scope.ServiceProvider.GetRequiredService<OccupationDayInLifeGenerator>();
                    var result = await generator.GenerateMissingAsync(take, skip, onlyEscoIds, cancellationToken);
                    generated += result.Generated;
                    failed += result.Failed;
                    foreach (var failure in result.Failures)
                    {
                        skip.Add(failure.EscoId);
                        lastEscoId = failure.EscoId;
                        lastError = failure.Reason;
                    }

                    var shown = result.KeyMissing
                        ? OccupationDayWriteErrors.KeyMissing
                        : result.KeyRejected
                            ? lastError ?? OccupationDayWriteErrors.KeyInvalid
                            : lastError;
                    lock (_gate)
                    {
                        _status = _status with
                        {
                            Generated = generated,
                            Failed = failed,
                            LastError = shown,
                            LastEscoId = lastEscoId
                        };
                    }

                    if (result.KeyMissing)
                    {
                        lastError = OccupationDayWriteErrors.KeyMissing;
                        _logger.LogWarning("Dag-in-het-leven gestopt: OpenAI-sleutel ontbreekt.");
                        break;
                    }

                    if (result.KeyRejected)
                    {
                        lastError = lastError ?? OccupationDayWriteErrors.KeyInvalid;
                        _logger.LogWarning(
                            "Dag-in-het-leven gestopt: OpenAI weigert de sleutel. {Reason}",
                            OccupationDayWriteErrors.SafeSnippet(lastError));
                        break;
                    }

                    if (result.Generated == 0 && result.Failed == 0)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    lastError = OccupationDayWriteErrors.Timeout;
                    _logger.LogWarning(
                        "Dag-in-het-leven batch-stap mislukt: {Reason}",
                        OccupationDayWriteErrors.SafeSnippet(ex.Message));
                    lock (_gate)
                    {
                        _status = _status with { Generated = generated, Failed = failed, LastError = lastError, LastEscoId = lastEscoId };
                    }

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

            await WriteOutcomeAsync(generated, failed, lastError, actorUserId, actorRole);
        }
    }

    private async Task WriteOutcomeAsync(int generated, int failed, string? lastError, Guid? actorUserId, string? actorRole)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAdminAuditLog>();
            var reason = generated + " gelukt, " + failed + " mislukt.";
            if (!string.IsNullOrWhiteSpace(lastError))
            {
                reason = reason + " " + OccupationDayWriteErrors.SafeSnippet(lastError);
            }

            await audit.WriteAsync(new AdminAuditEntry(
                Action: AdminAuditKeys.OccupationDayGenerate,
                TargetType: "setting",
                Reason: reason,
                Result: failed > 0 && generated == 0 ? AdminAuditKeys.Results.Failed : AdminAuditKeys.Results.Success,
                ActorUserId: actorUserId,
                ActorRole: string.IsNullOrWhiteSpace(actorRole) ? "Admin" : actorRole,
                ActorKind: AdminAuditKeys.ActorKinds.Admin));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit voor dag-in-het-leven kon niet worden opgeslagen.");
        }
    }

    public sealed record Snapshot(bool Running, int Generated, int Failed, string? LastError, string? LastEscoId)
    {
        public static Snapshot Idle() => new(false, 0, 0, null, null);

        public static Snapshot Started() => new(true, 0, 0, null, null);
    }
}
