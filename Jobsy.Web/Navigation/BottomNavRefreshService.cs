namespace Jobsy.Web.Navigation;

/// <summary>
/// Lets pages (e.g. Bedrijfsgegevens CSV toggle) ask the bottom nav to rebuild immediately
/// without waiting for a location change.
/// </summary>
public sealed class BottomNavRefreshService
{
    public event Func<Task>? RefreshRequested;

    public Task RequestRefreshAsync()
    {
        var handler = RefreshRequested;
        return handler?.Invoke() ?? Task.CompletedTask;
    }
}
