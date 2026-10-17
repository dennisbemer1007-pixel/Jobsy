using Microsoft.JSInterop;

namespace Jobsy.Web.Components.Pages.Werkgever;

internal static class KandidatenkaartMapInterop
{
    public static async Task EnsureLoadedAsync(IJSRuntime js)
    {
        await js.InvokeVoidAsync("jobsyMaps.ensure", "discovery");
    }

    public static Task MountAsync(IJSRuntime js, string elementId, object options)
        => js.InvokeVoidAsync("jobMap.initKandidatenkaart", elementId, options).AsTask();

    public static Task UpdateAreasAsync(IJSRuntime js, string elementId, object areas, string? selectedKey)
    {
        _ = elementId;
        return js.InvokeVoidAsync("jobMap.setKandidatenkaartAreas", areas, selectedKey).AsTask();
    }

    public static Task DestroyAsync(IJSRuntime js, string elementId)
    {
        _ = elementId;
        return js.InvokeVoidAsync("jobMap.dispose").AsTask();
    }
}

internal sealed class KandidatenkaartMapAreaBridge(IKandidatenkaartMapHost host) : IDisposable
{
    private readonly IKandidatenkaartMapHost _host = host;

    [JSInvokable]
    public Task OnAreaSelected(string areaKey) => _host.SelectAreaFromMapAsync(areaKey);

    public void Dispose()
    {
    }
}

internal interface IKandidatenkaartMapHost
{
    Task SelectAreaFromMapAsync(string areaKey);
}
