using Microsoft.JSInterop;

namespace Jobsy.Web.Pwa;

public static class PwaInstallInterop
{
    public static async Task<PwaInstallSignals?> GetSignalsAsync(IJSRuntime js)
    {
        try
        {
            return await js.InvokeAsync<PwaInstallSignals>("lobsyPwaInstall.getSignals");
        }
        catch
        {
            return null;
        }
    }
}
