using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>Loads MapLibre GL + map helpers once per circuit. Discovery starts immediately on the homepage.</summary>
public static class MapScripts
{
    public static ValueTask EnsureAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure");

    public static ValueTask EnsureDiscoveryAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure", "discovery");

    public static ValueTask EnsureDetailAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure", "detail");
}
