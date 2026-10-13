namespace Jobsy.Web.Components.Shared.Questionnaire;

/// <summary>
/// Debounces answer changes (~800 ms by default) and persists the full answer
/// dictionary through the existing save endpoints.
/// </summary>
public sealed class QuestionnaireAutosave : IAsyncDisposable
{
    public const int DefaultDebounceMs = 800;

    private readonly Func<IReadOnlyDictionary<int, int>, CancellationToken, Task> _persist;
    private readonly Func<Task>? _onChanged;
    private readonly Action<QuestionnaireSaveError>? _onFailed;
    private readonly int _debounceMs;
    private readonly Dictionary<int, int> _answers = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _debounceCts;
    private int _version;
    private int _dirtyVersion;
    private int _savedVersion;
    private bool _disposed;

    public QuestionnaireAutosave(
        Func<IReadOnlyDictionary<int, int>, CancellationToken, Task> persist,
        Func<Task>? onChanged = null,
        int debounceMs = DefaultDebounceMs,
        Action<QuestionnaireSaveError>? onFailed = null)
    {
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        _onChanged = onChanged;
        _onFailed = onFailed;
        _debounceMs = Math.Max(0, debounceMs);
    }

    public QuestionnaireSaveStatus Status { get; private set; } = QuestionnaireSaveStatus.Idle;

    public IReadOnlyDictionary<int, int> Answers => _answers;

    public int AnsweredCount => _answers.Count;

    public void ReplaceAll(IReadOnlyDictionary<int, int> answers)
    {
        if (_disposed)
        {
            return;
        }

        _answers.Clear();
        foreach (var (key, value) in answers)
        {
            if (value is >= 1 and <= 5)
            {
                _answers[key] = value;
            }
        }

        Status = QuestionnaireSaveStatus.Idle;
        _dirtyVersion = 0;
        _savedVersion = 0;
    }

    public bool TryGetAnswer(int id, out int value) => _answers.TryGetValue(id, out value);

    public async Task SetAnswerAsync(int id, int value, CancellationToken ct = default)
    {
        _ = ct;
        if (_disposed)
        {
            return;
        }

        if (value is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Likert value must be 1–5.");
        }

        _answers[id] = value;
        Status = QuestionnaireSaveStatus.Saving;
        _dirtyVersion = Interlocked.Increment(ref _version);
        await NotifyAsync();

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var debounceToken = _debounceCts.Token;
        var version = _dirtyVersion;

        try
        {
            if (_debounceMs > 0)
            {
                await Task.Delay(_debounceMs, debounceToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Newer answer superseded this debounce window, or lifetime cancelled — never throw to caller.
            return;
        }

        if (_disposed || version != Volatile.Read(ref _version))
        {
            return;
        }

        try
        {
            await FlushCoreAsync(version, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Lifetime cancel during persist — quiet.
        }
    }

    public Task FlushAsync(CancellationToken ct = default)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _debounceCts?.Cancel();
        var version = Volatile.Read(ref _version);
        return FlushCoreAsync(version, ct);
    }

    public Task RetryAsync(CancellationToken ct = default)
        => FlushAsync(ct);

    private async Task FlushCoreAsync(int version, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_disposed)
            {
                return;
            }

            Status = QuestionnaireSaveStatus.Saving;
            await NotifyAsync();
            var snapshot = new Dictionary<int, int>(_answers);
            await _persist(snapshot, ct);
            if (version == Volatile.Read(ref _version))
            {
                Status = QuestionnaireSaveStatus.Saved;
                _savedVersion = version;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Status = QuestionnaireSaveStatus.Failed;
            _onFailed?.Invoke(new QuestionnaireSaveError("persist_failed", ex));
            // Do not throw — callers map Failed status / OnFailed.
        }
        finally
        {
            _gate.Release();
            await NotifyAsync();
        }
    }

    private Task NotifyAsync()
        => _onChanged?.Invoke() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        if (_dirtyVersion > _savedVersion)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _gate.WaitAsync(timeout.Token);
                try
                {
                    if (_dirtyVersion > _savedVersion)
                    {
                        var snapshot = new Dictionary<int, int>(_answers);
                        await _persist(snapshot, timeout.Token);
                        _savedVersion = _dirtyVersion;
                        Status = QuestionnaireSaveStatus.Saved;
                    }
                }
                catch
                {
                    Status = QuestionnaireSaveStatus.Failed;
                }
                finally
                {
                    _gate.Release();
                }
            }
            catch
            {
                // Dispose must not throw.
            }
        }

        _gate.Dispose();
    }
}

public sealed record QuestionnaireSaveError(string Code, Exception? Exception = null);
