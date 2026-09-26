using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Safe wrappers for Blazor JS interop and fire-and-forget work that must not
/// tear down the circuit when the circuit is already gone.
/// </summary>
public static class CircuitInterop
{
    public static bool IsCircuitGone(Exception ex)
        => ex is JSDisconnectedException
            or ObjectDisposedException
            or OperationCanceledException
            or TaskCanceledException;

    public static async Task InvokeVoidIgnoredAsync(IJSRuntime js, string identifier, params object?[] args)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, args);
        }
        catch (Exception ex) when (IsCircuitGone(ex))
        {
            // Circuit disposed / navigation away.
        }
    }

    public static async Task<T?> InvokeIgnoredAsync<T>(IJSRuntime js, string identifier, params object?[] args)
    {
        try
        {
            return await js.InvokeAsync<T>(identifier, args);
        }
        catch (Exception ex) when (IsCircuitGone(ex))
        {
            return default;
        }
    }

    public static CancellationTokenSource CreateLinkedCts(params CancellationToken[] tokens)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(tokens);
        return linked;
    }
}
