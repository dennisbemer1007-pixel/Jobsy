using System.Net;

namespace Jobsy.Web.Services;

/// <summary>
/// Loads role dashboards without flashing a raw HTTP error while auth/circuit settle.
/// </summary>
public static class HomeDashboardLoad
{
    public const string FailedMessage =
        "Het dashboard kon niet worden geladen. Vernieuw de pagina als dit aanhoudt.";

    /// <summary>
    /// Starts dashboard loading once the circuit is interactive.
    /// Prerender/enhanced-nav can run <c>OnInitializedAsync</c> with
    /// <c>RendererInfo.IsInteractive == false</c>; callers must retry from
    /// <c>OnAfterRenderAsync</c> so the skeleton is not permanent.
    /// </summary>
    public static bool TryBegin(ref bool started, bool isInteractive)
    {
        if (started || !isInteractive)
        {
            return false;
        }

        started = true;
        return true;
    }

    public static async Task<(T? Data, string? Error)> FetchAsync<T>(Func<Task<T>> load)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                return (await load(), null);
            }
            catch (Exception ex) when (attempt < 2 && IsTransient(ex))
            {
                await Task.Delay(400);
            }
            catch
            {
                return (default, FailedMessage);
            }
        }

        return (default, FailedMessage);
    }

    internal static bool IsTransient(Exception ex)
    {
        if (ex is HttpRequestException http)
        {
            return http.StatusCode is null
                or HttpStatusCode.Unauthorized
                or HttpStatusCode.RequestTimeout
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;
        }

        if (ex is TaskCanceledException)
        {
            return true;
        }

        if (ex is InvalidOperationException)
        {
            var message = ex.Message;
            return message.Contains("401", StringComparison.Ordinal)
                   || message.Contains("503", StringComparison.Ordinal)
                   || message.Contains("502", StringComparison.Ordinal)
                   || message.Contains("504", StringComparison.Ordinal);
        }

        return false;
    }
}
