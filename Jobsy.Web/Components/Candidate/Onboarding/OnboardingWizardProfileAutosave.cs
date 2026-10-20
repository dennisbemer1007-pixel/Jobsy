namespace Jobsy.Web.Components.Candidate.Onboarding;

/// <summary>
/// Debounces onboarding profile draft changes and persists through a single serialized gate.
/// UI stays optimistic: callers mutate <see cref="OnboardingProfileDraft"/> locally and only schedule saves here.
/// </summary>
internal sealed class OnboardingWizardProfileAutosave : IAsyncDisposable
{
    public const int DefaultDebounceMs = 450;

    private readonly Func<CancellationToken, Task> _persist;
    private readonly Action? _onPersistStarted;
    private readonly Action<bool>? _onPersistEnded;
    private readonly Action? _onPersistSucceeded;
    private readonly int _debounceMs;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _debounceCts;
    private int _generation;
    private int _savedGeneration;
    private bool _disposed;

    public OnboardingWizardProfileAutosave(
        Func<CancellationToken, Task> persist,
        Action? onPersistStarted = null,
        Action<bool>? onPersistEnded = null,
        Action? onPersistSucceeded = null,
        int debounceMs = DefaultDebounceMs)
    {
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        _onPersistStarted = onPersistStarted;
        _onPersistEnded = onPersistEnded;
        _onPersistSucceeded = onPersistSucceeded;
        _debounceMs = Math.Max(0, debounceMs);
    }

    public void ScheduleSave()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Increment(ref _generation);
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = DebounceAsync(token);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
        Interlocked.Increment(ref _generation);
        await FlushPendingAsync(cancellationToken);
    }

    private async Task DebounceAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_debounceMs > 0)
            {
                await Task.Delay(_debounceMs, cancellationToken);
            }

            await FlushPendingAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task FlushPendingAsync(CancellationToken cancellationToken)
    {
        while (_savedGeneration < Volatile.Read(ref _generation))
        {
            var version = Volatile.Read(ref _generation);
            await PersistOnceAsync(version, cancellationToken);
        }
    }

    private async Task PersistOnceAsync(int version, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        var succeeded = false;
        try
        {
            if (_disposed || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _onPersistStarted?.Invoke();
            await _persist(cancellationToken);
            if (version != Volatile.Read(ref _generation))
            {
                return;
            }

            _savedGeneration = version;
            succeeded = true;
            _onPersistSucceeded?.Invoke();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            succeeded = false;
        }
        finally
        {
            _onPersistEnded?.Invoke(succeeded);
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        if (_savedGeneration < Volatile.Read(ref _generation))
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                await FlushPendingAsync(timeout.Token);
            }
            catch
            {
                // Best effort on teardown.
            }
        }

        _gate.Dispose();
    }
}
