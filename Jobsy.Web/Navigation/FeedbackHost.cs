namespace Jobsy.Web.Navigation;

/// <summary>
/// Opens the feedback dialog from chrome (account menu) without relying on the edge tab.
/// </summary>
public sealed class FeedbackHost
{
    public event Func<Task>? OpenRequested;

    public void RequestOpen()
    {
        var handlers = OpenRequested;
        if (handlers is null)
        {
            return;
        }

        _ = InvokeAsync(handlers);
    }

    private static async Task InvokeAsync(Func<Task> handlers)
    {
        foreach (var handler in handlers.GetInvocationList().OfType<Func<Task>>())
        {
            try
            {
                await handler();
            }
            catch
            {
                // Ignore disposed circuits.
            }
        }
    }
}
