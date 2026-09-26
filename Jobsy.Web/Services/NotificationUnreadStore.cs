using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

/// <summary>
/// Circuit-scoped unread notification count. Loads once, then refreshes on an
/// interval — not on every page mount / NotificationBell remount.
/// Skips polls while the tab is hidden (<c>jobsyPageVisible</c>).
/// </summary>
public sealed class NotificationUnreadStore : IAsyncDisposable
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(45);

    private readonly JobsyApiClient _api;
    private readonly IJSRuntime _js;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private PeriodicTimer? _timer;
    private bool _started;
    private int _unread;

    public NotificationUnreadStore(JobsyApiClient api, IJSRuntime js)
    {
        _api = api;
        _js = js;
    }

    public int Unread
    {
        get
        {
            lock (_gate)
            {
                return _unread;
            }
        }
    }

    public event Action? Changed;

    public void EnsureStarted()
    {
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _cts = new CancellationTokenSource();
            _timer = new PeriodicTimer(PollInterval);
        }

        _ = BootstrapAsync(_cts!.Token);
    }

    public void SetUnread(int value)
    {
        lock (_gate)
        {
            _unread = Math.Max(0, value);
        }

        Changed?.Invoke();
    }

    public void Decrement()
    {
        lock (_gate)
        {
            _unread = Math.Max(0, _unread - 1);
        }

        Changed?.Invoke();
    }

    private async Task BootstrapAsync(CancellationToken ct)
    {
        await RefreshAsync(ct);
        try
        {
            while (_timer is not null && await _timer.WaitForNextTickAsync(ct))
            {
                if (!await PageIsVisibleAsync())
                {
                    continue;
                }

                await RefreshAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // disposed
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            var count = await _api.GetUnreadNotificationCountAsync(ct);
            SetUnread(count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Not signed in / transient — keep last known.
        }
    }

    private async Task<bool> PageIsVisibleAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("jobsyPageVisible");
        }
        catch
        {
            return true;
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
        return ValueTask.CompletedTask;
    }
}
