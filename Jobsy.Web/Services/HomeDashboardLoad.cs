namespace Jobsy.Web.Services;

/// <summary>
/// Loads role dashboards without flashing a raw HTTP error while auth/circuit settle.
/// </summary>
internal static class HomeDashboardLoad
{
    public const string FailedMessage =
        "Het dashboard kon niet worden geladen. Vernieuw de pagina als dit aanhoudt.";

    public static async Task<(T? Data, string? Error)> FetchAsync<T>(Func<Task<T>> load)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                return (await load(), null);
            }
            catch when (attempt < 2)
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
}
