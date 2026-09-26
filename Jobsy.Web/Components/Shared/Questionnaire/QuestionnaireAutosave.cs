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
    private readonly int _debounceMs;
    private readonly Dictionary<int, int> _answers = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _debounceCts;
    private int _version;
    private bool _disposed;

    public QuestionnaireAutosave(
        Func<IReadOnlyDictionary<int, int>, CancellationToken, Task> persist,
        Func<Task>? onChanged = null,
        int debounceMs = DefaultDebounceMs)
    {
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        _onChanged = onChanged;
        _debounceMs = Math.Max(0, debounceMs);
    }

    public QuestionnaireSaveStatus Status { get; private set; } = QuestionnaireSaveStatus.Saved;

    public IReadOnlyDictionary<int, int> Answers => _answers;

    public int AnsweredCount => _answers.Count;

    public void ReplaceAll(IReadOnlyDictionary<int, int> answers)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _answers.Clear();
        foreach (var (key, value) in answers)
        {
            if (value is >= 1 and <= 5)
            {
                _answers[key] = value;
            }
        }

        Status = QuestionnaireSaveStatus.Saved;
    }

    public bool TryGetAnswer(int id, out int value) => _answers.TryGetValue(id, out value);

    public async Task SetAnswerAsync(int id, int value, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (value is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Likert value must be 1–5.");
        }

        _answers[id] = value;
        Status = QuestionnaireSaveStatus.Saving;
        await NotifyAsync();

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _debounceCts.Token;
        var version = Interlocked.Increment(ref _version);

        try
        {
            if (_debounceMs > 0)
            {
                await Task.Delay(_debounceMs, token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Newer answer superseded this debounce window.
            return;
        }

        if (version != Volatile.Read(ref _version))
        {
            return;
        }

        await FlushCoreAsync(token);
    }

    public Task FlushAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _debounceCts?.Cancel();
        return FlushCoreAsync(ct);
    }

    public async Task RetryAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await FlushAsync(ct);
    }

    private async Task FlushCoreAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            Status = QuestionnaireSaveStatus.Saving;
            await NotifyAsync();
            var snapshot = new Dictionary<int, int>(_answers);
            await _persist(snapshot, ct);
            Status = QuestionnaireSaveStatus.Saved;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            Status = QuestionnaireSaveStatus.Failed;
            throw;
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
        _gate.Dispose();
        await Task.CompletedTask;
    }
}
