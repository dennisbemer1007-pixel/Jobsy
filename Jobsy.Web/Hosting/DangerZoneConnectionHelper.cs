using System.Net.Http;
using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>Detects Blazor circuit / transport loss during irreversible danger-zone actions.</summary>
public static class DangerZoneConnectionHelper
{
    public static bool IsTransportOrCircuitFailure(Exception ex)
        => ex is HttpRequestException
           || CircuitInterop.IsCircuitGone(ex);

    public static async Task<bool> IsReadyForServerActionAsync(IJSRuntime js, CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("lobsyCircuit.isReadyForAction", cancellationToken);
        }
        catch (Exception ex) when (CircuitInterop.IsCircuitGone(ex))
        {
            return false;
        }
        catch (JSException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static async Task ScheduleReloadOnceAsync(IJSRuntime js, string reason)
    {
        await CircuitInterop.InvokeVoidIgnoredAsync(js, "lobsyCircuit.reloadPageOnce", reason);
    }
}
