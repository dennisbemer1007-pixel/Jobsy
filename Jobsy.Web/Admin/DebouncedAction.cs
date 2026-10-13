namespace Jobsy.Web.Admin;

/// <summary>
/// Runs the latest scheduled action after a quiet period. A newer call cancels the previous one.
/// </summary>
public sealed class DebouncedAction : IDisposable
{
    private CancellationTokenSource? _cts;

    public async Task RunAsync(int delayMs, Func<CancellationToken, Task> action)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;
        try
        {
            await Task.Delay(delayMs, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await action(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer query replaced this one.
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
